// VOXIE QUEST - Voice System & MicLevel Audio Analyser
// Bridges Web Speech API & Web Audio API AnalyserNode directly to C# Blazor

(function () {
    let recognition = null;
    let voiceDotNetRef = null;
    let isManuallyStopped = false;
    let isPushToTalkMode = false;

    // Web Audio Analyser for Mic Level
    let audioContext = null;
    let mediaStream = null;
    let analyserNode = null;
    let animFrameId = null;
    let lastVolumeSentTime = 0;
    const VOLUME_THROTTLE_MS = 40; // ~25 updates per second to avoid overtaxing WebAssembly message bus

    window.voxieVoiceService = {
        /**
         * Checks or requests browser microphone permission.
         */
        requestPermission: async function () {
            try {
                if (navigator.mediaDevices && navigator.mediaDevices.getUserMedia) {
                    const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
                    // Keep tracks alive if already initializing, or stop temporary probe stream
                    stream.getTracks().forEach(t => t.stop());
                    return true;
                }
                return false;
            } catch (err) {
                console.warn("[VoxieVoice] Microphone permission denied or unavailable:", err);
                return false;
            }
        },

        /**
         * Starts speech recognition with interim results and automatic continuous support.
         */
        startListening: function (dotNetHelper, pushToTalk) {
            voiceDotNetRef = dotNetHelper;
            isManuallyStopped = false;
            isPushToTalkMode = !!pushToTalk;

            const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
            if (!SpeechRecognition) {
                if (voiceDotNetRef) {
                    voiceDotNetRef.invokeMethodAsync("OnJsError", "Web Speech API is not supported in this browser. Please use Chrome, Edge, or enable Practice Mode.");
                }
                return;
            }

            if (recognition) {
                try { recognition.abort(); } catch (_) { }
            }

            recognition = new SpeechRecognition();
            recognition.continuous = !isPushToTalkMode;
            recognition.interimResults = true;
            recognition.lang = "en-US";
            recognition.maxAlternatives = 3;

            recognition.onstart = function () {
                if (voiceDotNetRef) {
                    voiceDotNetRef.invokeMethodAsync("OnJsListeningStatus", true);
                }
            };

            recognition.onresult = function (event) {
                if (!event.results || event.results.length === 0) return;

                const lastResult = event.results[event.results.length - 1];
                const transcript = lastResult[0].transcript;
                const confidence = lastResult[0].confidence || 1.0;
                const isFinal = lastResult.isFinal;

                if (voiceDotNetRef) {
                    voiceDotNetRef.invokeMethodAsync("OnJsSpeechResult", transcript, confidence, isFinal);
                }
            };

            recognition.onerror = function (event) {
                if (event.error === "no-speech") {
                    // Normal silence event while child is thinking
                    return;
                }
                if (voiceDotNetRef) {
                    voiceDotNetRef.invokeMethodAsync("OnJsError", "Microphone: " + event.error);
                }
            };

            recognition.onend = function () {
                if (!isManuallyStopped && !isPushToTalkMode && recognition) {
                    // Auto-listen mode restarts continuous capture
                    try {
                        recognition.start();
                        return;
                    } catch (_) { }
                }

                if (voiceDotNetRef) {
                    voiceDotNetRef.invokeMethodAsync("OnJsListeningStatus", false);
                }
            };

            try {
                recognition.start();
            } catch (err) {
                if (voiceDotNetRef) {
                    voiceDotNetRef.invokeMethodAsync("OnJsError", "Failed to start recognition: " + err.message);
                }
            }

            // Simultaneously engage AnalyserNode for wobbling character feedback
            window.voxieVoiceService.startVolumeMeter(dotNetHelper);
        },

        /**
         * Stops active speech recognition and disconnects meter.
         */
        stopListening: function () {
            isManuallyStopped = true;
            if (recognition) {
                try { recognition.stop(); } catch (_) { }
                recognition = null;
            }
            if (voiceDotNetRef) {
                voiceDotNetRef.invokeMethodAsync("OnJsListeningStatus", false);
            }
            window.voxieVoiceService.stopVolumeMeter();
        },

        /**
         * Starts Web Audio AnalyserNode to compute 0.0 - 1.0 volume levels.
         */
        startVolumeMeter: async function (dotNetHelper) {
            voiceDotNetRef = dotNetHelper || voiceDotNetRef;

            if (analyserNode && mediaStream) {
                return; // Already actively streaming
            }

            try {
                if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
                    return;
                }

                mediaStream = await navigator.mediaDevices.getUserMedia({ audio: true, video: false });
                const AudioContextClass = window.AudioContext || window.webkitAudioContext;
                if (!AudioContextClass) return;

                audioContext = new AudioContextClass();
                if (audioContext.state === "suspended") {
                    await audioContext.resume();
                }

                const source = audioContext.createMediaStreamSource(mediaStream);
                analyserNode = audioContext.createAnalyser();
                analyserNode.fftSize = 256;
                analyserNode.smoothingTimeConstant = 0.5;
                source.connect(analyserNode);

                const dataArray = new Uint8Array(analyserNode.frequencyBinCount);

                function pollVolume() {
                    if (!analyserNode) return;

                    analyserNode.getByteTimeDomainData(dataArray);

                    // Compute Root-Mean-Square (RMS) amplitude
                    let sumSquares = 0;
                    for (let i = 0; i < dataArray.length; i++) {
                        const normalized = (dataArray[i] - 128) / 128; // -1.0 to 1.0
                        sumSquares += normalized * normalized;
                    }
                    const rms = Math.sqrt(sumSquares / dataArray.length);

                    // Scale & clamp to 0.0 - 1.0 with gentle noise gate
                    let volume = rms * 3.5;
                    if (volume < 0.03) volume = 0.0;
                    volume = Math.min(1.0, Math.max(0.0, volume));

                    const now = performance.now();
                    if (now - lastVolumeSentTime >= VOLUME_THROTTLE_MS) {
                        lastVolumeSentTime = now;
                        if (voiceDotNetRef) {
                            voiceDotNetRef.invokeMethodAsync("OnJsVolumeChanged", volume);
                        }
                    }

                    animFrameId = requestAnimationFrame(pollVolume);
                }

                pollVolume();
            } catch (err) {
                console.warn("[VoxieVoice] Could not initialize Web Audio AnalyserNode:", err);
            }
        },

        /**
         * Shuts down volume meter and releases audio stream.
         */
        stopVolumeMeter: function () {
            if (animFrameId) {
                cancelAnimationFrame(animFrameId);
                animFrameId = null;
            }
            if (mediaStream) {
                mediaStream.getTracks().forEach(t => t.stop());
                mediaStream = null;
            }
            if (audioContext && audioContext.state !== "closed") {
                audioContext.close();
                audioContext = null;
            }
            analyserNode = null;

            if (voiceDotNetRef) {
                voiceDotNetRef.invokeMethodAsync("OnJsVolumeChanged", 0.0);
            }
        },

        /**
         * Speaks text aloud with gentle, pitch-shifted child-friendly voice.
         */
        speak: function (text, rate, pitch, dotNetHelper) {
            if (!window.speechSynthesis) return;

            window.speechSynthesis.cancel();
            const utterance = new SpeechSynthesisUtterance(text);
            utterance.rate = rate || 0.9;
            utterance.pitch = pitch || 1.1;
            utterance.lang = "en-US";

            const voices = window.speechSynthesis.getVoices();
            const friendly = voices.find(v => v.lang.startsWith("en") && (v.name.includes("Natural") || v.name.includes("Samantha") || v.name.includes("Google")));
            if (friendly) utterance.voice = friendly;

            utterance.onend = function () {
                if (dotNetHelper) dotNetHelper.invokeMethodAsync("OnJsSpeechFinished");
            };
            utterance.onerror = function () {
                if (dotNetHelper) dotNetHelper.invokeMethodAsync("OnJsSpeechFinished");
            };

            window.speechSynthesis.speak(utterance);
        },

        stopSpeaking: function () {
            if (window.speechSynthesis) {
                window.speechSynthesis.cancel();
            }
        }
    };
})();
