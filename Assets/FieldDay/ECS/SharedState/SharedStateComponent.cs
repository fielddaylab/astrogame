using System;
using BeauUtil;
using UnityEngine;

namespace FieldDay.SharedState {
    /// <summary>
    /// Base class for a shared state component.
    /// This data will be globally accessible while this component is active.
    /// </summary>
    [DefaultExecutionOrder(SharedStateComponent.DefaultExecutionOrder)]
    [NonIndexed]
    public abstract class SharedStateComponent : MonoBehaviour, ISharedState {
        public const int DefaultExecutionOrder = -20000;

        protected virtual void OnEnable() {
            Game.SharedState.Register(this);
        }

        protected virtual void OnDisable() {
            if (!Game.IsShuttingDown) {
                Game.SharedState.Deregister(this);
            }
        }
    }

    /// <summary>
    /// Controls initialization order of any SharedStateComponents.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class SharedStateInitOrderAttribute : DefaultExecutionOrder {
        public SharedStateInitOrderAttribute(int offset)
            : base(SharedStateComponent.DefaultExecutionOrder + offset) { }
    }
}