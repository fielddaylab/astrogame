using Astro.Audio;
using BeauData;
using BeauUtil;
using BeauUtil.Debugger;
using EasyAssetStreaming;
using FieldDay.Data;
using System;
using System.IO;
using System.Text;
using System.Threading;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Astro {
    static public class VoxWaveformManifestGenerator {
        static private void OnWizardCreate() {
            string url = Streaming.ResolveAddressToURL("");
            UnityWebRequest uwr = UnityWebRequest.Get(url);
            DownloadHandlerAudioClip downloadHandler = new DownloadHandlerAudioClip(url, AudioType.UNKNOWN);
            downloadHandler.compressed = false;

            uwr.downloadHandler = downloadHandler;

            uwr.SendWebRequest();

            while(!uwr.isDone) {
                Thread.Sleep(1);
            }

            if (uwr.result == UnityWebRequest.Result.Success) {
                AudioClip clip = downloadHandler.audioClip;

                var chunks = VoxWaveform.GenerateChunks(clip);

                StringBuilder sb = new StringBuilder(1024);
                sb.Append("Waveform: ").Append("");
                unsafe {
                    foreach (var chunk in chunks) {
                        for (int i = 0; i < VoxWaveform.SamplesPerChunk; i++) {
                            sb.Append("\n|").Append('-', chunk.Data[i]);
                        }
                    }
                }

                Debug.Log(sb.ToString());

                AudioClip.DestroyImmediate(clip);
            }

            uwr.Dispose();
        }

        static private VoxWaveformChunk[] GenerateChunks(string referencePath, FileInfo file, out StringHash32 lineCode) {
            string fullPath = file.FullName.Replace('\\', '/');
            string trimmedPath = fullPath.Replace(referencePath, string.Empty);
            if (trimmedPath.StartsWith('/')) {
                trimmedPath = trimmedPath.Substring(1);
            }

            trimmedPath = Path.ChangeExtension(trimmedPath, null);

            Debug.Log(trimmedPath);
            lineCode = StringHash32.Fast(trimmedPath);
            AudioClip clip = DownloadClip(file.FullName);

            if (clip != null) {
                var chunks = VoxWaveform.GenerateChunks(clip);

                //StringBuilder sb = new StringBuilder(1024);
                //sb.Append("Waveform: ").Append(file.FullName);
                //unsafe {
                //    foreach (var chunk in chunks) {
                //        for (int i = 0; i < VoxWaveform.SamplesPerChunk; i++) {
                //            sb.Append("\n|").Append('-', chunk.Data[i]);
                //        }
                //    }
                //}

                //Debug.Log(sb.ToString());

                AudioClip.DestroyImmediate(clip);
                return chunks;
            }

            return null;
        }

        static private AudioClip DownloadClip(string fullPath) {
            string url = Streaming.ResolveAddressToURL(fullPath);
            UnityWebRequest uwr = UnityWebRequest.Get(url);
            DownloadHandlerAudioClip downloadHandler = new DownloadHandlerAudioClip(url, AudioType.UNKNOWN);
            downloadHandler.compressed = false;

            uwr.downloadHandler = downloadHandler;

            uwr.SendWebRequest();

            while (!uwr.isDone) {
                Thread.Sleep(1);
            }

            AudioClip clip = null;

            if (uwr.result == UnityWebRequest.Result.Success) {
                clip = downloadHandler.audioClip;
            }

            uwr.Dispose();
            return clip;
        }

        static private unsafe void BuildInDirectory(DirectoryInfo dir) {
            byte[] tocData = new byte[64 * Unsafe.KiB];
            byte[] sampleData = new byte[64 * Unsafe.KiB];
            byte[] finalData = new byte[128 * Unsafe.KiB];

            string refPath = dir.FullName.Replace('\\', '/');

            fixed (byte* tocPtr = tocData) {
                fixed (byte* samplePtr = sampleData) {
                    ByteWriter tocWriter = new ByteWriter(tocPtr, tocData.Length);
                    ByteWriter sampleWriter = new ByteWriter(samplePtr, sampleData.Length);

                    int chunkCount = 0;
                    int entryCount = 0;

                    VoxWaveformTable.TOCEntry toc;
                    foreach (var file in dir.EnumerateFiles("*.mp3", SearchOption.AllDirectories)) {
                        var chunks = GenerateChunks(refPath, file, out toc.LineCode);
                        if (chunks != null && !toc.LineCode.IsEmpty) {
                            sampleWriter.WriteBuffer(chunks);
                            toc.Chunks.Offset = (ushort)chunkCount;
                            toc.Chunks.Length = (ushort)chunks.Length;
                            tocWriter.Write(toc);
                            chunkCount += chunks.Length;
                            entryCount++;
                        }
                    }

                    byte[] exportData;
                    fixed(byte* finalPtr = finalData) {
                        ByteWriter finalWriter = new ByteWriter(finalPtr, finalData.Length);
                        finalWriter.Write<uint>((uint)entryCount);
                        finalWriter.WriteBuffer(tocWriter);
                        finalWriter.Write<uint>((uint)chunkCount);
                        finalWriter.WriteBuffer(sampleWriter);

                        exportData = finalWriter.GetDataCopy();
                    }

                    Log.Msg("Built manifest for directory '{0}' - {1} entries, {2} total chunks", dir.Name, entryCount, chunkCount);
                    File.WriteAllBytes(Path.Combine(refPath, "VoxManifest.bytes"), exportData);
                }
            }
        }

        [MenuItem("Astro/Build Vox Waveform Manifests")]
        static private void BuildManifests() {
            foreach(var dirPath in Directory.EnumerateDirectories(Path.Combine(Application.streamingAssetsPath, "vox"))) {
                BuildInDirectory(new DirectoryInfo(dirPath));
            }

            foreach (var dirPath in Directory.EnumerateDirectories(Path.Combine(Application.streamingAssetsPath, "radio"))) {
                BuildInDirectory(new DirectoryInfo(dirPath));
            }
        }
    }
}