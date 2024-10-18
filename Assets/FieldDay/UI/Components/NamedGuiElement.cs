using System;
using BeauUtil;
using UnityEngine;

namespace FieldDay.UI {
    /// <summary>
    /// Registers this transform for global lookup in the GuiMgr.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("Field Day/Canvas/Global Named Gui Element")]
    public sealed class NamedGuiElement : MonoBehaviour {
        [SerializeField] private SerializedHash32 m_Id;
        [NonSerialized] private StringHash32 m_AppliedName;

        private void Awake() {
            m_AppliedName = m_Id.IsEmpty ? gameObject.name : m_Id;
            Game.Gui.RegisterNamed(m_AppliedName, (RectTransform) transform);
        }

        private void OnDestroy() {
            Game.Gui?.DeregisterNamed(m_AppliedName, (RectTransform) transform);
        }
    }
}