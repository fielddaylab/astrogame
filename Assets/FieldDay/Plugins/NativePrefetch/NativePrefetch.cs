#if (!UNITY_EDITOR && UNITY_WEBGL)
#define USE_JSLIB
#endif // !UNITY_EDITOR && UNITY_WEBGL

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace NativeUtils {
    static public class NativePrefetch {
#if USE_JSLIB

        [DllImport("__Internal")]
        static private extern void NativePrefetch_Start(string url, int resourceType, int identifier);

        [DllImport("__Internal")]
        static private extern bool NativePrefetch_IsLoaded(int identifier);

        [DllImport("__Internal")]
        static private extern bool NativePrefetch_Cancel(int identifier);

#else

        static private readonly HashSet<int> s_DebugPrefetchedURLS = new HashSet<int>();

#endif // USE_JSLIB

        /// <summary>
        /// Type of resource
        /// </summary>
        public enum ResourceType {
            Unknown,
            Audio,
            Image,
            Video
        }

        /// <summary>
        /// Prefetchs the resource with the given url.
        /// </summary>
        static public bool Prefetch(string url, ResourceType resourceType, int identifier) {
            if (url == null || !url.Contains("://")) {
                Console.Error.WriteLine("[NativePrefetch] Cannot prefetch invalid url '{0}'", url);
                return false;
            }

            if (identifier == 0) {
                Console.Error.WriteLine("[NativePrefetch] Cannot prefetch invalid identifier");
                return false;
            }

#if USE_JSLIB
            NativePrefetch_Start(url, (int) resourceType, identifier);
#else
            Console.Out.WriteLine("[NativePrefetch] Requested prefetch of '{0}' of type {1} (id {2})", url, resourceType, identifier);
            s_DebugPrefetchedURLS.Add(identifier);
#endif // USE_JSLIB

            return true;
        }

        /// <summary>
        /// Returns if the resource with the given identifier has been prefetched.
        /// </summary>
        static public bool IsLoaded(int identifier) {
            if (identifier == 0) {
                Console.Error.WriteLine("[NativePrefetch] Cannot prefetch invalid identifier");
                return false;
            }

#if USE_JSLIB
            return NativePrefetch_IsLoaded(identifier);
#else
            return s_DebugPrefetchedURLS.Contains(identifier);
#endif // USE_JSLIB
        }

        /// <summary>
        /// Cancels any prefetch of the resource with the given identifier.
        /// </summary>
        static public bool Cancel(int identifier) {
            if (identifier == 0) {
                Console.Error.WriteLine("[NativePrefetch] Cannot prefetch invalid identifier");
                return false;
            }

#if USE_JSLIB
            return NativePrefetch_Cancel(identifier);
#else
            Console.Out.WriteLine("[NativePrefetch] Requested cancel prefetch of resource id {0}", identifier);
            return s_DebugPrefetchedURLS.Remove(identifier);
#endif // USE_JSLIB
        }
    }
}