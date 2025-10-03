using System.Collections.Generic;
using System.IO;
using System.Text;
using BeauUtil;
using BeauUtil.Editor;
using FieldDay.Scripting;
using Leaf;
using UnityEditor;

namespace Astro.Editor {
    static public class LeafVoxMappingGenerator {
        [MenuItem("Astro/Generate Vox Map File")]
        static public void GenerateMappingFile() {
            var allScripts = AssetDBUtils.FindAssets<LeafAsset>();
            StringBuilder sb = new StringBuilder(1024);

            List<KeyValuePair<StringHash32, string>> outLines = new List<KeyValuePair<StringHash32, string>>();

            foreach (var script in allScripts) {
                ScriptNodePackage package = LeafAsset.Compile(script, ScriptNodePackage.Parser.Instance);
                outLines.Clear();
                package.GatherAllLinesWithCustomNames(outLines);
                foreach(var line in outLines) {
                    sb.Append(line.Key.ToString()).Append(", ").Append(line.Value).Append('\n');
                }
                package.Clear();
            }

            File.WriteAllText("Assets/_Assets/Audio/Core/VoxMap.txt", sb.ToString());
            AssetDatabase.Refresh(ImportAssetOptions.Default);
        }

        [MenuItem("Astro/Generate Vox Preload Files")]
        static public void GenerateVoxPreloadingFile() {
            var allScripts = AssetDBUtils.FindAssets<LeafAsset>();
            StringBuilder sb = new StringBuilder(1024);

            List<KeyValuePair<StringHash32, string>> outLines = new List<KeyValuePair<StringHash32, string>>();

            foreach (var script in allScripts) {
                ScriptNodePackage package = LeafAsset.Compile(script, ScriptNodePackage.Parser.Instance);
                outLines.Clear();
                package.GatherAllLinesWithCustomNames(outLines);
                foreach (var line in outLines) {
                    sb.Append("vox/en/").Append(line.Value).Append(".mp3").Append('\n');
                }
                package.Clear();
                File.WriteAllText(AssetDatabase.GetAssetPath(script).Replace(".leaf", "__vox.txt"), sb.ToString());
                sb.Clear();
            }

            AssetDatabase.Refresh(ImportAssetOptions.Default);
        }

        [MenuItem("Astro/Generate Reference Preload File")]
        static public void GenerateReferencePreloadFile() {
            var allPages = AssetDBUtils.FindAssets<ReferencePageAsset>();
            StringBuilder sb = new StringBuilder(1024);

            foreach (var page in allPages) {
                if (!string.IsNullOrEmpty(page.BackgroundImagePath)) {
                    sb.Append(page.BackgroundImagePath).Append('\n');
                }
            }

            File.WriteAllText("Assets/_Assets/Data/Reference/Pages/ReferencePreloadManifest.txt", sb.ToString());
            AssetDatabase.Refresh(ImportAssetOptions.Default);
        }
    }
}