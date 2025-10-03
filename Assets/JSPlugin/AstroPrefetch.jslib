var AstroPrefetchLib = {
    $ap_Preload: function(url, type) {
        const element = document.createElement("link");
        element.href = url;
        element.rel = "preload";
        element.as = type;
        element.crossOrigin = "anonymous";
        element.fetchPriority = "low";

        const loaded = function() {
            console.log("[AstroPrefetch] Link", url, "finished preloading");
            element.onload = element.onerror = null;
            element.remove();
        };

        const errored = function() {
            console.warn("[AstroPrefetch] Link", url, "failed to preload");
            element.onload = element.onerror = null;
            element.remove();
        };

        element.onload = loaded;
        element.onerror = errored;
        document.head.appendChild(element);
    },

    AstroPrefetch_Video__deps: ["$ap_Preload"],
    AstroPrefetch_Video__sig: "vi",
    AstroPrefetch_Video: function(url) {
        const urlString = UTF8ToString(url);
        ap_Preload(urlString, "fetch");
    },

    AstroPrefetch_Audio__deps: ["$ap_Preload"],
    AstroPrefetch_Audio__sig: "vi",
    AstroPrefetch_Audio: function(url) {
        const urlString = UTF8ToString(url);
        ap_Preload(urlString, "fetch");
    },

    AstroPrefetch_Texture__deps: ["$ap_Preload"],
    AstroPrefetch_Texture__sig: "vi",
    AstroPrefetch_Texture: function(url) {
        const urlString = UTF8ToString(url);
        ap_Preload(urlString, "image");
    },

    AstroPrefetch_File__deps: ["$ap_Preload"],
    AstroPrefetch_File__sig: "vi",
    AstroPrefetch_File: function(url) {
        const urlString = UTF8ToString(url);
        ap_Preload(urlString, "fetch");
    },
}

mergeInto(LibraryManager.library, AstroPrefetchLib);