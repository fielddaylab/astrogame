using System;
using BeauUtil;
using UnityEngine.Scripting;

namespace FieldDay.Systems {
    /// <summary>
    /// Base interface for a game system.
    /// Systems should possess no state.
    /// </summary>
    [TypeIndexCapacity(1024)]
    public interface ISystem {
        /// <summary>
        /// Initializes the system.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Shuts down the system.
        /// </summary>
        void Shutdown();

        /// <summary>
        /// Indicates if the system has any work to process.
        /// </summary>
        bool HasWork();

        /// <summary>
        /// Processes available work.
        /// </summary>
        void ProcessWork(float deltaTime);
    }

    /// <summary>
    /// Attribute defining system initialization order.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false), Preserve]
    public sealed class SysInitOrderAttribute : PreserveAttribute {
        public readonly int Order;

        public SysInitOrderAttribute(int order) {
            Order = order;
        }
    }

    /// <summary>
    /// Attribute defining system update order.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false), Preserve]
    public sealed class SysUpdateAttribute : PreserveAttribute {
        public readonly GameLoopPhaseMask PhaseMask;
        public readonly int Order;
        public readonly int CategoryMask;
        public bool AllowExecutionDuringLoad;

        public SysUpdateAttribute(GameLoopPhase phase, int order = 0, int updateMask = Bits.All32) {
            PhaseMask = (GameLoopPhaseMask) (1 << (int) phase);
            Order = order;
            CategoryMask = updateMask;
        }

        public SysUpdateAttribute(GameLoopPhaseMask phaseMask, int order = 0, int updateMask = Bits.All32) {
            PhaseMask = phaseMask;
            Order = order;
            CategoryMask = updateMask;
        }
    }
}