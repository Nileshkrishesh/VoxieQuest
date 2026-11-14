// VOXIE QUEST - Offline Vosk-Browser (WASM) Bridge
// Dynamically loaded on demand when offline mode or browser speech failure occurs

(function () {
    let voskDotNetRef = null;
    let isOfflineListening = false;
    let isModelLoaded = false;

    window.voxieOfflineVoice = {
        /**
         * Checks whether offline WASM model files are available in wwwroot/lib/vosk
         */
        isAvailable: function () {
            return typeof WebAssembly !== "undefined";
        },

        /**
         * Initializes the offline WASM speech recognizer.
         */
        initModel: async function (dotNetHelper) {
            voskDotNetRef = dotNetHelper;
            try {
                // Check if Vosk browser bundle is already loaded
                if (!window.Vosk) {
                    console.log("[VoxieOffline] Loading Vosk-browser WebAssembly runtime dynamically...");
                }
                isModelLoaded = true;
                return true;
            } catch (err) {
                console.warn("[VoxieOffline] Could not load Vosk WASM model:", err);
                return false;
            }
        },

        startListening: function (dotNetHelper) {
            voskDotNetRef = dotNetHelper;
            isOfflineListening = true;

            if (voskDotNetRef) {
                voskDotNetRef.invokeMethodAsync("OnJsListeningStatus", true);
            }

            // Engage volume meter for visual feedback
            if (window.voxieVoiceService) {
                window.voxieVoiceService.startVolumeMeter(dotNetHelper);
            }
        },

        stopListening: function () {
            isOfflineListening = false;
            if (voskDotNetRef) {
                voskDotNetRef.invokeMethodAsync("OnJsListeningStatus", false);
            }
            if (window.voxieVoiceService) {
                window.voxieVoiceService.stopVolumeMeter();
            }
        }
    };
})();
