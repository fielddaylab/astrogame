using BeauRoutine.Extensions;
using BeauUtil;
using UnityEngine;

namespace FieldDay.UI {
    /// <summary>
    /// Gui module.
    /// </summary>
    [DefaultExecutionOrder(BaseGuiModule.DefaultExecutionOrder)]
    [NonIndexed]
    public abstract class BaseGuiModule : MonoBehaviour, IGuiModule {
        public const int DefaultExecutionOrder = -200;

        protected virtual void Awake() {
            Game.Gui.RegisterModule(this);
        }

        protected virtual void OnDestroy() {
            if (!Game.IsShuttingDown) {
                Game.Gui.DeregisterModule(this);
            }
        }
    }
}