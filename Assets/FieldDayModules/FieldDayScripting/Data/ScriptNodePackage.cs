using BeauUtil;
using BeauUtil.Blocks;
using BeauUtil.Debugger;
using BeauUtil.IO;
using Leaf;
using Leaf.Compiler;

namespace FieldDay.Scripting {
    public class ScriptNodePackage : LeafNodePackage<ScriptNode> {
        private bool m_Active;
        private int m_UseCount = 0;
        private IHotReloadable m_HotReload;
        private LeafAsset m_Asset;
        internal UniqueId16 m_LoadId;
        private bool m_Reloading;

        public ScriptNodePackage(string name) : base(name) {
        }

        #region Active State

        /// <summary>
        /// Returns if the package is loaded and active.
        /// </summary>
        public bool IsActive() {
            return m_Active;
        }

        /// <summary>
        /// Sets whether nodes from this package can be triggered. 
        /// </summary>
        public bool SetActive(bool active) {
            return Ref.Replace(ref m_Active, active);
        }

        #endregion // Active State

        #region Reference Count

        /// <summary>
        /// Adds a reference. This tracks how many script stack frames are currently using this package.
        /// </summary>
        public void AddReference() {
            m_UseCount++;
        }

        /// <summary>
        /// Removes a reference. This tracks how many script stack frames are currently using this package.
        /// </summary>
        public void ReleaseReference() {
            Assert.True(m_UseCount > 0, "Unbalanced AddReference/ReleaseReference calls");
            m_UseCount--;
        }

        /// <summary>
        /// Returns if any script stack frames are currently using this package.
        /// </summary>
        public bool IsReferenced() {
            return m_UseCount > 0;
        }

        #endregion // Reference Count

        #region Hot Reload

        internal void AssignSource(LeafAsset asset) {
            m_Asset = asset;
            if (m_HotReload != null) {
                Game.Assets.DeregisterHotReloadable(m_HotReload);
                Ref.TryDispose(ref m_HotReload);
            }

            m_HotReload = Game.Assets.RegisterHotReloadCallbacks(asset, HandleHotReload);
        }

        internal bool WasFromSource(LeafAsset asset) {
            return m_Active == asset;
        }

        private void HandleHotReload(LeafAsset asset, HotReloadAssetRemapArgs<LeafAsset> remapArgs, HotReloadOperation op) {
            m_Asset = asset;
            m_Reloading = true;
            ScriptDBUtility.HotReload(ScriptUtility.DB, this, m_LoadId, asset, remapArgs, op);
            m_Reloading = false;
        }

        #endregion // Hot Reload

        public override void Clear() {
            base.Clear();

            if (!m_Reloading) {
                Game.Assets?.DeregisterHotReloadable(m_HotReload);
                Ref.TryDispose(ref m_HotReload);
                m_LoadId = default;
                m_Asset = default;
                m_Active = false;
                m_UseCount = 0;
            }
        }

        #region Generator

        /// <summary>
        /// Package parser.
        /// </summary>
        public sealed class Parser : LeafParser<ScriptNode, ScriptNodePackage> {
            static public readonly Parser Instance = new Parser();

            public override LeafCompilerFlags CompilerFlags {
                get { return base.CompilerFlags | LeafCompilerFlags.Preserve_CustomLineNameStrings; }
            }

            public override ScriptNodePackage CreatePackage(string inFileName) {
                return new ScriptNodePackage(inFileName);
            }

            protected override ScriptNode CreateNode(string inFullId, StringSlice inExtraData, ScriptNodePackage inPackage) {
                return new ScriptNode(inFullId, inPackage);
            }

            public override void OnEnd(IBlockParserUtil inUtil, ScriptNodePackage inPackage, bool inbError) {
                base.OnEnd(inUtil, inPackage, inbError);

                if (ScriptNode.PatchFunction != null) {
                    foreach (var node in inPackage.m_Nodes.Values) {
                        ScriptNode.PatchFunction(node);
                    }
                }

                if (inbError) {
                    Log.Error("[ScriptNodePackage] Package '{0}' failed to compile", inPackage.Name());
                }
            }
        }

        #endregion // Generator
    }
}