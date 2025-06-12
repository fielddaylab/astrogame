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
        static private VoxWaveformChunk[] GenerateChunks(string referencePath, FileInfo file, out StringHash32 lineCode, out float duration) {
            string fullPath = file.FullName.Replace('\\', '/');
            string trimmedPath = fullPath.Replace(referencePath, string.Empty);
            if (trimmedPath.StartsWith('/')) {
                trimmedPath = trimmedPath.Substring(1);
            }

            trimmedPath = Path.ChangeExtension(trimmedPath, null);

            lineCode = new StringHash32(trimmedPath);
            Debug.Log(trimmedPath + " " + lineCode.ToString());
            AudioClip clip = DownloadClip(file.FullName);

            if (clip != null) {
                var chunks = VoxWaveform.GenerateChunks(clip);
                duration = clip.length;

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

            duration = 0;
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

        static private unsafe void ExportTexture(StringHash32 lineCode, VoxWaveformChunk[] waveformChunks, float duration) {
            int fullWidth = (int) (50 * duration);
            Texture2D texture = new Texture2D(fullWidth, 256);
            Color32[] textureColors = texture.GetPixels32();

            fixed(VoxWaveformChunk* chunkPtr = waveformChunks) {
                VoxWaveform waveform;
                waveform.Chunks = new UnsafeSpan<VoxWaveformChunk>(chunkPtr, waveformChunks.Length);

                for(int i = 0; i < fullWidth; i++) {
                    float time = i * 0.02f;
                    float amp = VoxWaveform.ReadAmplitude(waveform, time, duration);
                    int ampPixels = (int) (256 * amp);
                    for(int p = 0; p < ampPixels; p++) {
                        textureColors[i + p * fullWidth] = Color.white;
                    }
                    for(int p = ampPixels; p < 256; p++) {
                        textureColors[i + p * fullWidth] = Color.black;
                    }
                }
            }

            texture.SetPixels32(textureColors);
            texture.Apply();

            byte[] png = texture.EncodeToPNG();
            File.WriteAllBytes("Library/VoxWaveformExportCache/" + lineCode.ToString() + ".png", png);

            Texture2D.DestroyImmediate(texture);
        }

        static private unsafe void BuildInDirectory(DirectoryInfo dir, bool generateTextures) {
            byte[] tocData = new byte[64 * Unsafe.KiB];
            byte[] sampleData = new byte[256 * Unsafe.KiB];
            byte[] finalData = new byte[310 * Unsafe.KiB];

            string refPath = dir.FullName.Replace('\\', '/');

            Directory.CreateDirectory("Library/VoxWaveformExportCache/");

            fixed (byte* tocPtr = tocData) {
                fixed (byte* samplePtr = sampleData) {
                    ByteWriter tocWriter = new ByteWriter(tocPtr, tocData.Length);
                    ByteWriter sampleWriter = new ByteWriter(samplePtr, sampleData.Length);

                    int chunkCount = 0;
                    int entryCount = 0;

                    VoxWaveformTable.TOCEntry toc;
                    float duration;
                    foreach (var file in dir.EnumerateFiles("*.mp3", SearchOption.AllDirectories)) {
                        EditorUtility.DisplayProgressBar("Generating waveforms...", file.Name, 0);
                        var chunks = GenerateChunks(refPath, file, out toc.LineCode, out duration);
                        if (chunks != null && !toc.LineCode.IsEmpty) {
                            if (generateTextures) {
                                ExportTexture(toc.LineCode, chunks, duration);
                            }
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
            bool generateTextures = EditorUtility.DisplayDialog("Generate Preview Textures", "Do you want to generate preview textures?", "Yeah", "no");

            try {
                foreach (var dirPath in Directory.EnumerateDirectories(Path.Combine(Application.streamingAssetsPath, "vox"))) {
                    BuildInDirectory(new DirectoryInfo(dirPath), generateTextures);
                }

                foreach (var dirPath in Directory.EnumerateDirectories(Path.Combine(Application.streamingAssetsPath, "radio"))) {
                    BuildInDirectory(new DirectoryInfo(dirPath), generateTextures);
                }
            }
            finally {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}