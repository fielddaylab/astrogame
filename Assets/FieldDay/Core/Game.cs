#if (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

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
using FieldDay.Perf;
using FieldDay.Files;
using FieldDay.Localization;

[assembly: InternalsVisibleTo("FieldDay.Core.Editor")]

namespace FieldDay {
    /// <summary>
    /// Maintains references to game engine components.
    /// </summary>
    public class Game {

        /// <summary>
        /// Is this a development build?
        /// </summary>
        public const bool IsDevBuild =
#if DEVELOPMENT
            true;
#else
            false;
#endif // DEVELOPMENT

        /// <summary>
        /// Is this in the unity editor?
        /// </summary>
        public const bool IsEditor =
#if UNITY_EDITOR
            true;
#else
            false;
#endif // UNITY_EDITOR

        /// <summary>
        /// Audio manager. Maintains audio playback.
        /// </summary>
        static public AudioMgr Audio { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }

        /// <summary>
        /// ISystem manager. Maintains system updates.
        /// </summary>
        static public SystemsMgr Systems { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }

        /// <summary>
        /// IComponentData manager. Maintains component lists.
        /// </summary>
        static public ComponentMgr Components { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }

        /// <summary>
        /// ISharedState manager. Maintains shared state components.
        /// </summary>
        static public SharedStateMgr SharedState { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }

        /// <summary>
        /// Process manager. Maintains process states.
        /// </summary>
        static public ProcessMgr Processes { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }

        /// <summary>
        /// Scene manager. Maintains scene loading.
        /// </summary>
        static public SceneMgr Scenes { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }

        /// <summary>
        /// Rendering manager. Handles render state callbacks.
        /// </summary>
        static public RenderMgr Rendering { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }

        /// <summary>
        /// Input manager. Maintains input state.
        /// </summary>
        static public InputMgr Input { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }

        /// <summary>
        /// Gui panel manager. Maintains shared panel references.
        /// </summary>
        static public GuiMgr Gui { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }

        /// <summary>
        /// Asset lookup manager. Maintains asset lookup tables.
        /// </summary>
        static public AssetMgr Assets { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }

        /// <summary>
        /// Animation manager. Maintains lite and procedural animations.
        /// </summary>
        static public AnimationMgr Animation { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }

        /// <summary>
        /// Memory manager. Maintains memory pools.
        /// </summary>
        static public MemoryMgr Memory { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }

        /// <summary>
        /// Performance and profiling manager.
        /// </summary>
        static public PerformanceMgr Perf { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }

        /// <summary>
        /// File system manager.
        /// </summary>
        static public FileSystem Files { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }

        /// <summary>
        /// Localization manager.
        /// </summary>
        static public LocMgr Localization { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }

        /// <summary>
        /// Event dispatcher. Maintains event dispatch.
        /// </summary>
        static public IEventDispatcher Events { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; internal set; }

        /// <summary>
        /// Returns if the game loop is currently shutting down.
        /// </summary>
        static public bool IsShuttingDown {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
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