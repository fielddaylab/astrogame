using FieldDay.Assets;
using UnityEngine;

namespace FieldDay.Localization {
    /// <summary>
    /// Localization configuration.
    /// </summary>
    public sealed class LocManifest : NamedAsset {
        [SerializeField] private LanguageId m_Language;
        [SerializeField] private string m_BinaryPath;

        /// <summary>
        /// This file's language.
        /// </summary>
        public LanguageId Language {
            get { return m_Language; }
        }
    }
}