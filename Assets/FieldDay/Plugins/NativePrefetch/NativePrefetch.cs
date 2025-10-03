#if !(!UNITY_EDITOR && UNITY_WEBGL)
#define USE_JSLIB
#endif // !UNITY_EDITOR && UNITY_WEBGL

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace NativeUtils {
    static public class NativePrefetch {
#if USE_JSLIB

        [DllImport("__Internal")]
        static private unsafe extern void NativePrefetch_LoadResource(char* url, int resourceType, int priority, int identifier, int group);

        [DllImport("__Internal")]
        static private extern bool NativePrefetch_IsResourceLoaded(int identifier);

        [DllImport("__Internal")]
        static private extern bool NativePrefetch_IsGroupLoaded(int group);

        [DllImport("__Internal")]
        static private extern bool NativePrefetch_CancelResource(int identifier);

        [DllImport("__Internal")]
        static private extern bool NativePrefetch_CancelGroup(int group);

#endif // USE_JSLIB

        /// <summary>
        /// Type of resource
        /// </summary>
        public enum ResourceType : byte {
            Unknown,
            Audio,
            Image,
            Video
        }

        /// <summary>
        /// Loading priority.
        /// </summary>
        public enum ResourcePriority : byte {
            Auto,
            Low,
            High
        }

        /// <summary>
        /// Prefetchs the resource with the given url.
        /// </summary>
        static public bool LoadResource(string url, ResourceType resourceType, ResourcePriority priority, int identifier, int group) {
            if (url == null || !url.Contains("://")) {
                Console.Error.WriteLine("[NativePrefetch] Cannot prefetch invalid url '{0}'", url);
                return false;
            }

            if (identifier == 0) {
                Console.Error.WriteLine("[NativePrefetch] Cannot prefetch invalid identifier");
                return false;
            }

#if USE_JSLIB
            unsafe {
                fixed (char* stringPtr = url) {
                    NativePrefetch_LoadResource(stringPtr, (int)resourceType, (int)priority, identifier, group);
                }
            }
#endif // USE_JSLIB

            return true;
        }

        /// <summary>
        /// Prefetchs the resource with the given url.
        /// </summary>
        static public unsafe bool LoadResource(char* url, int urlLength, ResourceType resourceType, ResourcePriority priority, int identifier, int group) {
            if (url == null ) {
                Console.Error.WriteLine("[NativePrefetch] Cannot prefetch invalid url ''");
                return false;
            }
            if (urlLength < 4) {
                Console.Error.WriteLine("[NativePrefetch] Cannot prefetch invalid url '{0}'", new string(url, 0, urlLength));
                return false;
            }

            if (identifier == 0) {
                Console.Error.WriteLine("[NativePrefetch] Cannot prefetch invalid identifier");
                return false;
            }

#if USE_JSLIB
            NativePrefetch_LoadResource(url, (int)resourceType, (int)priority, identifier, group);
#endif // USE_JSLIB

            return true;
        }

        /// <summary>
        /// Returns if the resource with the given identifier has been prefetched.
        /// </summary>
        static public bool IsResourceLoaded(int identifier) {
            if (identifier == 0) {
                Console.Error.WriteLine("[NativePrefetch] Cannot prefetch invalid identifier");
                return false;
            }

#if USE_JSLIB
            return NativePrefetch_IsResourceLoaded(identifier);
#else
            return true;
#endif // USE_JSLIB
        }

        /// <summary>
        /// Returns if all resources within the given group has been prefetched.
        /// </summary>
        static public bool IsGroupLoaded(int group) {
#if USE_JSLIB
            return NativePrefetch_IsGroupLoaded(group);
#else
            return true;
#endif // USE_JSLIB
        }

        /// <summary>
        /// Cancels any prefetch of the resource with the given identifier.
        /// </summary>
        static public bool CancelResource(int identifier) {
            if (identifier == 0) {
                Console.Error.WriteLine("[NativePrefetch] Cannot cancel the prefetch of an invalid identifier");
                return false;
            }

#if USE_JSLIB
            return NativePrefetch_CancelResource(identifier);
#else
            return true;
#endif // USE_JSLIB
        }

        /// <summary>
        /// Cancels any prefetch of resources with the given group.
        /// </summary>
        static public bool CancelGroup(int group) {
#if USE_JSLIB
            return NativePrefetch_CancelGroup(group);
#else
            return true;
#endif // USE_JSLIB
        }
    }
}