var NativePrefetchLib = {

    $NPCache: {
        /**
         * @type {Map<number, HTMLLinkElement | HTMLAudioElement>}
        */
        prefetchLinkMap: null,

        /**
         * @type {string[]}
         */
        resourceTypeStrings: [
            "fetch",
            "audio",
            "image",
            "video"
        ],

        /**
         * @type {Set<number>}
         */
        prefetchLinksLoaded: null,

        /**
         * @type {string}
         */
        prefetchCrossOriginSetting: "anonymous",

        /**
         * 
         */
        Initialize: function() {
            if (!NPCache.prefetchLinkMap) {
                NPCache.prefetchLinkMap = new Map();
            }
            if (!NPCache.prefetchLinksLoaded) {
                NPCache.prefetchLinksLoaded = new Set();
            }
        },

        /**
         * 
         * @param {string} url 
         * @param {number} identifier
         */
        NativePrefetchOnLoad: function(url, identifier) {
            if (!NPCache.prefetchLinkMap.has(identifier)) {
                return;
            }

            NPCache.prefetchLinksLoaded.add(identifier);
        },

        /**
         * 
         * @param {string} url 
         * @param {number} identifier
         */
        NativePrefetchOnError: function(url, identifier) {
            if (!NPCache.prefetchLinkMap.has(identifier)) {
                return;
            }

            NPCache.prefetchLinksLoaded.add(identifier);
            console.error("[NativePrefetch] Error when loading", url);
        },

        /**
         * @param {string} path 
         * @param {string} ext
         * @return {string}
         */
        ChangeExtension: function(path, ext) {
            const idx = path.lastIndexOf(".");
            if (idx >= 0) {
                return path.substring(0, idx) + ext;
            } else {
                return path + ext;
            }
        }
    },

    /**
     * Begins prefetching from the given url.
     * @param {string} url 
     * @param {number} resourceType
     * @param {number} identifier
     */
    NativePrefetch_Start__sig: 'viii',
    NativePrefetch_Start: function(url, resourceType, identifier) {
        NPCache.Initialize();

        /** @type {string} */
        var urlStr = Pointer_stringify(url);

        if (!NPCache.prefetchLinkMap.has(urlStr)) {
            var prefetchElement;
            if (resourceType == 1) { // audio loads via audio
                prefetchElement = new Audio();

                var oggSource = document.createElement("source");
                oggSource.src = NPCache.ChangeExtension(urlStr, ".ogg");
                oggSource.type = "audio/ogg";

                var mp3Source = document.createElement("source");
                mp3Source.src = NPCache.ChangeExtension(urlStr, ".mp3");
                mp3Source.type = "audio/mpeg";
                
                prefetchElement.appendChild(oggSource);
                prefetchElement.appendChild(mp3Source);

                prefetchElement.autoplay = false;
                prefetchElement.crossOrigin = NPCache.prefetchCrossOriginSetting;
                prefetchElement.load();
            } else { // everything else loads via link
                prefetchElement = document.createElement("link");
                prefetchElement.href = urlStr;
                prefetchElement.rel = "prefetch";
                prefetchElement.as = NPCache.resourceTypeStrings[resourceType | 0];
                prefetchElement.crossOrigin = NPCache.prefetchCrossOriginSetting;
            }

            prefetchElement._url = urlStr;

            NPCache.prefetchLinkMap.set(number, prefetchElement);
            document.body.appendChild(prefetchElement);

            if (resourceType != 1) {
                prefetchElement.onload = function() {
                    NPCache.NativePrefetchOnLoad(urlStr, identifier);
                };
                prefetchElement.onerror = function() {
                    NPCache.NativePrefetchOnError(urlStr, identifier);
                }
            }

            console.log("[NativePrefetch] Beginning prefetch of", urlStr);
        }
    },

    /**
     * Returns if the resource for the given identifier is prefetched.
     * @param {number} identifier
     */
    NativePrefetch_IsLoaded__sig: 'ii',
    NativePrefetch_IsLoaded: function(identifier) {
        
        if (NPCache.prefetchLinkMap && NPCache.prefetchLinkMap.has(identifier)) {
            var prefetchElement = NPCache.prefetchLinkMap.get(identifier);
            if (prefetchElement instanceof HTMLAudioElement) {
                return prefetchElement.readyState == 4;
            } else if (prefetchElement instanceof HTMLLinkElement) {
                return NPCache.prefetchLinksLoaded.has(identifier);
            } else {
                return false;
            }
        } else {
            return false;
        }
    },

    /**
     * Cancels the prefetch for resource of the given identifier.
     * @param {number} identifier
     */
    NativePrefetch_Cancel__sig: 'ii',
    NativePrefetch_Cancel: function (identifier) {
        if (NPCache.prefetchLinkMap && NPCache.prefetchLinkMap.has(identifier)) {
            var prefetchElement = NPCache.prefetchLinkMap.get(identifier);
            prefetchElement.onload = null;
            prefetchElement.onerror = null;
            prefetchElement.parentElement.removeChild(prefetchElement);
            NPCache.prefetchLinkMap.delete(urlStr);
            NPCache.prefetchLinksLoaded.delete(urlStr);

            console.log("[NativePrefetch] Canceling prefetch of", prefetchElement._url);
            return true;
        }

        return false;
    }
}

autoAddDeps(NativePrefetchLib, '$NPCache');
mergeInto(LibraryManager.library, NativePrefetchLib);