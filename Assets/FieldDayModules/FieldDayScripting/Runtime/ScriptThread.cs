using System;
using BeauPools;
using BeauUtil;
using Leaf;
using Leaf.Runtime;
using FieldDay.Vox;
using BeauUtil.Debugger;
using FieldDay.Audio;

namespace FieldDay.Scripting {
    /// <summary>
    /// Scripting thread implementation.
    /// </summary>
    public sealed class ScriptThread : LeafThreadState<ScriptNode> {
        private readonly IPool<ScriptThread> m_Pool;
        private readonly ScriptPlugin m_CustomPlugin;

        private StringHash32 m_OriginalNodeId;
        private StringHash32 m_Target;
        private ScriptNodePriority m_Priority;
        private ScriptThreadFlags m_Flags;

        private int m_CutsceneDepth;
        private VoxRequestHandle m_Voiceover;
        private float m_VoiceoverReleaseTime;

        public ScriptThread(IPool<ScriptThread> pool, ScriptPlugin inPlugin) : base(inPlugin) {
            m_Pool = pool;
            m_CustomPlugin = inPlugin;
        }

        #region Initial State

        public StringHash32 InitialNodeId() {
            return m_OriginalNodeId;
        }

        public StringHash32 Target() {
            return m_Target;
        }

        public ScriptNodePriority Priority() {
            return m_Priority;
        }

        public bool IsFunction() {
            return (m_Flags & ScriptThreadFlags.IsFunction) != 0;
        }

        public bool IsTrigger() {
            return (m_Flags & ScriptThreadFlags.IsTrigger) != 0;
        }

        internal void SetInitialNode(ScriptNode node, StringHash32 target) {
            m_OriginalNodeId = node.Id();
            m_Target = target;
            m_Priority = node.Priority;

            if ((node.Flags & ScriptNodeFlags.Trigger) != 0) {
                m_Flags |= ScriptThreadFlags.IsTrigger;
            } else if ((node.Flags & ScriptNodeFlags.Function) != 0) {
                m_Flags |= ScriptThreadFlags.IsFunction;
            }
        }

        #endregion // Initial State

        #region Cutscene

        internal void PushCutscene() {
            if (m_CutsceneDepth++ == 0) {
                m_CustomPlugin.SetCutscene(GetHandle());
            }
        }

        internal void PopCutscene() {
            Assert.True(m_CutsceneDepth > 0, "ScriptThread.Push/PopCutscene calls unbalanced");
            if (m_CutsceneDepth-- == 1) {
                m_CustomPlugin.DereferenceCutscene(GetHandle());
            }
        }

        #endregion // Cutscene

        #region Skipping

        public bool IsSkipping() {
            return (m_Flags & ScriptThreadFlags.Skipping) != 0;
        }

        internal void StopSkipping() {
            // TODO: Implement
        }

        public bool PopSkipSingle() {
            if ((m_Flags & ScriptThreadFlags.SkipSingle) != 0) {
                m_Flags &= ~ScriptThreadFlags.SkipSingle;
                return true;
            }

            return false;
        }

        public void SkipSingle() {
            m_Flags |= ScriptThreadFlags.SkipSingle;
            SkipCurrentVox();
        }

        #endregion // Skipping

        #region Voiceover

        public void SkipCurrentVox() {
            if (m_Voiceover.IsValid) {
                VoxUtility.Stop(m_Voiceover);
                m_Voiceover = default;
                m_VoiceoverReleaseTime = 0;
            }
        }

        internal void AssignVox(VoxRequestHandle voxHandle) {
            if (m_Voiceover != voxHandle) {
                VoxUtility.Stop(m_Voiceover);
                m_Voiceover = voxHandle;
                m_VoiceoverReleaseTime = 0;
            }
        }

        internal void SetVoxReleaseTime(float releaseTime) {
            if (m_Voiceover.IsValid) {
                m_VoiceoverReleaseTime = releaseTime;
            }
        }

        internal float GetVoxReleaseTime() {
            if (m_VoiceoverReleaseTime >= 0) {
                return m_VoiceoverReleaseTime;
            } else {
                return VoxUtility.GetDuration(m_Voiceover) + m_VoiceoverReleaseTime;
            }
        }

        internal void ReleaseVox() {
            if (m_Voiceover.IsValid) {
                m_Voiceover = default;
                m_VoiceoverReleaseTime = 0;
            }
        }

        #endregion // Voiceover

        protected override void Reset() {
            m_CustomPlugin.StopTracking(this);
            VoxUtility.Stop(ref m_Voiceover);
            m_VoiceoverReleaseTime = 0;
            
            base.Reset();

            while(m_CutsceneDepth > 0) {
                PopCutscene();
            }

            m_Flags = default;
            m_OriginalNodeId = null;
            m_Priority = default;

            m_Pool.Free(this);
        }
    }

    [Flags]
    internal enum ScriptThreadFlags {
        None = 0,

        Skipping = 0x01,
        Cutscene = 0x02,
        IsFunction = 0x04,
        IsTrigger = 0x08,
        SkipSingle = 0x10
    }
}