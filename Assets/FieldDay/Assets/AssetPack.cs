using System;
using UnityEngine;
using BeauUtil.Debugger;
using BeauUtil;


#if UNITY_EDITOR
using ScriptableBake;
using System.IO;
using UnityEditor;
#endif // UNITY_EDITOR

namespace FieldDay.Assets {
    /// <summary>
    /// Default asset package.
    /// </summary>
    [CreateAssetMenu(menuName = "Field Day/Asset Pack", order = -300)]
    public sealed class AssetPack : ScriptableObject, IAssetPackage {
        [SerializeField] private GlobalAsset[] m_GlobalAssets = Array.Empty<GlobalAsset>();
        [SerializeField] private NamedAsset[] m_NamedAssets = Array.Empty<NamedAsset>();
        [SerializeField] private LiteAssetGroup[] m_LiteAssets = Array.Empty<LiteAssetGroup>();

        [NonSerialized] private int m_RefCount;

        #region IAssetPackage

        void IAssetPackage.Mount(AssetMgr mgr) {
            foreach (var global in m_GlobalAssets) {
                mgr.Register(global);
            }

            foreach (var named in m_NamedAssets) {
                mgr.AddNamed(named.name, named);
            }

            foreach(var lite in m_LiteAssets) {
                lite.RegisterAssets(mgr);
            }
        }

        void IAssetPackage.Unmount(AssetMgr mgr) {
            foreach(var global in m_GlobalAssets) {
                mgr.Deregister(global);
            }

            foreach(var named in m_NamedAssets) {
                mgr.RemoveNamed(named.name, named);
            }

            foreach (var lite in m_LiteAssets) {
                lite.DeregisterAssets(mgr);
            }
        }

        bool IRefCountedAsset.AddRef() {
            return (m_RefCount++) == 0;
        }

        bool IRefCountedAsset.RemoveRef() {
            Assert.True(m_RefCount > 0, "Unbalanced AssetPack.AddRef/RemoveRef calls");
            return (m_RefCount--) == 1;
        }

        bool IRefCountedAsset.IsReferenced() {
            return m_RefCount > 0;
        }

        #endregion // IAssetPackage

#if UNITY_EDITOR

        /// <summary>
        /// Refreshes all assets for the given pack from the pack's editor directory.
        /// </summary>
        static public void ReadFromEditorDirectory(AssetPack pack) {
            string myDir = Baking.GetAssetDirectory(pack);
            GlobalAsset[] global = Baking.FindAssets<GlobalAsset>(myDir);
            NamedAsset[] named = Baking.FindAssets<NamedAsset>(myDir);
            LiteAssetGroup[] lite = Baking.FindAssets<LiteAssetGroup>(myDir);

            Array.Sort(pack.m_NamedAssets, (a, b) => a.GetType().FullName.CompareTo(b.GetType().FullName));

            bool isChanged = false;
            if (!ArrayUtils.ContentEquals(pack.m_GlobalAssets, global)) {
                isChanged = true;
                pack.m_GlobalAssets = global;
            }
            if (!ArrayUtils.ContentEquals(pack.m_NamedAssets, named)) {
                isChanged = true;
                pack.m_NamedAssets = named;
            }
            if (!ArrayUtils.ContentEquals(pack.m_LiteAssets, lite)) {
                isChanged = true;
                pack.m_LiteAssets = lite;
            }

            if (isChanged) {
                EditorUtility.SetDirty(pack);
            }
        }

#endif // UNITY_EDITOR
    }
}