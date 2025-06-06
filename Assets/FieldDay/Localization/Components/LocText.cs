using BeauUtil;
using FieldDay.Components;
using System;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace FieldDay.Localization {
    /// <summary>
    /// Localized text object.
    /// </summary>
    [RequireComponent(typeof(TMP_Text)), DisallowMultipleComponent]
    public sealed class LocText : BatchedComponent, ILocalizedComponent {
        #region Types

        public struct TextMetrics {
            public int VisibleCharCount;
            public int RichCharCount;
        }

        #endregion // Types

        #region Inspector

        [SerializeField, HideInEditor] private TMP_Text m_Graphic;
        [SerializeField] internal LocId m_DefaultId;

        [Header("Modifications")]
        [SerializeField] private string m_Prefix;
        [SerializeField] private string m_Postfix;

        [Header("Additional Settings")]
        [SerializeField] private bool m_TintSprites;

        #endregion // Inspector

        [NonSerialized] private LocId m_LastAssignedId;
        [NonSerialized] private LanguageId m_LastKnownLanguage;

        #region Unity Events

        protected override void OnEnable() {
            base.OnEnable();
            // TODO: register to localization
            if (m_LastKnownLanguage.IsEmpty) {
                m_Graphic.tintAllSprites = m_TintSprites;
                // TODO: check if we should initialize
            }
        }

        protected override void OnDisable() {
            // TODO: deregister from localization
            base.OnDisable();
        }

#if UNITY_EDITOR

        private void Reset() {
            m_Graphic = GetComponent<TMP_Text>();
        }

        private void OnValidate() {
            this.CacheComponent(ref m_Graphic);
        }

#endif // UNITY_EDITOR 

        #endregion // Unity Events

        #region ILocalizedComponent

        void ILocalizedComponent.OnLocalizationUpdated(LanguageId language) {
            
        }

        #endregion // ILocalizedComponent
    }
}