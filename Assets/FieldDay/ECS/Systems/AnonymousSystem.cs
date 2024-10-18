using BeauUtil;
using System;

namespace FieldDay.Systems {
    /// <summary>
    /// Anonymous system driven by callbacks.
    /// </summary>
    public sealed class AnonymousSystem : ISystem {
        public StringHash32 Name;
        public Action Initialize;
        public Action Shutdown;
        public Func<bool> HasWork;
        public Action<float> ProcessWork;

        /// <summary>
        /// Creates a new anonymous system with the given primary functions.
        /// </summary>
        public AnonymousSystem(StringHash32 name, Action<float> processWork, Func<bool> hasWork, Action init, Action shutdown) {
            Name = name;
            ProcessWork = processWork;
            HasWork = hasWork;
            Initialize = init;
            Shutdown = shutdown;
        }

        /// <summary>
        /// Creates a new anonymous system with the given primary functions.
        /// </summary>
        public AnonymousSystem(StringHash32 name, Action<float> processWork, Func<bool> hasWork = null) {
            Name = name;
            ProcessWork = processWork;
            HasWork = hasWork;
        }

        #region Configuration

        /// <summary>
        /// Sets the initialization function.
        /// </summary>
        public AnonymousSystem InitializeWith(Action initialize) {
            Initialize = initialize;
            return this;
        }

        /// <summary>
        /// Sets the shutdown function.
        /// </summary>
        public AnonymousSystem ShutdownWith(Action shutdown) {
            Shutdown = shutdown;
            return this;
        }

        #endregion // Configuration

        #region ISystem

        bool ISystem.HasWork() {
            return HasWork?.Invoke() ?? true;
        }

        void ISystem.ProcessWork(float deltaTime) {
            ProcessWork?.Invoke(deltaTime);
        }

        void ISystem.Initialize() {
            Initialize?.Invoke();
        }

        void ISystem.Shutdown() {
            Shutdown?.Invoke();
        }

        #endregion // ISystem
    }
}