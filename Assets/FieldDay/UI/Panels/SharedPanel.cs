using BeauRoutine.Extensions;
using BeauUtil;
using UnityEngine;

namespace FieldDay.UI {
    /// <summary>
    /// Shared panel.
    /// </summary>
    [DefaultExecutionOrder(SharedPanel.DefaultExecutionOrder)]
    [NonIndexed]
    public abstract class SharedPanel : MonoBehaviour, ISharedGuiPanel {
        public const int DefaultExecutionOrder = -100;

        protected virtual void Awake() {
            Game.Gui.RegisterPanel(this);
        }

        protected virtual void OnDestroy() {
            if (!Game.IsShuttingDown) {
                Game.Gui.DeregisterPanel(this);
            }
        }

        #region ISharedGuiPanel

        public virtual Transform Root {
            get { return transform; }
        }

        public virtual void Hide() {
            gameObject.SetActive(false);
        }

        public virtual bool IsVisible() {
            return gameObject.activeInHierarchy;
        }

        public virtual bool IsShowing() {
            return gameObject.activeSelf;
        }

        public virtual void Show() {
            gameObject.SetActive(true);
        }

        public virtual bool IsTransitioning() {
            return false;
        }

        #endregion // ISharedGuiPanel
    }
}