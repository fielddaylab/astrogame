using System;
using BeauUtil;
using UnityEngine.Scripting;

namespace FieldDay.SharedState {
    /// <summary>
    /// Singleton state object.
    /// </summary>
    [TypeIndexCapacity(512)]
    public interface ISharedState { }

    /// <summary>
    /// Attribute marking a static field or property as an injected ISharedState reference.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    [Preserve]
    public sealed class SharedStateReferenceAttribute : PreserveAttribute { }
}