using BeauUtil;
using UnityEngine;

namespace FieldDay.UI {
    /// <summary>
    /// Gui update callback interface.
    /// Will be executed after UnscaledLateUpdate.
    /// </summary>
    public interface IOnGuiUpdate {
        void OnGuiUpdate();
    }
}