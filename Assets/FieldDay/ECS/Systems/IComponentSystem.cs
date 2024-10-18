using System;
using FieldDay.Components;

namespace FieldDay.Systems {
    /// <summary>
    /// Interface for a game system that operates on a set of components.
    /// Systems should possess no state.
    /// </summary>
    public interface IComponentSystem : ISystem {
        /// <summary>
        /// Number of components in this system.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Primary component this system iterates on.
        /// </summary>
        Type ComponentType { get; }

        /// <summary>
        /// Adds the given component to the system.
        /// </summary>
        void Add(IComponentData component);

        /// <summary>
        /// Removes the given component from the system.
        /// </summary>
        void Remove(IComponentData component);
    }

    /// <summary>
    /// Interface for a game system that operates on a set of components with the given type.
    /// Systems should possess no state.
    /// </summary>
    public interface IComponentSystem<in TPrimary> : IComponentSystem
        where TPrimary : class, IComponentData {
        /// <summary>
        /// Adds the given component to the system.
        /// </summary>
        void Add(TPrimary component);

        /// <summary>
        /// Removes the given component from the system.
        /// </summary>
        void Remove(TPrimary component);
    }
}