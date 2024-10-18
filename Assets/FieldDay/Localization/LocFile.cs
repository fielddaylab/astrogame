using BeauUtil.Blocks;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif // UNITY_2020_2_OR_NEWER

#endif // UNITY_EDITOR

namespace FieldDay.Localization {
    /// <summary>
    /// Localization file.
    /// </summary>
    public sealed class LocFile : CustomTextAsset {
        internal const string FileExtension = "fdloc";
        internal const string FileExtensionWithDot = "." + FileExtension;

        [SerializeField] private LanguageId m_Language;

        /// <summary>
        /// Language this file
        /// </summary>
        public LanguageId Language {
            get { return m_Language; }
        }

#if UNITY_EDITOR
        private class Importer : ImporterBase<LocFile> {
            public override void OnImportAsset(AssetImportContext ctx) {
                base.OnImportAsset(ctx);

                LocFile file = (LocFile) ctx.mainObject;
                file.m_Language = LanguageId.IdentifyLanguageFromPath(ctx.assetPath);
            }
        }
#endif // UNITY_EDITOR
    }
}