/**
 * @typedef ResourceGroup
 * @type {Object}
 * @property {number} assetCount
 * @property {number} loading
 * @property {number} loaded
 * @property {number} error
 */

/**
 * @typedef ResourceInfo
 * @type {Object}
 * @property {HTMLLinkElement | HTMLAudioElement} htmlElement
 * @property {number} identifier
 * @property {string} sourceUrl
 * @property {number} group
 * @property {0 | 1 | 2} state
 */

var NativePrefetchLib = {
    $NPCache: {
        // MAP

        /**
         * @type {Map<number, ResourceInfo>}
         */
        assetMap: null,

        /**
         * @type {Map<number, ResourceGroup>}
         */
        groupMap: null,

        // SETTINGS

        /**
         * Array of audio formats to attempt to load.
         */
        audioFormatTypes: [],

        // CONSTANTS

        resourceTypeStrings: [
            "fetch",
            "audio",
            "image",
            "video"
        ],

        fetchPriorityStrings: [
            "auto",
            "low",
            "high"
        ],

        crossOriginSetting: "anonymous",
    },

    /**
     * Changes the extension of a path.
     * @param {string} path
     * @param {string} ext 
     * @returns {string}
     */
    $npChangeExtension: function (path, ext) {
        const idx = path.lastIndexOf(".");
        if (idx >= 0) {
            return path.substring(0, idx) + ext;
        } else {
            return path + ext;
        }
    },

    $npInitialize: function () {
        if (NPCache.assetMap == null) {
            NPCache.assetMap = new Map();
        }

        if (NPCache.groupMap == null) {
            NPCache.groupMap = new Map();
        }
    },

    $npOnLoad__deps: ['$npGetGroup'],
    $npOnLoad: function (info) {
        /** @type {ResourceInfo} */ const infoObj = info;
        if (infoObj.state != 1) {
            /** @type {ResourceGroup} */ const group = npGetGroup(info.group);
            if (group) {
                if (infoObj.state == 0) {
                    group.loading--;
                } else if (infoObj.state == 2) {
                    group.error--;
                }
            }
            infoObj.state = 1;
            const element = infoObj.htmlElement;
            if (element) {
                element.onload = element.onerror = null;
                element.remove();
                infoObj.htmlElement = null;
            }
        }
    },

    $npOnError__deps: ['$npGetGroup'],
    $npOnError: function (info) {
        /** @type {ResourceInfo} */ const infoObj = info;
        if (infoObj.state != 2) {
            /** @type {ResourceGroup} */ const group = npGetGroup(info.group);
            if (group) {
                if (infoObj.state == 0) {
                    group.loading--;
                } else if (infoObj.state == 1) {
                    group.loaded--;
                }
            }
            infoObj.state = 2;
        }
    },

    /**
     * Returns an asset group with the given identifier.
     * @param {number} identifier
     * @param {boolean} create
     * @returns {ResourceGroup}
     */
    $npGetGroup: function (identifier, create) {
        /**
         * @type {Map<number, ResourceGroup>}
         */
        const map = NPCache.groupMap;
        var group = map.get(identifier);
        if (!group && create) {
            group = {
                assetCount: 0,
                loading: 0,
                loaded: 0,
                error: 0
            };
            map.set(identifier, group);
        }
        return group;
    },

    /**
     * 
     * @param {string} url
     * @param {number} type
     * @param {number} priority
     * @param {number} identifier
     * @param {number} group
     * @returns {ResourceInfo}
     */
    $npCreateResource__deps: ['$npChangeExtension', '$npOnLoad', '$npOnError'],
    $npCreateResource: function (url, type, priority, identifier, group) {
        var element, isAudioElement;
        if (type == 1 && NPCache.audioFormatTypes.length > 0) {
            isAudioElement = true;

            element = new Audio();
            for (let i = 0; i < NPCache.audioFormatTypes.length; i++) {
                const format = NPCache.audioFormatTypes[i];
                const formatSource = document.createElement("source");
                formatSource.src = npChangeExtension(url, format.extension);
                formatSource.type = format.type;

                element.appendChild(formatSource);
            }

            element.autoplay = false;
        } else {
            isAudioElement = false;

            element = document.createElement("link");
            element.href = url;
            element.rel = "prefetch";
            element.as = NPCache.resourceTypeStrings[type | 0];
            element.fetchPriority = NPCache.fetchPriorityStrings[priority | 0];
        }
        element.crossOrigin = NPCache.crossOriginSetting;

        /** @type {ResourceInfo} */
        const elementInfo = {
            htmlElement: element,
            sourceUrl: url,
            identifier: identifier,
            group: group,
            state: 0
        };

        element.onload = () => {
            npOnLoad(elementInfo);
        };
        element.onerror = () => {
            npOnError(elementInfo);
        };

        if (isAudioElement) {
            element.load();
        }

        document.head.appendChild(element);
        NPCache.assetMap.set(identifier, elementInfo);

        return elementInfo;
    },

    NativePrefetch_LoadResource__deps: ['$npCreateResource', '$npInitialize', '$npGetGroup'],
    NativePrefetch_LoadResource__sig: 'viiiii',
    NativePrefetch_LoadResource: function (url, type, priority, identifier, group) {
        npInitialize();

        /**
         * @type {Map<number, ResourceInfo>}
         */
        const elementMap = NPCache.assetMap;

        const urlString = UTF8ToString(url);

        if (!elementMap.has(identifier)) {
            /** @type {ResourceInfo} */
            const element = npCreateResource(urlString, type, priority, identifier, group);
            /** @type {ResourceGroup} */
            const groupInfo = npGetGroup(group, true);

            groupInfo.assetCount++;
            groupInfo.loading++;

            console.log("[NativePrefetch] Prefetching", urlString, "in group", group);
        }
    },

    NativePrefetch_IsResourceLoaded__sig: 'ii',
    NativePrefetch_IsResourceLoaded: function (identifier) {
        /**
         * @type {Map<number, ResourceInfo>}
         */
        const elementMap = NPCache.assetMap;
        if (elementMap) {
            var elementInfo;
            if (elementInfo = elementMap.get(identifier)) {
                return elementInfo.state == 1;
            }
        }
        return false;
    },

    NativePrefetch_IsGroupLoaded__sig: 'ii',
    NativePrefetch_IsGroupLoaded: function (group) {
        /**
         * @type {Map<number, ResourceGroup>}
        */
        const groupMap = NPCache.groupMap;
        if (groupMap) {
            var groupInfo;
            if (groupInfo = groupMap.get(group)) {
                return groupInfo.loading == 0;
            }
        }
        return false;
    },

    NativePrefetch_CancelResource__deps: ['$npInitialize', '$npGetGroup'],
    NativePrefetch_CancelResource__sig: 'ii',
    NativePrefetch_CancelResource: function (identifier) {
        npInitialize();

        /** @type {ResourceInfo} */
        const elementInfo = NPCache.assetMap.get(identifier);
        if (elementInfo) {
            /** @type {ResourceGroup} */
            const group = npGetGroup(elementInfo.group);
            if (group) {
                group.assetCount--;
                switch (elementInfo.state) {
                    case 0: {
                        group.loading--;
                        break;
                    }
                    case 1: {
                        group.loaded--;
                        break;
                    }
                    case 1: {
                        group.error--;
                        break;
                    }
                }
            }
            const element = elementInfo.htmlElement;
            if (element) {
                element.onload = element.onerror = null;
                element.remove();
                elementInfo.htmlElement = null;

                console.log("[NativePrefetch] Cancelling prefetch of", elementInfo.sourceUrl);
            }

            NPCache.assetMap.delete(identifier);
            return true;
        }

        return false;
    },

    NativePrefetch_CancelGroup__sig: 'ii',
    NativePrefetch_CancelGroup: function (group) {
        /** @type {Map<number, ResourceGroup>}*/
        const groupMap = NPCache.groupMap;
        /** @type {Map<number, ResourceInfo>}*/
        const assetMap = NPCache.assetMap;

        if (groupMap && groupMap.has(group)) {
            groupMap.delete(group);

            console.log("[NativePrefetch] Cancelling prefetch group", group);

            assetMap.forEach((elementInfo, identifier, map) => {
                if (elementInfo.group == group) {
                    const element = elementInfo.htmlElement;
                    if (element) {
                        element.onload = element.onerror = null;
                        element.remove();
                        elementInfo.htmlElement = null;

                        console.log("[NativePrefetch] Cancelling prefetch of", elementInfo.sourceUrl);
                    }
                    map.delete(identifier);
                }
            });

            return true;
        }

        return false;
    }
}

autoAddDeps(NativePrefetchLib, '$NPCache');
mergeInto(LibraryManager.library, NativePrefetchLib);