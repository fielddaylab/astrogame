using System;
using UnityEngine;

namespace TMPro {
    /// <summary>
    /// Adjusts individual characters.
    /// </summary>
    [RequireComponent(typeof(TMP_Text)), ExecuteAlways]
    public class TMP_AdjustCharacters : MonoBehaviour {

        #region Types

        [Serializable]
        public struct CharacterAdjustment {
            public Vector3 Offset;
            public Color32 Color;
        }

        #endregion // Types

        #region Inspector

        [SerializeField] private CharacterAdjustment[] m_CharAdjustments = Array.Empty<CharacterAdjustment>();

        #endregion // Inspector

        [NonSerialized] private TMP_Text m_Text;
        [NonSerialized] private int m_CharsApplied;
        [NonSerialized] private CharacterAdjustment[] m_PrevApplied = Array.Empty<CharacterAdjustment>();

        private readonly Action<UnityEngine.Object> TextUpdatedCallback;

        private TMP_AdjustCharacters() {
            TextUpdatedCallback = (o) => {
                if (o == m_Text) {
                    m_CharsApplied = 0;
                    ApplyChanges();
                }
            };
        }

        #region Unity Events

        private void Awake() {
            m_Text = GetComponent<TMP_Text>();
        }

        private void OnEnable() {
#if UNITY_EDITOR
            if (!Application.IsPlaying(this) && UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) {
                return;
            }
#endif // UNITY_EDITOR

            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(TextUpdatedCallback);
            m_CharsApplied = 0;
            ApplyChanges();
        }

        private void OnDisable() {
#if UNITY_EDITOR
            if (!Application.IsPlaying(this) && UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) {
                return;
            }
#endif // UNITY_EDITOR

            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(TextUpdatedCallback);
        }

#if UNITY_EDITOR
        private void OnValidate() {
            if (!isActiveAndEnabled) {
                return;
            }

            if (!Application.IsPlaying(this) && UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) {
                return;
            }

            m_Text = GetComponent<TMP_Text>();
            m_Text.ForceMeshUpdate();
            //Reapply();
        }
#endif // UNITY_EDITOR

        #endregion // Unity Events

        /// <summary>
        /// Returns the adjustment struct for the given character index.
        /// </summary>
        public ref CharacterAdjustment Adjustment(int index) {
            if (m_CharAdjustments.Length < index) {
                Array.Resize(ref m_CharAdjustments, Mathf.NextPowerOfTwo(index + 1));
            }
            return ref m_CharAdjustments[index];
        }

        /// <summary>
        /// Clears all character adjustments.
        /// </summary>
        public void ClearAdjustments(bool commit = true) {
            for(int i = 0; i < m_CharAdjustments.Length; i++) {
                m_CharAdjustments[i] = default;
            }
            if (commit) {
                Commit();
            }
        }

        /// <summary>
        /// Commits all changes to the text mesh.
        /// </summary>
        public void Commit() {
            if (m_Text) {
                ApplyChanges();
            }
        }

        private void ApplyChanges() {
            TMP_TextInfo info = m_Text.textInfo;

            if (m_PrevApplied.Length < m_CharAdjustments.Length) {
                Array.Resize(ref m_PrevApplied, m_CharAdjustments.Length);
            }

            int deltaChars = m_CharsApplied;
            int totalChars = Math.Min(m_CharAdjustments.Length, info.characterCount);
            bool updated = false;

            for (int i = 0; i < deltaChars; i++) {
                updated |= AdjustCharacter(info, i, m_CharAdjustments[i].Offset - m_PrevApplied[i].Offset, m_CharAdjustments[i].Color, m_PrevApplied[i].Color);
                m_PrevApplied[i] = m_CharAdjustments[i];
            }

            for (int i = deltaChars; i < totalChars; i++) {
                updated |= AdjustCharacter(info, i, m_CharAdjustments[i].Offset, m_CharAdjustments[i].Color, Color.white);
                m_PrevApplied[i] = m_CharAdjustments[i];
            }

            if (updated) {
                info.textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
            }

            m_CharsApplied = totalChars;
        }

        static public unsafe bool AdjustCharacter(TMP_TextInfo info, int charIndex, Vector3 offset, Color32 blend, Color32 prevColor) {
            if (offset.x != 0 || offset.y != 0 || offset.z != 0 || *(uint*) (&blend) != *(uint*)(&prevColor)) {
                ref var charInfo = ref info.characterInfo[charIndex];
                int meshIndex = charInfo.materialReferenceIndex;
                int vertIndex = charInfo.vertexIndex;

                Color32 finalColor = charInfo.color * (Color) blend;

                Color32[] colors = info.meshInfo[meshIndex].colors32;
                colors[vertIndex + 0] = finalColor;
                colors[vertIndex + 1] = finalColor;
                colors[vertIndex + 2] = finalColor;
                colors[vertIndex + 3] = finalColor;

                Vector3[] positions = info.meshInfo[meshIndex].vertices;
                positions[vertIndex + 0] += offset;
                positions[vertIndex + 1] += offset;
                positions[vertIndex + 2] += offset;
                positions[vertIndex + 3] += offset;

                return true;
            }

            return false;
        }
    }
}