using System;
using System.IO;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace Astro {
    public sealed class SkyboxTextureCombiner : ScriptableWizard {
        public Texture2D Color;
        public Texture2D Alpha;
        public Texture2D Output;

        private void OnWizardUpdate() {
            if (!Color || !Alpha) {
                helpString = "Please provide source textures";
                isValid = false;
            } else if (Color.width != Alpha.width || Color.height != Alpha.height) {
                helpString = "Source textures are not the same size";
                isValid = false;
            } else if (!Color.isReadable || !Alpha.isReadable) {
                helpString = "Source textures must be readable";
                isValid = false;
            } else if (!Output) {
                helpString = "Please provide output texture";
                isValid = false;
            } else {
                helpString = string.Empty;
                isValid = true;
            }
        }

        private void OnWizardCreate() {
            Color32[] colorPixels = Color.GetPixels32();
            Color32[] alphaPixels = Alpha.GetPixels32();

            string outputPath = AssetDatabase.GetAssetPath(Output);

            Texture2D combinedTex = new Texture2D(Color.width, Color.height, TextureFormat.RGBA32, false);

            Color32[] outputPixels = new Color32[colorPixels.Length];
            for(int i = 0; i < outputPixels.Length; i++) {
                Color32 c = colorPixels[i];
                Color32 a = alphaPixels[i];
                c.a = Math.Max(a.b, Math.Max(a.g, a.r));
                outputPixels[i] = c;
            }

            combinedTex.SetPixels32(outputPixels);
            combinedTex.Apply();

            File.WriteAllBytes(outputPath, combinedTex.EncodeToPNG());

            DestroyImmediate(combinedTex);

            AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceSynchronousImport);
        }

        [MenuItem("Astro/Skybox Texture Combiner")]
        static private void CreateWizard() {
            DisplayWizard<SkyboxTextureCombiner>("Skybox Texture Combiner", "Combine");
        }
    }
}