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

        public StringHash32 Name;
        public StringHash32 Group;

        public uint PathKey;
        public string Path;

        public FileReadHandler Callback;
        public object CallbackContext;

        public void ConfigureAudioClip(AudioClipLoadType loadType) {
            Mode = FileBufferMode.AudioClip;
            Flags &= ~(FileLoadFlags.Audio_Streaming | FileLoadFlags.Audio_Compressed);
            if (loadType == AudioClipLoadType.CompressedInMemory) {
                Flags |= FileLoadFlags.Audio_Compressed;
            } else if (loadType == AudioClipLoadType.Streaming) {
                Flags |= FileLoadFlags.Audio_Streaming;
            }
        }

        public void ConfigureTexture(bool markAsNonReadable) {
            Mode = FileBufferMode.Texture;
            Flags &= ~(FileLoadFlags.Texture_MarkNonReadable);
            if (markAsNonReadable) {
                Flags |= FileLoadFlags.Texture_MarkNonReadable;
            }
        }

        public void SetIdentifiers(StringHash32 name, StringHash32 group) {
            Name = name;
            Group = group;
        }

        public void SetInfiniteRetries() {
            Flags |= FileLoadFlags.InfiniteRetries;
        }

        public void PutInExhaustedQueue() {
            Flags |= FileLoadFlags.PushToExhaustedQueueOnFailure;
        }

        static public FileLoadRequest Buffer(string path, FileLocation location, FileReadHandler callback, object callbackContext = null) {
            return new FileLoadRequest() {
                Location = location,
                Mode = FileBufferMode.Buffer,
                Path = path,
                Callback = callback,
                CallbackContext = callbackContext
            };
        }

        static public FileLoadRequest Texture(string path, FileLocation location, FileReadHandler callback, object callbackContext = null) {
            return new FileLoadRequest() {
                Location = location,
                Mode = FileBufferMode.Texture,
                Path = path,
                Callback = callback,
                CallbackContext = callbackContext
            };
        }

        static public FileLoadRequest AudioClip(string path, FileLocation location, FileReadHandler callback, object callbackContext = null) {
            return new FileLoadRequest() {
                Location = location,
                Mode = FileBufferMode.AudioClip,
                Path = path,
                Callback = callback,
                CallbackContext = callbackContext
            };
        }
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
        Texture_MarkNonReadable = 0x004,

        InfiniteRetries = 0x008,
        PushToExhaustedQueueOnFailure = 0x010
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
        /// Interprets the downloaded data as a byte span.
        /// </summary>
        public unsafe UnsafeSpan<byte> ReadByteSpan() {
            Assert.True(Succeeded());
            var nativeData = Handler.nativeData;
            byte* ptr = (byte*)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(nativeData);
            return new UnsafeSpan<byte>(ptr, nativeData.Length);
        }

        /// <summary>
        /// Interprets the downloaded data as a string.
        /// </summary>
        public unsafe string ReadText() {
            Assert.True(Succeeded());
            return Handler.text;
        }

        /// <summary>
        /// Interprets the downloaded data as a string buffer.
        /// </summary>
        public unsafe void ReadText(UnsafeSpan<char> destination) {
            Assert.True(Succeeded());
            var nativeData = Handler.nativeData;
            byte* ptr = (byte*)NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(nativeData);
            StringUtils.DecodeUFT8(ptr, nativeData.Length, destination.Ptr, destination.Length);
        }

        /// <summary>
        /// Estimates the length of the string data.
        /// </summary>
        public unsafe int EstimateStringLength() {
            Assert.True(Succeeded());
            return StringUtils.DecodeSizeUTF8((int) Request.downloadedBytes);
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
        High,
        Urgent
    }
}