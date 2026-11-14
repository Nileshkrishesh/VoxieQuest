// VOXIE QUEST - Voice System & MicLevel Audio Analyser
// Bridges Web Speech API & Web Audio API AnalyserNode directly to C# Blazor

(function () {
    let recognition = null;
    let voiceDotNetRef = null;
    let isManuallyStopped = false;
    let isPushToTalkMode = false;
    let isSpeakingActive = false;
    let restartTimerId = null;

    // Web Audio Analyser for Mic Level
    let audioContext = null;
    let mediaStream = null;
    let analyserNode = null;
    let animFrameId = null;
    let lastVolumeSentTime = 0;
    const VOLUME_THROTTLE_MS = 80; // ~12 updates per second to avoid overtaxing WebAssembly thread on mobile

    /**
     * Determines optimal speech recognition locale for the user.
     * Accurately supports Indian accents, UK, and US.
     */
    function getOptimalSpeechLang() {
        const navLang = navigator.language || navigator.userLanguage || "en-US";
        if (navLang.startsWith("en-IN") || navLang.startsWith("hi")) {
            return "en-IN";
        }
        if (navLang.startsWith("en-GB")) {
            return "en-GB";
        }
        if (navLang.startsWith("en")) {
            return navLang;
        }
        return "en-US";
    }

    /**
     * Detects mobile browser environment.
     */
    function isMobileBrowser() {
        return /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);
    }

    window.voxieVoiceService = {
        /**
         * Checks or requests browser microphone permission.
         */
        requestPermission: async function () {
            try {
                if (navigator.mediaDevices && navigator.mediaDevices.getUserMedia) {
                    const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
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
         * Starts speech recognition with multi-alternative capture and mobile-resilient auto-restart.
         */
        startListening: function (dotNetHelper, pushToTalk) {
            voiceDotNetRef = dotNetHelper || voiceDotNetRef;
            isManuallyStopped = false;
            isPushToTalkMode = !!pushToTalk;

            if (restartTimerId) {
                clearTimeout(restartTimerId);
                restartTimerId = null;
            }

            // Do not listen while Voxie is speaking aloud through the device speaker
            if (isSpeakingActive) {
                return;
            }

            const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
            if (!SpeechRecognition) {
                if (voiceDotNetRef) {
                    voiceDotNetRef.invokeMethodAsync("OnJsError", "Web Speech API is not supported in this browser. Please use Chrome, Edge, or enable Practice Mode.");
                }
                return;
            }

            if (recognition) {
                try { recognition.abort(); } catch (_) { }
                recognition = null;
            }

            try {
                recognition = new SpeechRecognition();
                // On mobile Android/iOS, continuous=true often causes native crashes or disconnects.
                // We use false on mobile and auto-restart cleanly in onend.
                recognition.continuous = !isMobileBrowser() && !isPushToTalkMode;
                recognition.interimResults = true;
                recognition.lang = getOptimalSpeechLang();
                recognition.maxAlternatives = 5;

                recognition.onstart = function () {
                    if (voiceDotNetRef) {
                        voiceDotNetRef.invokeMethodAsync("OnJsListeningStatus", true);
                    }
                };

                recognition.onresult = function (event) {
                    if (!event.results || event.results.length === 0) return;

                    // Iterate over any changed results
                    for (let i = event.resultIndex; i < event.results.length; ++i) {
                        const res = event.results[i];
                        if (!res || res.length === 0) continue;

                        const isFinal = res.isFinal;
                        const alternatives = [];

                        // Collect all candidate transcriptions returned by the acoustic model
                        for (let a = 0; a < res.length; a++) {
                            if (res[a] && res[a].transcript) {
                                const clean = res[a].transcript.trim();
                                if (clean && !alternatives.includes(clean)) {
                                    alternatives.push(clean);
                                }
                            }
                        }

                        if (alternatives.length === 0) continue;

                        const topTranscript = alternatives[0];
                        const confidence = res[0] ? (res[0].confidence || 1.0) : 1.0;
                        const joinedAlts = alternatives.join("|");

                        if (voiceDotNetRef) {
                            voiceDotNetRef.invokeMethodAsync("OnJsSpeechResultWithAlternatives", topTranscript, joinedAlts, confidence, isFinal);
                        }
                    }
                };

                recognition.onerror = function (event) {
                    if (event.error === "no-speech") {
                        // Normal silence while the child is thinking
                        return;
                    }
                    if (event.error === "aborted") {
                        return;
                    }
                    console.warn("[VoxieVoice] Recognition error:", event.error);
                };

                recognition.onend = function () {
                    if (!isManuallyStopped && !isPushToTalkMode && !isSpeakingActive) {
                        // Debounced restart ensures native audio driver is freed before re-arming
                        if (restartTimerId) clearTimeout(restartTimerId);
                        restartTimerId = setTimeout(function () {
                            if (!isManuallyStopped && !isSpeakingActive) {
                                try {
                                    if (recognition) {
                                        recognition.start();
                                    }
                                } catch (e) {
                                    // If already started or interrupted, safely re-initiate
                                    window.voxieVoiceService.startListening(voiceDotNetRef, isPushToTalkMode);
                                }
                            }
                        }, 250);
                        return;
                    }

                    if (voiceDotNetRef) {
                        voiceDotNetRef.invokeMethodAsync("OnJsListeningStatus", false);
                    }
                };

                recognition.start();
            } catch (err) {
                console.warn("[VoxieVoice] Failed to start recognition:", err);
            }

            // Engage volume meter for tactile bouncy feedback
            window.voxieVoiceService.startVolumeMeter(dotNetHelper);
        },

        /**
         * Stops active speech recognition and disconnects meter.
         */
        stopListening: function () {
            isManuallyStopped = true;
            if (restartTimerId) {
                clearTimeout(restartTimerId);
                restartTimerId = null;
            }
            if (recognition) {
                try { recognition.abort(); } catch (_) { }
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
                return;
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
                analyserNode.fftSize = 128;
                analyserNode.smoothingTimeConstant = 0.4;
                source.connect(analyserNode);

                const dataArray = new Uint8Array(analyserNode.frequencyBinCount);

                function pollVolume() {
                    if (!analyserNode) return;

                    analyserNode.getByteTimeDomainData(dataArray);

                    let sumSquares = 0;
                    for (let i = 0; i < dataArray.length; i++) {
                        const normalized = (dataArray[i] - 128) / 128;
                        sumSquares += normalized * normalized;
                    }
                    const rms = Math.sqrt(sumSquares / dataArray.length);

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
                // If microphone hardware is locked or restricted on mobile,
                // degrade gracefully without disrupting speech recognition
                console.warn("[VoxieVoice] Web Audio AnalyserNode fallback:", err);
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
         * Speaks text aloud, muting the microphone while Voxie speaks so prompt words are never recorded as answers.
         * Returns a Promise that resolves when speech synthesis finishes.
         */
        speak: function (text, rate, pitch, dotNetHelper) {
            return new Promise((resolve) => {
                if (!window.speechSynthesis || !text) {
                    if (dotNetHelper) {
                        try { dotNetHelper.invokeMethodAsync("OnJsSpeechFinished"); } catch (_) { }
                    }
                    resolve();
                    return;
                }

                // Mutual exclusion: Stop recognition while speaking so microphone doesn't hear Voxie's voice
                isSpeakingActive = true;
                if (recognition) {
                    try { recognition.abort(); } catch (_) { }
                }

                window.speechSynthesis.cancel();
                const utterance = new SpeechSynthesisUtterance(text);
                utterance.rate = rate || 0.95;
                utterance.pitch = pitch || 1.1;
                utterance.lang = getOptimalSpeechLang();

                const voices = window.speechSynthesis.getVoices();
                const friendly = voices.find(v => v.lang.startsWith("en") && (v.name.includes("Natural") || v.name.includes("Samantha") || v.name.includes("Google") || v.name.includes("India")));
                if (friendly) utterance.voice = friendly;

                let hasFinished = false;
                function finish() {
                    if (hasFinished) return;
                    hasFinished = true;
                    isSpeakingActive = false;

                    if (dotNetHelper) {
                        try { dotNetHelper.invokeMethodAsync("OnJsSpeechFinished"); } catch (_) { }
                    }
                    resolve();
                }

                utterance.onend = finish;
                utterance.onerror = finish;

                // Safety timeout in case browser drops onend (mobile Safari bug)
                const safeDurationMs = Math.max(1800, text.length * 85);
                setTimeout(finish, safeDurationMs);

                window.speechSynthesis.speak(utterance);
            });
        },

        stopSpeaking: function () {
            isSpeakingActive = false;
            if (window.speechSynthesis) {
                window.speechSynthesis.cancel();
            }
        }
    };
})();
