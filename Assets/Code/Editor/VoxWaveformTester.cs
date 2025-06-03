using Astro.Audio;
using EasyAssetStreaming;
using System;
using System.IO;
using System.Text;
using System.Threading;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Astro {
    public sealed class VoxWaveformTester : ScriptableWizard {
        [StreamingAudioPath] public string Path;

        private void OnWizardUpdate() {
            //if (string.IsNullOrEmpty(Path)) {
            //    helpString = "Please provide source path";
            //    isValid = false;
            //} else if (!File.Exists(Application.streamingAssetsPath + Path)) {
            //    helpString = "Source file does not exist";
            //    isValid = false;
            //} else {
            //    helpString = string.Empty;
            //    isValid = true;
            //}
        }

        private void OnWizardCreate() {
            string url = Streaming.ResolveAddressToURL(Path);
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
                sb.Append("Waveform: ").Append(Path);
                unsafe {
                    foreach (var chunk in chunks) {
                        for (int i = 0; i < VoxWaveform.SamplesPerChunk; i++) {
                            sb.Append("\n|").Append('-', chunk.Data[i] / 2);
                        }
                    }
                }

                Debug.Log(sb.ToString());

                DestroyImmediate(clip);
            }

            uwr.Dispose();
        }

        [MenuItem("Astro/Vox Waveform Tester")]
        static private void CreateWizard() {
            DisplayWizard<VoxWaveformTester>("Vox Waveform Tester", "Generate");
        }
    }
}