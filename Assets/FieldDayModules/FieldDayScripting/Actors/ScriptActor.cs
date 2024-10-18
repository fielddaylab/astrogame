using System;
using BeauPools;
using BeauUtil;
using BeauUtil.Variants;
using FieldDay.Components;
using FieldDay.Data;
using Leaf.Runtime;
using UnityEngine;

namespace FieldDay.Scripting {
    [DisallowMultipleComponent]
    public sealed class ScriptActor : BatchedComponent, IPoolAllocHandler, IPoolConstructHandler, ILeafActor, IRegistrationCallbacks, IEditorOnlyData {
        #region Inspector

        [SerializeField] private SerializedHash32 m_Id = string.Empty;
        [SerializeField] private SerializedHash32 m_ClassName = string.Empty;

        #endregion // Inspector

        [NonSerialized] private VariantTable m_Locals;
        [NonSerialized] private bool m_Pooled;
        [NonSerialized] private IScriptActorComponent[] m_Components;

        #region ILeafActor

        public StringHash32 Id { get { return m_Id.Hash(); } }
        public VariantTable Locals { get { return m_Locals ?? (m_Locals = new VariantTable()); } }

        #endregion // ILeafActor

        public StringHash32 ClassName { get { return m_ClassName.Hash(); } }

        #region Pool Callbacks

        void IPoolConstructHandler.OnConstruct() {
            m_Pooled = true;
        }

        void IPoolAllocHandler.OnAlloc() {
            Register(this);
        }

        void IPoolAllocHandler.OnFree() {
            Deregister(this);
        }

        void IPoolConstructHandler.OnDestruct() {
            Deregister(this);

        }

        #endregion // Pool Callbacks

        #region Unity Events

        private void OnDestroy() {
            Deregister(this);
        }

        #endregion // Unity Events

        #region IRegistrationCallbacks

        void IRegistrationCallbacks.OnRegister() {
            Register(this);
        }

        void IRegistrationCallbacks.OnDeregister() {
        }

        #endregion // IRegistrationCallbacks

        #region Registration

        static public bool Register(ScriptActor actor) {
            ScriptRuntimeState runtime = Find.State<ScriptRuntimeState>();
            if (runtime.Actors.Register(actor)) {
                actor.OnScriptRegistered();
                return true;
            }

            return false;
        }

        static public bool Deregister(ScriptActor actor) {
            if (Game.IsShuttingDown) {
                return false;
            }

            ScriptRuntimeState runtime = Find.State<ScriptRuntimeState>();
            if (runtime.Actors.Deregister(actor)) {
                actor.OnScriptDeregistered();
                return true;
            }
            return false;
        }

        private void OnScriptRegistered() {
            if (m_Components == null) {
                m_Components = GetComponents<IScriptActorComponent>();
            }

            for (int i = 0; i < m_Components.Length; i++) {
                m_Components[i].OnScriptRegister(this);
            }

            Game.Scenes.QueueOnLoad(this, OnScriptSceneReady);
        }

        private void OnScriptSceneReady() {
            for (int i = 0; i < m_Components.Length; i++) {
                m_Components[i].OnScriptSceneReady(this);
            }
        }

        private void OnScriptDeregistered() {
            for (int i = 0; i < m_Components.Length; i++) {
                m_Components[i].OnScriptDeregister(this);
            }
        }

        #endregion // Registration

        #region Leaf

        [LeafMember("Activate")]
        public void Activate() {
            gameObject.SetActive(true);
        }

        [LeafMember("Deactivate")]
        public void Deactivate() {
            gameObject.SetActive(false);
        }

        [LeafMember("ToggleActive")]
        public void ToggleActive() {
            gameObject.SetActive(!gameObject.activeSelf);
        }

        #endregion

#if UNITY_EDITOR
        void IEditorOnlyData.ClearEditorData(bool isDevelopmentBuild) {
            EditorOnlyData.Strip(ref m_Id);
            EditorOnlyData.Strip(ref m_ClassName);
        }
#endif // UNITY_EDITOR
    }
}