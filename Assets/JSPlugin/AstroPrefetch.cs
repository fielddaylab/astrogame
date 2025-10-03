#if UNITY_WEBGL && !UNITY_EDITOR
#define USE_JSLIB
#endif // UNITY_WEBGL && !UNITY_EDITOR

using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Files;
using FieldDay.Localization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace Astro {
    static public class AstroPrefetch {
        [DllImport("__Internal")]
        static private extern void AstroPrefetch_Video(string url);

        [DllImport("__Internal")]
        static private extern void AstroPrefetch_Audio(string url);

        [DllImport("__Internal")]
        static private extern void AstroPrefetch_Texture(string url);

        [DllImport("__Internal")]
        static private extern void AstroPrefetch_File(string url);

        static public void Video(string streamingPath) {
            streamingPath = FileSystem.ResolvePathToUrl(streamingPath, FileLocation.Streaming);
#if USE_JSLIB
            AstroPrefetch_Video(streamingPath);
#else
            Log.Debug("[AstroPrefetch] Prefetching video '{0}'", streamingPath);
#endif // USE_JSLIB
        }

        static public void Audio(string streamingPath) {
            streamingPath = FileSystem.ResolvePathToUrl(streamingPath, FileLocation.Streaming);
#if USE_JSLIB
            AstroPrefetch_Audio(streamingPath);
#else
            Log.Debug("[AstroPrefetch] Prefetching audio '{0}'", streamingPath);
#endif // USE_JSLIB
        }

        static public void Texture(string streamingPath) {
            streamingPath = FileSystem.ResolvePathToUrl(streamingPath, FileLocation.Streaming);
#if USE_JSLIB
            AstroPrefetch_Texture(streamingPath);
#else
            Log.Debug("[AstroPrefetch] Prefetching texture '{0}'", streamingPath);
#endif // USE_JSLIB
        }

        static public void File(string streamingPath) {
            streamingPath = FileSystem.ResolvePathToUrl(streamingPath, FileLocation.Streaming);
#if USE_JSLIB
            AstroPrefetch_File(streamingPath);
#else
            Log.Debug("[AstroPrefetch] Prefetching file '{0}'", streamingPath);
#endif // USE_JSLIB
        }

        static private HashSet<int> s_ManifestsParsed = new HashSet<int>();

        static public void Manifest(TextAsset manifest) {
            if (!s_ManifestsParsed.Add(manifest.GetInstanceID())) {
                return;
            }

            string data = manifest.text;
            foreach(var line in StringSlice.Split(data, StringUtils.DefaultNewLineChars, StringSplitOptions.RemoveEmptyEntries)) {
                if (line.StartsWith("##")) {
                    continue;
                }

                if (line.EndsWith(".mp3") || line.EndsWith(".wav") || line.EndsWith(".ogg")) {
                    Audio(line.ToString());
                } else if (line.EndsWith(".png") || line.EndsWith(".jpg")) {
                    Texture(line.ToString());
                } else if (line.EndsWith(".webm") || line.EndsWith(".mp4")) {
                    Video(line.ToString());
                } else {
                    File(line.ToString());
                }
            }
        }

        static public IEnumerator<WorkSlicer.Result?> ManifestAsync(TextAsset manifest) {
            if (!s_ManifestsParsed.Add(manifest.GetInstanceID())) {
                yield break;
            }

            string data = manifest.text;
            foreach (var line in StringSlice.Split(data, StringUtils.DefaultNewLineChars, StringSplitOptions.RemoveEmptyEntries)) {
                if (line.StartsWith("##")) {
                    yield return WorkSlicer.Result.Processed;
                    continue;
                }

                if (line.EndsWith(".mp3") || line.EndsWith(".wav") || line.EndsWith(".ogg")) {
                    Audio(line.ToString());
                } else if (line.EndsWith(".png") || line.EndsWith(".jpg")) {
                    Texture(line.ToString());
                } else if (line.EndsWith(".webm") || line.EndsWith(".mp4")) {
                    Video(line.ToString());
                } else {
                    File(line.ToString());
                }

                yield return WorkSlicer.Result.Processed;
            }
        }
    }
}