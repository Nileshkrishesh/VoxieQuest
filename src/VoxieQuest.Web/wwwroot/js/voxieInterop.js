// Voxie Quest - Thin JavaScript Interop Bridge
// Dedicated exclusively to browser Speech, Audio, and LocalStorage APIs

(function () {
    // ------------------------------------------------------------------------
    // 1. Safe LocalStorage Wrapper
    // ------------------------------------------------------------------------
    window.voxieStorage = {
        getItem: function (key) {
            try {
                return localStorage.getItem(key);
            } catch (e) {
                console.warn("[VoxieStorage] Read failed", e);
                return null;
            }
        },
        setItem: function (key, value) {
            try {
                localStorage.setItem(key, value);
            } catch (e) {
                console.warn("[VoxieStorage] Write failed", e);
            }
        },
        removeItem: function (key) {
            try {
                localStorage.removeItem(key);
            } catch (e) {
                console.warn("[VoxieStorage] Remove failed", e);
            }
        }
    };

    // ------------------------------------------------------------------------
    // 2. Child-Safe Web Speech API Wrapper
    // ------------------------------------------------------------------------
    let recognition = null;
    let voiceDotNetRef = null;
    let isManuallyStopped = false;

    window.voxieVoice = {
        startListening: function (dotNetHelper) {
            voiceDotNetRef = dotNetHelper;
            isManuallyStopped = false;

            const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
            if (!SpeechRecognition) {
                if (voiceDotNetRef) {
                    voiceDotNetRef.invokeMethodAsync("OnJsSpeechError", "Speech recognition is not supported in this browser. Try Chrome, Edge, or Safari.");
                }
                return;
            }

            if (recognition) {
                try { recognition.abort(); } catch (_) { }
            }

            recognition = new SpeechRecognition();
            recognition.continuous = true;
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
                    voiceDotNetRef.invokeMethodAsync("OnJsSpeechRecognized", transcript, confidence, isFinal);
                }
            };

            recognition.onerror = function (event) {
                // Ignore harmless 'no-speech' events while child is thinking
                if (event.error === "no-speech") return;

                if (voiceDotNetRef) {
                    voiceDotNetRef.invokeMethodAsync("OnJsSpeechError", "Microphone: " + event.error);
                }
            };

            recognition.onend = function () {
                if (!isManuallyStopped && recognition) {
                    // Automatically restart continuous listening for smooth children's gameplay
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
                    voiceDotNetRef.invokeMethodAsync("OnJsSpeechError", "Could not start microphone: " + err.message);
                }
            }
        },

        stopListening: function () {
            isManuallyStopped = true;
            if (recognition) {
                try {
                    recognition.stop();
                } catch (_) { }
                recognition = null;
            }
            if (voiceDotNetRef) {
                voiceDotNetRef.invokeMethodAsync("OnJsListeningStatus", false);
            }
        },

        speak: function (text, rate, pitch, dotNetHelper) {
            if (!window.speechSynthesis) return;

            window.speechSynthesis.cancel(); // Stop any pending utterance
            const utterance = new SpeechSynthesisUtterance(text);
            utterance.rate = rate || 0.9;
            utterance.pitch = pitch || 1.1;
            utterance.lang = "en-US";

            // Select a friendly English voice if available
            const voices = window.speechSynthesis.getVoices();
            const childFriendly = voices.find(v => v.lang.startsWith("en") && (v.name.includes("Natural") || v.name.includes("Samantha") || v.name.includes("Google")));
            if (childFriendly) {
                utterance.voice = childFriendly;
            }

            utterance.onend = function () {
                if (dotNetHelper) {
                    dotNetHelper.invokeMethodAsync("OnJsSpeechFinished");
                }
            };

            utterance.onerror = function () {
                if (dotNetHelper) {
                    dotNetHelper.invokeMethodAsync("OnJsSpeechFinished");
                }
            };

            window.speechSynthesis.speak(utterance);
        },

        stopSpeaking: function () {
            if (window.speechSynthesis) {
                window.speechSynthesis.cancel();
            }
        }
    };

    // ------------------------------------------------------------------------
    // 3. Procedural Web Audio API Sound Synthesizer
    // ------------------------------------------------------------------------
    let audioCtx = null;
    let masterGain = null;
    let isAudioMuted = false;
    let currentVolume = 0.8;

    function getAudioContext() {
        if (!audioCtx) {
            const AudioContextClass = window.AudioContext || window.webkitAudioContext;
            if (AudioContextClass) {
                audioCtx = new AudioContextClass();
                masterGain = audioCtx.createGain();
                masterGain.gain.setValueAtTime(currentVolume, audioCtx.currentTime);
                masterGain.connect(audioCtx.destination);
            }
        }
        if (audioCtx && audioCtx.state === "suspended") {
            audioCtx.resume();
        }
        return audioCtx;
    }

    window.voxieAudio = {
        playEffect: function (effectName, volumeMultiplier) {
            if (isAudioMuted) return;
            const ctx = getAudioContext();
            if (!ctx) return;

            const now = ctx.currentTime;
            const gain = ctx.createGain();
            const effVol = (volumeMultiplier !== undefined ? volumeMultiplier : currentVolume) * (isAudioMuted ? 0 : 1);
            gain.gain.setValueAtTime(effVol, now);
            gain.connect(ctx.destination);

            switch (effectName) {
                case "click":
                    // Crisp wooden button tap
                    {
                        const osc = ctx.createOscillator();
                        osc.type = "sine";
                        osc.frequency.setValueAtTime(440, now);
                        osc.frequency.exponentialRampToValueAtTime(120, now + 0.06);
                        gain.gain.setValueAtTime(0.3 * effVol, now);
                        gain.gain.exponentialRampToValueAtTime(0.001, now + 0.06);
                        osc.connect(gain);
                        osc.start(now);
                        osc.stop(now + 0.06);
                    }
                    break;

                case "pop":
                    // Playful bubble pop
                    {
                        const osc = ctx.createOscillator();
                        osc.type = "sine";
                        osc.frequency.setValueAtTime(320, now);
                        osc.frequency.exponentialRampToValueAtTime(800, now + 0.08);
                        gain.gain.setValueAtTime(0.4 * effVol, now);
                        gain.gain.exponentialRampToValueAtTime(0.001, now + 0.08);
                        osc.connect(gain);
                        osc.start(now);
                        osc.stop(now + 0.08);
                    }
                    break;

                case "success":
                    // Harmonious joyful two-tone chime (E5 -> G#5 -> B5)
                    {
                        const notes = [659.25, 830.61, 987.77];
                        notes.forEach((freq, idx) => {
                            const osc = ctx.createOscillator();
                            const noteGain = ctx.createGain();
                            const startTime = now + idx * 0.09;
                            osc.type = "triangle";
                            osc.frequency.setValueAtTime(freq, startTime);
                            noteGain.gain.setValueAtTime(0.35 * effVol, startTime);
                            noteGain.gain.exponentialRampToValueAtTime(0.001, startTime + 0.45);
                            osc.connect(noteGain);
                            noteGain.connect(gain);
                            osc.start(startTime);
                            osc.stop(startTime + 0.45);
                        });
                    }
                    break;

                case "encourage":
                    // Gentle curious low-to-mid chime
                    {
                        const osc = ctx.createOscillator();
                        osc.type = "sine";
                        osc.frequency.setValueAtTime(340, now);
                        osc.frequency.exponentialRampToValueAtTime(440, now + 0.25);
                        gain.gain.setValueAtTime(0.3 * effVol, now);
                        gain.gain.exponentialRampToValueAtTime(0.001, now + 0.35);
                        osc.connect(gain);
                        osc.start(now);
                        osc.stop(now + 0.35);
                    }
                    break;

                case "star":
                    // Twinkling upward sparkle
                    {
                        const sparkleFreqs = [523.25, 659.25, 783.99, 1046.50];
                        sparkleFreqs.forEach((freq, idx) => {
                            const osc = ctx.createOscillator();
                            const noteGain = ctx.createGain();
                            const startTime = now + idx * 0.07;
                            osc.type = "sine";
                            osc.frequency.setValueAtTime(freq, startTime);
                            noteGain.gain.setValueAtTime(0.3 * effVol, startTime);
                            noteGain.gain.exponentialRampToValueAtTime(0.001, startTime + 0.3);
                            osc.connect(noteGain);
                            noteGain.connect(gain);
                            osc.start(startTime);
                            osc.stop(startTime + 0.3);
                        });
                    }
                    break;

                case "fanfare":
                    // Triumphant 5-note brass fanfare
                    {
                        const fanfare = [392.00, 523.25, 659.25, 783.99, 1046.50];
                        fanfare.forEach((freq, idx) => {
                            const osc = ctx.createOscillator();
                            const noteGain = ctx.createGain();
                            const startTime = now + idx * 0.12;
                            osc.type = "triangle";
                            osc.frequency.setValueAtTime(freq, startTime);
                            const duration = (idx === fanfare.length - 1) ? 0.7 : 0.25;
                            noteGain.gain.setValueAtTime(0.4 * effVol, startTime);
                            noteGain.gain.exponentialRampToValueAtTime(0.001, startTime + duration);
                            osc.connect(noteGain);
                            noteGain.connect(gain);
                            osc.start(startTime);
                            osc.stop(startTime + duration);
                        });
                    }
                    break;
            }
        },

        setMuted: function (muted) {
            isAudioMuted = !!muted;
        },

        setVolume: function (vol) {
            currentVolume = Math.max(0, Math.min(1, vol));
            if (masterGain && audioCtx) {
                masterGain.gain.setValueAtTime(currentVolume, audioCtx.currentTime);
            }
        },

        playAnimalSound: function (animalName, volumeMultiplier) {
            if (isAudioMuted) return;
            const normAnimal = (animalName || "").toLowerCase().trim();
            const effVol = (volumeMultiplier !== undefined ? volumeMultiplier : currentVolume) * (isAudioMuted ? 0 : 1);

            // First: Attempt to play static audio file if present in wwwroot/audio/animals/
            try {
                const audio = new Audio("audio/animals/" + normAnimal + ".mp3");
                audio.volume = effVol;
                const playPromise = audio.play();
                if (playPromise !== undefined) {
                    playPromise.then(() => {
                        // Successfully played audio file!
                    }).catch(() => {
                        // Audio file missing or blocked -> synthesize procedurally
                        synthesizeProceduralAnimal(normAnimal, effVol);
                    });
                    return;
                }
            } catch (_) {
                // Ignore and proceed to procedural synthesis
            }

            synthesizeProceduralAnimal(normAnimal, effVol);
        }
    };

    function synthesizeProceduralAnimal(animal, vol) {
        const ctx = getAudioContext();
        if (!ctx) return;
        const now = ctx.currentTime;
        const out = ctx.createGain();
        out.gain.setValueAtTime(vol, now);
        out.connect(ctx.destination);

        switch (animal) {
            case "cat":
            case "kitten":
                // Playful cat meow: frequency sweeps down and slightly back up
                {
                    const osc = ctx.createOscillator();
                    const g = ctx.createGain();
                    osc.type = "sine";
                    osc.frequency.setValueAtTime(680, now);
                    osc.frequency.linearRampToValueAtTime(840, now + 0.15);
                    osc.frequency.exponentialRampToValueAtTime(520, now + 0.55);
                    g.gain.setValueAtTime(0.01, now);
                    g.gain.linearRampToValueAtTime(0.4 * vol, now + 0.12);
                    g.gain.exponentialRampToValueAtTime(0.001, now + 0.6);
                    osc.connect(g);
                    g.connect(out);
                    osc.start(now);
                    osc.stop(now + 0.6);
                }
                break;

            case "dog":
            case "puppy":
                // Cheerful double bark
                [0, 0.18].forEach(delay => {
                    const t = now + delay;
                    const osc = ctx.createOscillator();
                    const g = ctx.createGain();
                    osc.type = "triangle";
                    osc.frequency.setValueAtTime(260, t);
                    osc.frequency.exponentialRampToValueAtTime(110, t + 0.12);
                    g.gain.setValueAtTime(0.45 * vol, t);
                    g.gain.exponentialRampToValueAtTime(0.001, t + 0.12);
                    osc.connect(g);
                    g.connect(out);
                    osc.start(t);
                    osc.stop(t + 0.12);
                });
                break;

            case "cow":
                // Deep resonant moo
                {
                    const osc = ctx.createOscillator();
                    const g = ctx.createGain();
                    const filter = ctx.createBiquadFilter();
                    filter.type = "lowpass";
                    filter.frequency.setValueAtTime(400, now);
                    osc.type = "sawtooth";
                    osc.frequency.setValueAtTime(135, now);
                    osc.frequency.linearRampToValueAtTime(110, now + 0.7);
                    g.gain.setValueAtTime(0.05, now);
                    g.gain.linearRampToValueAtTime(0.4 * vol, now + 0.15);
                    g.gain.exponentialRampToValueAtTime(0.001, now + 0.75);
                    osc.connect(filter);
                    filter.connect(g);
                    g.connect(out);
                    osc.start(now);
                    osc.stop(now + 0.75);
                }
                break;

            case "duck":
                // Quack quack! Nasal double tone
                [0, 0.16].forEach(delay => {
                    const t = now + delay;
                    const osc = ctx.createOscillator();
                    const g = ctx.createGain();
                    const filter = ctx.createBiquadFilter();
                    filter.type = "bandpass";
                    filter.frequency.setValueAtTime(650, t);
                    filter.Q.setValueAtTime(3.0, t);
                    osc.type = "sawtooth";
                    osc.frequency.setValueAtTime(380, t);
                    osc.frequency.exponentialRampToValueAtTime(280, t + 0.13);
                    g.gain.setValueAtTime(0.4 * vol, t);
                    g.gain.exponentialRampToValueAtTime(0.001, t + 0.13);
                    osc.connect(filter);
                    filter.connect(g);
                    g.connect(out);
                    osc.start(t);
                    osc.stop(t + 0.13);
                });
                break;

            case "frog":
                // Ribbit ribbit!
                [0, 0.14].forEach(delay => {
                    const t = now + delay;
                    const osc = ctx.createOscillator();
                    const g = ctx.createGain();
                    osc.type = "sawtooth";
                    osc.frequency.setValueAtTime(160, t);
                    osc.frequency.exponentialRampToValueAtTime(260, t + 0.08);
                    osc.frequency.exponentialRampToValueAtTime(140, t + 0.12);
                    g.gain.setValueAtTime(0.35 * vol, t);
                    g.gain.exponentialRampToValueAtTime(0.001, t + 0.12);
                    osc.connect(g);
                    g.connect(out);
                    osc.start(t);
                    osc.stop(t + 0.12);
                });
                break;

            case "sheep":
                // Baaa with playful vibrato
                {
                    const osc = ctx.createOscillator();
                    const g = ctx.createGain();
                    const vib = ctx.createOscillator();
                    const vibGain = ctx.createGain();
                    vib.frequency.setValueAtTime(8, now); // 8 Hz tremolo
                    vibGain.gain.setValueAtTime(18, now);
                    vib.connect(osc.frequency);
                    osc.type = "triangle";
                    osc.frequency.setValueAtTime(290, now);
                    g.gain.setValueAtTime(0.05, now);
                    g.gain.linearRampToValueAtTime(0.4 * vol, now + 0.1);
                    g.gain.exponentialRampToValueAtTime(0.001, now + 0.65);
                    osc.connect(g);
                    g.connect(out);
                    vib.start(now);
                    osc.start(now);
                    vib.stop(now + 0.65);
                    osc.stop(now + 0.65);
                }
                break;

            case "pig":
                // Oink oink!
                [0, 0.15].forEach(delay => {
                    const t = now + delay;
                    const osc = ctx.createOscillator();
                    const g = ctx.createGain();
                    osc.type = "sawtooth";
                    osc.frequency.setValueAtTime(140, t);
                    osc.frequency.exponentialRampToValueAtTime(190, t + 0.06);
                    osc.frequency.exponentialRampToValueAtTime(110, t + 0.12);
                    g.gain.setValueAtTime(0.35 * vol, t);
                    g.gain.exponentialRampToValueAtTime(0.001, t + 0.12);
                    osc.connect(g);
                    g.connect(out);
                    osc.start(t);
                    osc.stop(t + 0.12);
                });
                break;

            case "bird":
            case "rooster":
                // Chirping trills
                [0, 0.1, 0.22].forEach((delay, idx) => {
                    const t = now + delay;
                    const osc = ctx.createOscillator();
                    const g = ctx.createGain();
                    osc.type = "sine";
                    const startF = 2100 + idx * 200;
                    osc.frequency.setValueAtTime(startF, t);
                    osc.frequency.exponentialRampToValueAtTime(startF + 600, t + 0.05);
                    osc.frequency.exponentialRampToValueAtTime(startF - 300, t + 0.09);
                    g.gain.setValueAtTime(0.35 * vol, t);
                    g.gain.exponentialRampToValueAtTime(0.001, t + 0.09);
                    osc.connect(g);
                    g.connect(out);
                    osc.start(t);
                    osc.stop(t + 0.09);
                });
                break;

            case "lion":
            case "tiger":
            case "bear":
                // Mighty rumble roar
                {
                    const osc = ctx.createOscillator();
                    const g = ctx.createGain();
                    const filter = ctx.createBiquadFilter();
                    filter.type = "lowpass";
                    filter.frequency.setValueAtTime(250, now);
                    osc.type = "sawtooth";
                    osc.frequency.setValueAtTime(160, now);
                    osc.frequency.linearRampToValueAtTime(70, now + 0.65);
                    g.gain.setValueAtTime(0.1, now);
                    g.gain.linearRampToValueAtTime(0.5 * vol, now + 0.1);
                    g.gain.exponentialRampToValueAtTime(0.001, now + 0.7);
                    osc.connect(filter);
                    filter.connect(g);
                    g.connect(out);
                    osc.start(now);
                    osc.stop(now + 0.7);
                }
                break;

            case "owl":
                // Soft double hoot
                [0, 0.28].forEach((delay, idx) => {
                    const t = now + delay;
                    const osc = ctx.createOscillator();
                    const g = ctx.createGain();
                    osc.type = "sine";
                    osc.frequency.setValueAtTime(idx === 0 ? 340 : 300, t);
                    g.gain.setValueAtTime(0.01, t);
                    g.gain.linearRampToValueAtTime(0.35 * vol, t + 0.08);
                    g.gain.exponentialRampToValueAtTime(0.001, t + 0.24);
                    osc.connect(g);
                    g.connect(out);
                    osc.start(t);
                    osc.stop(t + 0.24);
                });
                break;

            case "elephant":
                // Brass trumpet sweep
                {
                    const osc = ctx.createOscillator();
                    const g = ctx.createGain();
                    osc.type = "sawtooth";
                    osc.frequency.setValueAtTime(360, now);
                    osc.frequency.linearRampToValueAtTime(560, now + 0.25);
                    osc.frequency.exponentialRampToValueAtTime(420, now + 0.6);
                    g.gain.setValueAtTime(0.05, now);
                    g.gain.linearRampToValueAtTime(0.45 * vol, now + 0.15);
                    g.gain.exponentialRampToValueAtTime(0.001, now + 0.65);
                    osc.connect(g);
                    g.connect(out);
                    osc.start(now);
                    osc.stop(now + 0.65);
                }
                break;

            case "monkey":
                // Ooh ooh aah aah chatter
                [0, 0.12, 0.24].forEach((delay, idx) => {
                    const t = now + delay;
                    const osc = ctx.createOscillator();
                    const g = ctx.createGain();
                    osc.type = "sine";
                    osc.frequency.setValueAtTime(500 + idx * 100, t);
                    osc.frequency.exponentialRampToValueAtTime(750 + idx * 100, t + 0.08);
                    g.gain.setValueAtTime(0.35 * vol, t);
                    g.gain.exponentialRampToValueAtTime(0.001, t + 0.09);
                    osc.connect(g);
                    g.connect(out);
                    osc.start(t);
                    osc.stop(t + 0.09);
                });
                break;

            case "wolf":
                // Majestic wolf howl
                {
                    const osc = ctx.createOscillator();
                    const g = ctx.createGain();
                    osc.type = "sine";
                    osc.frequency.setValueAtTime(260, now);
                    osc.frequency.linearRampToValueAtTime(480, now + 0.35);
                    osc.frequency.exponentialRampToValueAtTime(310, now + 0.8);
                    g.gain.setValueAtTime(0.01, now);
                    g.gain.linearRampToValueAtTime(0.4 * vol, now + 0.2);
                    g.gain.exponentialRampToValueAtTime(0.001, now + 0.85);
                    osc.connect(g);
                    g.connect(out);
                    osc.start(now);
                    osc.stop(now + 0.85);
                }
                break;

            case "bee":
                // Bzzzz sound
                {
                    const osc = ctx.createOscillator();
                    const g = ctx.createGain();
                    osc.type = "sawtooth";
                    osc.frequency.setValueAtTime(240, now);
                    g.gain.setValueAtTime(0.25 * vol, now);
                    g.gain.exponentialRampToValueAtTime(0.001, now + 0.45);
                    osc.connect(g);
                    g.connect(out);
                    osc.start(now);
                    osc.stop(now + 0.45);
                }
                break;

            default:
                // Gentle animal call chime
                {
                    const notes = [440, 554.37, 659.25];
                    notes.forEach((freq, idx) => {
                        const osc = ctx.createOscillator();
                        const noteGain = ctx.createGain();
                        const t = now + idx * 0.08;
                        osc.type = "triangle";
                        osc.frequency.setValueAtTime(freq, t);
                        noteGain.gain.setValueAtTime(0.3 * vol, t);
                        noteGain.gain.exponentialRampToValueAtTime(0.001, t + 0.3);
                        osc.connect(noteGain);
                        noteGain.connect(out);
                        osc.start(t);
                        osc.stop(t + 0.3);
                    });
                }
                break;
        }
    }
})();
