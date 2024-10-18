using System;
using System.IO;
using BeauUtil;
using UnityEditor;
using UnityEngine;

namespace FieldDay.Editor {
    public sealed class AudioImportCategories : AssetPostprocessor {
        private void OnPreprocessAudio() {
            AudioImporter importer = (AudioImporter) assetImporter;
            if (importer.userData.Contains("[AudioImportCategories]")) {
                return;
            }

            Flags flags = ReadFlags(assetPath);
            //Debug.LogFormat("audioclip '{0}' has flags {1}", assetPath, flags);

            importer.userData += "[AudioImportCategories]";

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;

            settings.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
            settings.quality = 0.1f;
            
            if ((flags & Flags.Music) != 0 || ((flags & Flags.Voice) != 0)) {
                settings.loadType = AudioClipLoadType.Streaming;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
            }

            importer.defaultSampleSettings = settings;
        }

        private void OnPostprocessAudio(AudioClip clip) {
            // TODO: Implement
        }

        static private Flags ReadFlags(string path) {
            string fileName = Path.GetFileNameWithoutExtension(path);
            if (fileName.Contains("Music", StringComparison.OrdinalIgnoreCase) || fileName.StartsWith("BGM", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith("BGM", StringComparison.OrdinalIgnoreCase)) {
                return Flags.Music;
            } else if (fileName.StartsWith("VO_", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith("_vo", StringComparison.OrdinalIgnoreCase)) {
                return Flags.Voice;
            }

            foreach (var parentDir in FilePathUtility.GetParentDirectoryNames(path)) {
                //Debug.Log(parentDir);
                if (parentDir.Equals("vo", true) || parentDir.Equals("voice", true) || parentDir.Equals("voiceover", true)) {
                    return Flags.Voice;
                } else if (parentDir.Contains("Music", true) || parentDir.Contains("BGM", true)) {
                    return Flags.Music;
                }
            }
            return 0;
        }

        [Flags]
        private enum Flags {
            Music = 0x01,
            Voice = 0x02
        }
    }
}