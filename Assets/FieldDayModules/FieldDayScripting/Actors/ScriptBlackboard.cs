using System;
using BeauUtil;
using BeauUtil.Variants;
using FieldDay.Components;
using FieldDay.SharedState;
using UnityEngine;

namespace FieldDay.Scripting {
    [DefaultExecutionOrder(SharedStateComponent.DefaultExecutionOrder - 100)]
    public sealed class ScriptBlackboard : MonoBehaviour {
        [SerializeField] private SerializedHash32 m_Id;

        [NonSerialized] private VariantTable m_Table;

        private void OnEnable() {
            ScriptUtility.BindTable(m_Id, m_Table ?? (m_Table = new VariantTable()));
        }

        private void OnDisable() {
            ScriptUtility.UnbindTable(m_Id);
        }
    }
}