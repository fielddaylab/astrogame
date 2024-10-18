using System;
using System.IO;
using BeauUtil;
using UnityEditor;
using UnityEngine;

namespace FieldDay.Editor {
    public sealed class TextureImportProcessor : AssetPostprocessor {
        private void OnPreprocessTexture() {
            TextureImporter importer = (TextureImporter) assetImporter;
            if (importer.userData.Contains("[TextureImportProcessor]")) {
                return;
            }

            Flags flags = ReadFlags(assetPath);
            //Debug.LogFormat("texture '{0}' has flags {1}", assetPath, flags);

            if (flags == 0) {
                return;
            }

            importer.userData += "[TextureImportProcessor]";

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);

            settings.readable = false;

            if ((flags & Flags.Texture) != 0) {
                settings.npotScale = TextureImporterNPOTScale.ToNearest;
                if (settings.textureType == TextureImporterType.Sprite) {
                    settings.textureType = TextureImporterType.Default;
                }
            } else if ((flags & Flags.Sprite) != 0) {
                settings.mipmapEnabled = false;
                settings.textureType = TextureImporterType.Sprite;
                settings.spriteMode = (int) SpriteImportMode.Single;
                settings.alphaIsTransparency = true;
                settings.npotScale = TextureImporterNPOTScale.None;
                settings.wrapMode = TextureWrapMode.Clamp;
            }

            if ((flags & Flags.UI) != 0) {
                settings.mipmapEnabled = false;
                settings.spriteGenerateFallbackPhysicsShape = false;
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.filterMode = FilterMode.Bilinear;
                importer.maxTextureSize = 1024;
            }

            importer.SetTextureSettings(settings);
        }

        static private Flags ReadFlags(string path) {
            Flags f = 0;
            bool foundType = false;
            string fileName = Path.GetFileNameWithoutExtension(path);
            if (fileName.Contains("_tex", StringComparison.OrdinalIgnoreCase)) {
                foundType = true;
                f |= Flags.Texture;
            }

            foreach (var parentDir in FilePathUtility.GetParentDirectoryNames(path)) {
                //Debug.Log(parentDir);
                if (parentDir.Equals("ui", true) || parentDir.Equals("UISprites", true)) {
                    f |= Flags.UI;
                } else {
                    if (!foundType) {
                        if (parentDir.Contains("Sprite", true)) {
                            f |= Flags.Sprite;
                            foundType = true;
                        } else if (parentDir.Contains("Tex", true) || parentDir.Contains("Pattern", true)) {
                            f |= Flags.Texture;
                            foundType = true;
                        }
                    }
                }
            }
            return f;
        }

        [Flags]
        private enum Flags {
            Sprite = 0x01,
            Texture = 0x02,
            UI = 0x04
        }
    }
}