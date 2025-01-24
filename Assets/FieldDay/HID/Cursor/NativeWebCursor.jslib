var NativeWebCursorLib = {
    $NWCCache: {
        /**
         * @type {string}
         */
        canvasId: "#unity-canvas",
        
        /**
         * @type {HTMLCanvasElement}
         */
        canvasOverride: null
    },

    /**
     * Attempts to automatically find the appropriate canvas. 
     */
    NativeWebCursor_AutoFindCanvas: function() {
        var allCanvases = document.getElementsByTagName("canvas");
        for(var i = 0; i < allCanvases.length; i++) {
            var canvas = allCanvases[i];
            if (canvas.id.includes("unity") || (canvas.parentElement && canvas.parentElement.id.includes("unity"))) {
                NWCCache.canvasOverride = canvas;
                break;
            }
        }
    },

    /**
     * Sets the canvas id to look up.
     */
    NativeWebCursor_SetCanvasId: function(id) {
        NWCCache.canvasId = UTF8ToString(id);
        NWCCache.canvasOverride = null;
    },

    /**
     * Returns if the cursor is visible.
     */
    NativeWebCursor_IsVisible: function() {
        /** @type {HTMLCanvasElement} */
        var canvasElement = NWCCache.canvasOverride || document.getElementById(NWCCache.canvasId);
        if (canvasElement) {
            var style = getComputedStyle(canvasElement);
            return style.getPropertyValue("cursor") != "none";
        } else {
            return true;
        }
    },

    /**
     * Shows the default cursor.
     */
    NativeWebCursor_Show: function() {
        /** @type {HTMLCanvasElement} */
        var canvasElement = NWCCache.canvasOverride || document.getElementById(NWCCache.canvasId);
        if (canvasElement) {
            canvasElement.style.setProperty("cursor", "default", "important");
        }
    },

    /**
     * Shows a specific type of cursor.
     * @param {string} type
     */
    NativeWebCursor_ShowType: function(type) {
        /** @type {HTMLCanvasElement} */
        var canvasElement = NWCCache.canvasOverride || document.getElementById(NWCCache.canvasId);
        if (canvasElement) {
            canvasElement.style.setProperty("cursor", UTF8ToString(type), "important");
        }
    },

    /**
     * Hides the native cursor.
     */
    NativeWebCursor_Hide: function() {
        /** @type {HTMLCanvasElement} */
        var canvasElement = NWCCache.canvasOverride || document.getElementById(NWCCache.canvasId);
        if (canvasElement) {
            canvasElement.style.setProperty("cursor", "none", "important");
        }
    }
};

autoAddDeps(NativeWebCursorLib, "$NWCCache");
mergeInto(LibraryManager.library, NativeWebCursorLib);