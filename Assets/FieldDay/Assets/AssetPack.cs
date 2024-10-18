using System;
using UnityEngine;
using BeauUtil.Debugger;
using FieldDay.Asset;



#if UNITY_EDITOR
using ScriptableBake;
using System.IO;
using UnityEditor;
#endif // UNITY_EDITOR

namespace FieldDay.Assets {
    /// <summary>
    /// Default asset package.
    /// </summary>
    [CreateAssetMenu(menuName = "Field Day/Assets/Asset Pack")]
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
            Baking.PrepareUndo(pack, "locating all assets in directory");
            string myDir = Baking.GetAssetDirectory(pack);
            pack.m_GlobalAssets = Baking.FindAssets<GlobalAsset>(myDir);
            pack.m_NamedAssets = Baking.FindAssets<NamedAsset>(myDir);
            pack.m_LiteAssets = Baking.FindAssets<LiteAssetGroup>(myDir);

            Array.Sort(pack.m_NamedAssets, (a, b) => a.GetType().FullName.CompareTo(b.GetType().FullName));
        }

#endif // UNITY_EDITOR
    }
}