using System;
using System.Text;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Data;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Networking;

namespace FieldDay.Files {
    public struct FileLoadRequest {
        public FileLocation Location;
        public FileBufferMode Mode;
        public FileLoadFlags Flags;
        public StringHash32 Identifier;
        public StringHash32 Group;

        public string Path;
        public FileReadHandler Callback;
        public object CallbackContext;
    }

    public delegate void FileReadHandler(FileLoadRequest request, FileLoadResult result, object context);

    public enum FileLocation : byte {
        Raw,
        Streaming,
        Persistent,
        TempCachePath
    }

    public enum FileBufferMode : byte {
        Buffer,
        Texture,
        AudioClip,
    }

    [Flags]
    public enum FileLoadFlags : ushort {
        Audio_Compressed = 0x001,
        Audio_Streaming = 0x002,
        Texture_MarkNonReadable = 0x004
    }

    public readonly struct FileLoadResult {
        public readonly FileLoadResponse Response;
        public readonly UnityWebRequest Request;
        public readonly DownloadHandler Handler;

        internal FileLoadResult(UnityWebRequest uwr) {
            Request = uwr;
            Handler = uwr.downloadHandler;

            switch (uwr.result) {
                case UnityWebRequest.Result.Success:
                    Response = FileLoadResponse.Success;
                    break;
                case UnityWebRequest.Result.ConnectionError:
                    Response = FileLoadResponse.Error_Network;
                    break;
                case UnityWebRequest.Result.ProtocolError:
                    Response = FileLoadResponse.Error_Http;
                    break;
                default:
                    Response = FileLoadResponse.Error_Unknown;
                    break;
            }
        }

        /// <summary>
        /// Returns if the load succeeded.
        /// </summary>
        public bool Succeeded() {
            return Response == FileLoadResponse.Success;
        }

        /// <summary>
        /// Creates a byte reader from the downloaded data.
        /// </summary>
        public unsafe ByteReader CreateByteReader() {
            Assert.True(Succeeded());
            var nativeData = Handler.nativeData;
            byte* ptr = (byte*) NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(nativeData);
            return new ByteReader(ptr, nativeData.Length);
        }

        /// <summary>
        /// Interprets the downloaded data as a string.
        /// </summary>
        public unsafe string ReadText() {
            Assert.True(Succeeded());
            return Handler.text;
        }

        /// <summary>
        /// Interprets the downloaded data as an AudioClip.
        /// </summary>
        public AudioClip ReadAudioClip() {
            Assert.True(Succeeded());
            DownloadHandlerAudioClip clipHandler = (DownloadHandlerAudioClip) Handler;
            return clipHandler.audioClip;
        }

        /// <summary>
        /// Interprets the downloaded data as a Texture2D.
        /// </summary>
        public Texture2D ReadTexture() {
            Assert.True(Succeeded());
            DownloadHandlerTexture textureHandler = (DownloadHandlerTexture) Handler;
            return textureHandler.texture;
        }

        /// <summary>
        /// Returns the length of the downloaded data.
        /// </summary>
        public ulong ResponseLength() {
            if (Succeeded()) {
                return Request.downloadedBytes;
            } else {
                return 0;
            }
        }
    }

    public enum FileLoadResponse {
        Success,
        Error_Http,
        Error_Network,
        Error_Unknown
    }

    public enum FileLoadPriority : byte {
        Low,
        High
    }
}