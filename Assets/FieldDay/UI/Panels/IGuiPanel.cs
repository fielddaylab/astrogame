using BeauUtil;
using UnityEngine;

namespace FieldDay.UI {
    /// <summary>
    /// Interface panel.
    /// </summary>
    [TypeIndexCapacity(512)]
    public interface IGuiPanel {
        Transform Root { get; }

        void Show();
        void Hide();

        bool IsShowing();
        bool IsTransitioning();
        bool IsVisible();
    }

    /// <summary>
    /// Singleton interface panel.
    /// </summary>
    public interface ISharedGuiPanel : IGuiPanel {  }
}