using FieldDay.Systems;
using FieldDay.SharedState;
using FieldDay.Components;
using FieldDay.Processes;
using FieldDay.Audio;
using System.Runtime.CompilerServices;
using FieldDay.Scenes;
using FieldDay.UI;
using FieldDay.Assets;
using FieldDay.HID;
using FieldDay.Rendering;
using FieldDay.Animation;
using FieldDay.Memory;

[assembly: InternalsVisibleTo("FieldDay.Core.Editor")]

namespace FieldDay {
    /// <summary>
    /// Maintains references to game engine components.
    /// </summary>
    public class Game {
        /// <summary>
        /// Audio manager. Maintains audio playback.
        /// </summary>
        static public AudioMgr Audio { get; internal set; }

        /// <summary>
        /// ISystem manager. Maintains system updates.
        /// </summary>
        static public SystemsMgr Systems { get; internal set; }

        /// <summary>
        /// IComponentData manager. Maintains component lists.
        /// </summary>
        static public ComponentMgr Components { get; internal set; }

        /// <summary>
        /// ISharedState manager. Maintains shared state components.
        /// </summary>
        static public SharedStateMgr SharedState { get; internal set; }

        /// <summary>
        /// Process manager. Maintains process states.
        /// </summary>
        static public ProcessMgr Processes { get; internal set; }

        /// <summary>
        /// Scene manager. Maintains scene loading.
        /// </summary>
        static public SceneMgr Scenes { get; internal set; }

        /// <summary>
        /// Rendering manager. Handles render state callbacks.
        /// </summary>
        static public RenderMgr Rendering { get; internal set; }

        /// <summary>
        /// Input manager. Maintains input state.
        /// </summary>
        static public InputMgr Input { get; internal set; }

        /// <summary>
        /// Gui panel manager. Maintains shared panel references.
        /// </summary>
        static public GuiMgr Gui { get; internal set; }

        /// <summary>
        /// Asset lookup manager. Maintains asset lookup tables.
        /// </summary>
        static public AssetMgr Assets { get; internal set; }

        /// <summary>
        /// Animation manager. Maintains lite and procedural animations.
        /// </summary>
        static public AnimationMgr Animation { get; internal set; }

        /// <summary>
        /// Memory manager. Maintains memory pools.
        /// </summary>
        static public MemoryMgr Memory { get; internal set; }

        /// <summary>
        /// Event dispatcher. Maintains event dispatch.
        /// </summary>
        static public IEventDispatcher Events { get; internal set; }

        /// <summary>
        /// Returns if the game loop is currently shutting down.
        /// </summary>
        static public bool IsShuttingDown {
            get { return GameLoop.s_CurrentPhase == GameLoopPhase.Shutdown; }
        }

        /// <summary>
        /// Sets the current event dispatcher.
        /// </summary>
        static public void SetEventDispatcher(IEventDispatcher eventDispatcher) {
            Events = eventDispatcher;
        }
    }
}