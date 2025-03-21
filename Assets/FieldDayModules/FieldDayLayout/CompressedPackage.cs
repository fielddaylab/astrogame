using System;
using System.Collections.Generic;
using System.IO;
using BeauUtil;
using UnityEngine;

namespace FieldDay.Layout {
    /// <summary>
    /// Builder for an indexed resource bank.
    /// </summary>
    public struct IndexedBankBuilder<T> {
        public readonly Dictionary<T, ushort> Lookup;
        public readonly List<T> Table;
        public ushort Count;

        public IndexedBankBuilder(int initialCapacity) {
            Lookup = MapUtils.Create<T, ushort>(initialCapacity);
            Table = new List<T>(initialCapacity);
            Count = 0;
        }

        public ushort Add(in T value) {
            ushort index;
            if (!Lookup.TryGetValue(value, out index)) {
                index = Count++;
                Lookup.Add(value, index);
                Table.Add(value);
            }
            return index;
        }

        public ushort AddDirectly(in T value) {
            ushort index = Count++;
            Lookup.Add(value, index);
            Table.Add(value);
            return index;
        }

        public void AddLookupOnly(in T value, ushort index) {
            Lookup.Add(value, index);
        }

        public bool TryGet(in T value, out ushort index) {
            return Lookup.TryGetValue(value, out index);
        }
    }

    /// <summary>
    /// Builder for a set of resource banks.
    /// </summary>
    public sealed class CompressedPackageBuilder {
        public IndexedBankBuilder<string> Strings = new IndexedBankBuilder<string>(512);
        public IndexedBankBuilder<UnityEngine.Object> Assets = new IndexedBankBuilder<UnityEngine.Object>(512);

        /// <summary>
        /// Adds a reference to a string.
        /// </summary>
        public ushort AddString(string value) {
            return Strings.Add(value);
        }

        /// <summary>
        /// Adds a reference to an asset.
        /// If this asset exists in a Resources folder, this will instead
        /// add a reference to the Resources path.
        /// </summary>
        public ushort AddAsset(UnityEngine.Object asset) {
            ushort index;

            if (Assets.TryGet(asset, out index))
                return index;

            string resourcePath = GetResourcePath(asset);
            if (!string.IsNullOrEmpty(resourcePath)) {
                ushort pathIndex = AddString(resourcePath);
                index = (ushort) (CompressedPackageBank.LoadAssetIndexFlag | pathIndex);
                Assets.AddLookupOnly(asset, index);
                return index;
            }

            index = Assets.AddDirectly(asset);
            return index;
        }

        static private string GetResourcePath(UnityEngine.Object obj) {
#if UNITY_EDITOR
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(assetPath)) {
                return null;
            }

            int firstResourcesIdx = assetPath.IndexOf("/Resources/");
            if (firstResourcesIdx >= 0) {
                assetPath = assetPath.Substring(firstResourcesIdx + 11);
                assetPath = Path.ChangeExtension(assetPath, null);
                return assetPath;
            }
#endif // UNITY_EDITOR

            return null;
        }
    }

    /// <summary>
    /// CompressedPackage string and asset reference bank.
    /// </summary>
    [Serializable]
    public sealed class CompressedPackageBank {
        public const ushort NullIndex = ushort.MaxValue;
        public const ushort MaxIndex = (1 << 14) - 2;
        public const ushort LoadAssetIndexFlag = (1 << 15) - 1;

        [Multiline] public string[] StringTable = Array.Empty<string>();
        public UnityEngine.Object[] AssetTable = Array.Empty<UnityEngine.Object>();

        public CompressedPackageBank() { }

#if UNITY_EDITOR

        public CompressedPackageBank(CompressedPackageBuilder builder) {
            StringTable = builder.Strings.Table.ToArray();
            AssetTable = builder.Assets.Table.ToArray();
        }

#endif // UNITY_EDITOR

        /// <summary>
        /// Reads a string from the given index.
        /// </summary>
        public string ReadString(ushort index) {
            return index == NullIndex ? string.Empty : StringTable[index];
        }

        /// <summary>
        /// Reads an asset from the given index.
        /// </summary>
        public T ReadAsset<T>(ushort index, CompressedPackageAssetCache cache) where T : UnityEngine.Object {
            UnityEngine.Object asset;
            if (index == NullIndex) {
                asset = null;
            } else if ((index & LoadAssetIndexFlag) != 0) {
                ushort unmaskedPath = (ushort) (index & ~LoadAssetIndexFlag);
                if (cache != null) { 
                    if (!cache.Assets.TryGetValue(index, out asset)) {
                        asset = Resources.Load<T>(ReadString(unmaskedPath));
                        cache.Assets.Add(index, asset);
                    }
                } else {
                    asset = Resources.Load<T>(ReadString(unmaskedPath));
                }
            } else {
                asset = AssetTable[index];
            }
            return (T) asset;
        }
    }

    public sealed class CompressedPackageAssetCache {
        public readonly Dictionary<ushort, UnityEngine.Object> Assets = new Dictionary<ushort, UnityEngine.Object>();
    }
}