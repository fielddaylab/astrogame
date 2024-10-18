using UnityEngine;

namespace FieldDay.Scenes {
    /// <summary>
    /// Behaviour marking a GameObject hierarchy as only existing for development builds.
    /// </summary>
    public sealed class DevModeOnly : MonoBehaviour, IDevModeOnly {
    }

    /// <summary>
    /// Marks a component type as only existing for development builds.
    /// </summary>
    public interface IDevModeOnly { }
}