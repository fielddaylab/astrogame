using System;
using BeauUtil;
using FieldDay.Components;
using UnityEngine;

namespace FieldDay.Scripting {
    public interface IScriptActorComponent {
        ScriptActor Actor { get; }
        void OnScriptRegister(ScriptActor actor);
        void OnScriptSceneReady(ScriptActor actor);
        void OnScriptDeregister(ScriptActor actor);
    }

    [NonIndexed, RequireComponent(typeof(ScriptActor))]
    public abstract class ScriptActorComponent : BatchedComponent, IScriptActorComponent {
        #region IScriptActorComponent

        public ScriptActor Actor { get; private set; }

        public virtual void OnScriptRegister(ScriptActor actor) {
            Actor = actor;
        }

        public virtual void OnScriptSceneReady(ScriptActor actor) {
        }

        public virtual void OnScriptDeregister(ScriptActor actor) {
            Actor = null;
        }

        #endregion // IScriptActorComponent
    }
}