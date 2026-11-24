// VOXIE QUEST - Production Voice System & Audio Capture Engine
// Bridges Web Speech API, Web Audio Analyser, and MediaStream lifecycle to Blazor C#

(function () {
    let recognition = null;
    let voiceDotNetRef = null;
    let isManuallyStopped = true;
    let isPushToTalkMode = false;
    let isSpeakingActive = false;
    let pendingListenRequest = false;
    let restartTimerId = null;
    let speechDebounceTimer = null;
    let silenceWatchdogTimer = null;
    let hasDetectedSpeechInRound = false;

    // Web Audio Analyser for True Live Mic Level
    let audioContext = null;
    let mediaStream = null;
    let analyserNode = null;
    let animFrameId = null;
    let lastVolumeSentTime = 0;
    let lastReportedVolume = 0.0;
    const VOLUME_THROTTLE_MS = 75; // ~13 updates/sec to keep Blazor WebAssembly thread snappy

    // Current State Tracker
    let currentVoiceState = "idle";

    /**
     * Updates and reports the state to C# Blazor layer.
     * States: 'idle', 'requesting_permission', 'listening', 'processing',
     *         'success', 'no_speech', 'permission_denied', 'unsupported', 'error'
     */
    function reportVoiceState(state) {
        currentVoiceState = state;
        if (voiceDotNetRef) {
            try {
                voiceDotNetRef.invokeMethodAsync("OnJsVoiceStateChanged", state);
            } catch (err) {
                console.warn("[VoxieVoice] Failed to report state to C#:", err);
            }
        }
    }

    /**
     * Determines optimal speech recognition locale for the user.
     * Supports Indian English accents, UK, and US cleanly.
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
     * Detects mobile browser environment (Android, iOS Safari, etc.)
     */
    function isMobileBrowser() {
        return /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);
    }

    /**
     * Resets the silence watchdog timer (alerts if child hasn't spoken after 6 seconds of listening).
     */
    function resetSilenceWatchdog() {
        clearSilenceWatchdog();
        if (isManuallyStopped || isSpeakingActive) return;

        silenceWatchdogTimer = setTimeout(function () {
            if (!isManuallyStopped && !isSpeakingActive && !hasDetectedSpeechInRound && currentVoiceState === "listening") {
                reportVoiceState("no_speech");
            }
        }, 6000);
    }

    function clearSilenceWatchdog() {
        if (silenceWatchdogTimer) {
            clearTimeout(silenceWatchdogTimer);
            silenceWatchdogTimer = null;
        }
    }

    window.voxieVoiceService = {
        /**
         * Checks whether speech recognition and mediaDevices are supported in this browser.
         */
        isSupported: function () {
            const hasSpeech = Boolean(window.SpeechRecognition || window.webkitSpeechRecognition);
            const hasMedia = Boolean(navigator.mediaDevices && navigator.mediaDevices.getUserMedia);
            return hasSpeech && hasMedia;
        },

        /**
         * Explicitly requests microphone permission with user gesture.
         */
        requestPermission: async function () {
            try {
                if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
                    reportVoiceState("unsupported");
                    return false;
                }
                reportVoiceState("requesting_permission");
                const stream = await navigator.mediaDevices.getUserMedia({
                    audio: {
                        echoCancellation: true,
                        noiseSuppression: true,
                        autoGainControl: true
                    }
                });
                // Keep permission granted and close test stream
                stream.getTracks().forEach(t => t.stop());
                reportVoiceState("idle");
                return true;
            } catch (err) {
                console.warn("[VoxieVoice] Microphone permission denied:", err);
                if (err.name === "NotAllowedError" || err.name === "PermissionDeniedError") {
                    reportVoiceState("permission_denied");
                } else {
                    reportVoiceState("error");
                }
                return false;
            }
        },

        /**
         * Ensures microphone permission is granted, then immediately starts active listening.
         */
        ensurePermissionAndStartListening: async function (dotNetHelper, pushToTalk) {
            voiceDotNetRef = dotNetHelper || voiceDotNetRef;

            const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
            if (!SpeechRecognition) {
                reportVoiceState("unsupported");
                if (voiceDotNetRef) {
                    voiceDotNetRef.invokeMethodAsync("OnJsError", "Speech recognition is not supported in this browser. Use Chrome, Edge, Safari, or touch buttons!");
                }
                return false;
            }

            try {
                if (navigator.mediaDevices && navigator.mediaDevices.getUserMedia) {
                    reportVoiceState("requesting_permission");
                    // Acquire audio stream directly for volume meter and permission verification
                    await window.voxieVoiceService.startVolumeMeter(voiceDotNetRef);
                }
            } catch (err) {
                console.warn("[VoxieVoice] Microphone permission denied or blocked:", err);
                if (err.name === "NotAllowedError" || err.name === "PermissionDeniedError") {
                    reportVoiceState("permission_denied");
                    if (voiceDotNetRef) {
                        voiceDotNetRef.invokeMethodAsync("OnJsError", "Microphone access is blocked! Tap the lock icon in your address bar to allow, or use the touch buttons below.");
                    }
                } else {
                    reportVoiceState("error");
                    if (voiceDotNetRef) {
                        voiceDotNetRef.invokeMethodAsync("OnJsError", "Microphone could not be started (" + err.message + "). Try again or use touch buttons!");
                    }
                }
                return false;
            }

            window.voxieVoiceService.startListening(dotNetHelper, pushToTalk);
            return true;
        },

        /**
         * Starts speech recognition with robust multi-alternative capture and mobile-resilient auto-restart.
         */
        startListening: function (dotNetHelper, pushToTalk) {
            voiceDotNetRef = dotNetHelper || voiceDotNetRef;
            isManuallyStopped = false;
            isPushToTalkMode = !!pushToTalk;
            hasDetectedSpeechInRound = false;

            if (restartTimerId) {
                clearTimeout(restartTimerId);
                restartTimerId = null;
            }

            // If Voxie is currently speaking a prompt aloud, queue listening so speech doesn't hear Voxie's voice
            if (isSpeakingActive) {
                pendingListenRequest = true;
                return;
            }

            const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
            if (!SpeechRecognition) {
                reportVoiceState("unsupported");
                if (voiceDotNetRef) {
                    voiceDotNetRef.invokeMethodAsync("OnJsError", "Web Speech API is not supported in this browser. Please use Chrome, Edge, Safari, or tap/type your answer!");
                }
                return;
            }

            if (recognition) {
                try { recognition.abort(); } catch (_) { }
                recognition = null;
            }

            try {
                recognition = new SpeechRecognition();
                // On mobile devices, continuous=false prevents silent engine stalls and premature termination
                const isMobile = isMobileBrowser();
                recognition.continuous = !isPushToTalkMode && !isMobile;
                recognition.interimResults = true;
                recognition.lang = getOptimalSpeechLang();
                recognition.maxAlternatives = 5;

                recognition.onstart = function () {
                    reportVoiceState("listening");
                    resetSilenceWatchdog();
                    if (voiceDotNetRef) {
                        voiceDotNetRef.invokeMethodAsync("OnJsListeningStatus", true);
                    }
                };

                recognition.onspeechstart = function () {
                    hasDetectedSpeechInRound = true;
                    clearSilenceWatchdog();
                    reportVoiceState("listening");
                };

                recognition.onspeechend = function () {
                    reportVoiceState("processing");
                };

                recognition.onresult = function (event) {
                    if (!event.results || event.results.length === 0) return;

                    hasDetectedSpeechInRound = true;
                    clearSilenceWatchdog();

                    for (let i = event.resultIndex; i < event.results.length; ++i) {
                        const res = event.results[i];
                        if (!res || res.length === 0) continue;

                        const isFinal = res.isFinal;
                        const alternatives = [];

                        // Collect candidate transcriptions from the acoustic model
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
                            if (isFinal) {
                                if (speechDebounceTimer) {
                                    clearTimeout(speechDebounceTimer);
                                    speechDebounceTimer = null;
                                }
                                reportVoiceState("processing");
                                voiceDotNetRef.invokeMethodAsync("OnJsSpeechResultWithAlternatives", topTranscript, joinedAlts, confidence, true);
                            } else {
                                // Immediate interim delivery for instant visual response
                                reportVoiceState("processing");
                                voiceDotNetRef.invokeMethodAsync("OnJsSpeechResultWithAlternatives", topTranscript, joinedAlts, confidence, false);

                                // If user stops speaking and browser holds back final event, auto-finalize after 850ms of quiet
                                if (speechDebounceTimer) clearTimeout(speechDebounceTimer);
                                speechDebounceTimer = setTimeout(function () {
                                    speechDebounceTimer = null;
                                    if (voiceDotNetRef && !isSpeakingActive && !isManuallyStopped) {
                                        reportVoiceState("processing");
                                        voiceDotNetRef.invokeMethodAsync("OnJsSpeechResultWithAlternatives", topTranscript, joinedAlts, confidence, true);
                                    }
                                }, 850);
                            }
                        }
                    }
                };

                recognition.onerror = function (event) {
                    if (event.error === "no-speech") {
                        if (!hasDetectedSpeechInRound) {
                            reportVoiceState("no_speech");
                        }
                        return;
                    }
                    if (event.error === "aborted") {
                        return;
                    }

                    console.warn("[VoxieVoice] Recognition error:", event.error);
                    if (event.error === "not-allowed" || event.error === "service-not-allowed") {
                        reportVoiceState("permission_denied");
                        if (voiceDotNetRef) {
                            voiceDotNetRef.invokeMethodAsync("OnJsError", "Microphone access blocked. Please enable mic permissions or tap/type your answer!");
                        }
                    } else if (event.error === "network") {
                        reportVoiceState("error");
                        if (voiceDotNetRef) {
                            voiceDotNetRef.invokeMethodAsync("OnJsError", "Speech network error. Tap mic to retry or use written answer!");
                        }
                    } else {
                        reportVoiceState("error");
                    }
                };

                recognition.onend = function () {
                    if (speechDebounceTimer) {
                        clearTimeout(speechDebounceTimer);
                        speechDebounceTimer = null;
                    }

                    // Auto-restart for smooth continuous gameplay when active
                    if (!isManuallyStopped && !isPushToTalkMode && !isSpeakingActive) {
                        if (restartTimerId) clearTimeout(restartTimerId);
                        restartTimerId = setTimeout(function () {
                            if (!isManuallyStopped && !isSpeakingActive) {
                                // Create fresh instance to prevent InvalidStateError
                                window.voxieVoiceService.startListening(voiceDotNetRef, isPushToTalkMode);
                            }
                        }, 200);
                        return;
                    }

                    if (voiceDotNetRef) {
                        voiceDotNetRef.invokeMethodAsync("OnJsListeningStatus", false);
                    }
                    if (isManuallyStopped) {
                        reportVoiceState("idle");
                    }
                };

                recognition.start();
            } catch (err) {
                console.warn("[VoxieVoice] Failed to start recognition:", err);
                reportVoiceState("error");
            }

            // Ensure volume meter is capturing audio
            window.voxieVoiceService.startVolumeMeter(dotNetHelper);
        },

        /**
         * Stops active speech recognition, clears timers, and releases audio hardware completely.
         */
        stopListening: function () {
            isManuallyStopped = true;
            clearSilenceWatchdog();

            if (speechDebounceTimer) {
                clearTimeout(speechDebounceTimer);
                speechDebounceTimer = null;
            }
            if (restartTimerId) {
                clearTimeout(restartTimerId);
                restartTimerId = null;
            }
            if (recognition) {
                try { recognition.abort(); } catch (_) { }
                recognition = null;
            }

            reportVoiceState("idle");
            if (voiceDotNetRef) {
                voiceDotNetRef.invokeMethodAsync("OnJsListeningStatus", false);
            }
            window.voxieVoiceService.stopVolumeMeter();
        },

        /**
         * Starts Web Audio AnalyserNode to compute true 0.0 - 1.0 volume levels from real mic input.
         */
        startVolumeMeter: async function (dotNetHelper) {
            voiceDotNetRef = dotNetHelper || voiceDotNetRef;

            if (analyserNode && mediaStream && mediaStream.active) {
                return;
            }

            try {
                if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
                    return;
                }

                // Close any stale stream first
                if (mediaStream) {
                    mediaStream.getTracks().forEach(t => t.stop());
                    mediaStream = null;
                }

                mediaStream = await navigator.mediaDevices.getUserMedia({
                    audio: {
                        echoCancellation: true,
                        noiseSuppression: true,
                        autoGainControl: true
                    },
                    video: false
                });

                const AudioContextClass = window.AudioContext || window.webkitAudioContext;
                if (!AudioContextClass) return;

                if (!audioContext || audioContext.state === "closed") {
                    audioContext = new AudioContextClass();
                }

                if (audioContext.state === "suspended") {
                    await audioContext.resume();
                }

                const source = audioContext.createMediaStreamSource(mediaStream);
                analyserNode = audioContext.createAnalyser();
                analyserNode.fftSize = 128;
                analyserNode.smoothingTimeConstant = 0.35;
                source.connect(analyserNode);

                const dataArray = new Uint8Array(analyserNode.frequencyBinCount);

                function pollVolume() {
                    if (!analyserNode || !mediaStream || !mediaStream.active) return;

                    analyserNode.getByteTimeDomainData(dataArray);

                    let sumSquares = 0;
                    for (let i = 0; i < dataArray.length; i++) {
                        const normalized = (dataArray[i] - 128) / 128;
                        sumSquares += normalized * normalized;
                    }
                    const rms = Math.sqrt(sumSquares / dataArray.length);

                    let volume = rms * 3.6;
                    if (volume < 0.04) volume = 0.0;
                    volume = Math.min(1.0, Math.max(0.0, volume));

                    // If audible sound is detected, reset silence watchdog
                    if (volume > 0.08) {
                        hasDetectedSpeechInRound = true;
                        clearSilenceWatchdog();
                    }

                    const now = performance.now();
                    if (now - lastVolumeSentTime >= VOLUME_THROTTLE_MS || (volume === 0 && lastReportedVolume > 0)) {
                        lastVolumeSentTime = now;
                        lastReportedVolume = volume;
                        if (voiceDotNetRef) {
                            voiceDotNetRef.invokeMethodAsync("OnJsVolumeChanged", volume);
                        }
                    }

                    animFrameId = requestAnimationFrame(pollVolume);
                }

                pollVolume();
            } catch (err) {
                console.warn("[VoxieVoice] Web Audio Analyser fallback:", err);
            }
        },

        /**
         * Shuts down volume meter and cleanly releases the physical audio hardware stream.
         */
        stopVolumeMeter: function () {
            if (animFrameId) {
                cancelAnimationFrame(animFrameId);
                animFrameId = null;
            }
            if (mediaStream) {
                mediaStream.getTracks().forEach(t => {
                    try { t.stop(); } catch (_) { }
                });
                mediaStream = null;
            }
            if (audioContext && audioContext.state !== "closed") {
                try { audioContext.close(); } catch (_) { }
                audioContext = null;
            }
            analyserNode = null;
            lastReportedVolume = 0.0;

            if (voiceDotNetRef) {
                try {
                    voiceDotNetRef.invokeMethodAsync("OnJsVolumeChanged", 0.0);
                } catch (_) { }
            }
        },

        /**
         * Sets state explicitly from C# (e.g., when answer is verified correct -> 'success').
         */
        setState: function (state) {
            reportVoiceState(state);
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
                const friendly = voices.find(v => v.lang.startsWith("en") &&
                    (v.name.includes("Natural") || v.name.includes("Samantha") || v.name.includes("Google") || v.name.includes("India")));
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

                    if (pendingListenRequest && !isManuallyStopped) {
                        pendingListenRequest = false;
                        setTimeout(function () {
                            if (!isManuallyStopped && !isSpeakingActive) {
                                window.voxieVoiceService.startListening(voiceDotNetRef, isPushToTalkMode);
                            }
                        }, 200);
                    }
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
