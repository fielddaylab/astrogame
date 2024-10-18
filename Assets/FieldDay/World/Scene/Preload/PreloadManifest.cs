using System;
using System.Collections.Generic;
using BeauUtil;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FieldDay.Scenes {
    /// <summary>
    /// Contains an array of scene preload callbacks.
    /// </summary>
    [Serializable]
    public class PreloadManifest {
        [Serializable]
        internal struct BucketInfo {
            public int Order;
            public int LoaderOffset;
            public int LoaderCount;
        }

        [SerializeField] internal Component[] Loaders;
        [SerializeField] internal BucketInfo[] Buckets;

        #region Generation

#if UNITY_EDITOR

        static private readonly AttributeCache<PreloadOrderAttribute, int> OrderAttribute = new AttributeCache<PreloadOrderAttribute, int>((a, m) => a.Order, 8);

        static private readonly Comparison<IScenePreload> Sorter = (a, b) => {
            Type typeA = a.GetType(), typeB = b.GetType();
            int orderComp = OrderAttribute.Get(typeA) - OrderAttribute.Get(typeB);
            if (orderComp != 0) {
                return orderComp;
            }
            if (typeA == typeB) {
                return 0; // try to group components of the same type together
            }
            return typeA.GUID.CompareTo(typeB.GUID);
        };

        /// <summary>
        /// Generates a preload manifest for the given scene.
        /// </summary>
        static public PreloadManifest Generate(Scene scene) {
            GameObject[] roots = scene.GetRootGameObjects();
            return Generate(roots);
        }

        /// <summary>
        /// Generates a preload manifest for the given set of GameObjects.
        /// </summary>
        static public PreloadManifest Generate(IEnumerable<GameObject> roots) {
            return Generate(GetTransforms(roots));
        }

        /// <summary>
        /// Generates a preload manifest for the given set of Transform roots.
        /// </summary>
        static public PreloadManifest Generate(IEnumerable<Transform> roots) {
            List<IScenePreload> preloaders = new List<IScenePreload>(128);
            List<IScenePreload> preloadersTemp = new List<IScenePreload>(16);

            foreach (var transform in roots) {
                transform.GetComponentsInChildren<IScenePreload>(true, preloadersTemp);
                preloaders.AddRange(preloadersTemp);
            }

            preloaders.Sort(Sorter);

            PreloadManifest manifest = new PreloadManifest();
            manifest.Loaders = ArrayUtils.MapFrom(preloaders, (a) => (Component) a);

            if (preloaders.Count > 0) {
                List<BucketInfo> buckets = new List<BucketInfo>(4);

                int loaderPtr = 1, bucketOrder = OrderAttribute.Get(preloaders[0].GetType()), bucketStart = 0;
                while (loaderPtr < preloaders.Count) {
                    int nextBucket = OrderAttribute.Get(preloaders[loaderPtr].GetType());
                    if (nextBucket != bucketOrder) {
                        int count = loaderPtr - bucketStart;
                        if (count > 0) {
                            buckets.Add(new BucketInfo() {
                                Order = bucketOrder,
                                LoaderOffset = bucketStart,
                                LoaderCount = count
                            });
                        }
                        bucketStart = loaderPtr;
                        bucketOrder = nextBucket;
                    }
                    loaderPtr++;
                }

                buckets.Add(new BucketInfo() {
                    Order = bucketOrder,
                    LoaderOffset = bucketStart,
                    LoaderCount = loaderPtr - bucketStart
                });

                manifest.Buckets = buckets.ToArray();
            } else {
                manifest.Buckets = Array.Empty<BucketInfo>();
            }

            return manifest;
        }

        static private IEnumerable<Transform> GetTransforms(IEnumerable<GameObject> gameObjects) {
            foreach(var go in gameObjects) {
                yield return go.transform;
            }
        }

#endif // UNITY_EDITOR

        #endregion // Generation

        #region Reader

        /// <summary>
        /// Reads blocks of preloaded objects.
        /// </summary>
        public class Reader {
            private PreloadManifest[] m_Manifests = Array.Empty<PreloadManifest>();
            private int[] m_BucketIndices = Array.Empty<int>();
            private BitSet64 m_ReadingManifestMask;
            private int m_ManifestCount;

            /// <summary>
            /// Initializes the reader for a single manifest.
            /// </summary>
            public void Init(PreloadManifest manifest) {
                Clear();
                Reserve(1);

                m_Manifests[0] = manifest;
                m_ReadingManifestMask.Set(0, manifest.Buckets.Length > 0);
            }

            /// <summary>
            /// Initializes the reader for an array of manifests.
            /// </summary>
            public void Init(PreloadManifest[] manifests) {
                Clear();
                Reserve(manifests.Length);

                for (int i = 0; i < manifests.Length; i++) {
                    m_Manifests[i] = manifests[i];
                    m_ReadingManifestMask.Set(i, manifests[i].Buckets.Length > 0);
                }
                m_ManifestCount = manifests.Length;
            }

            /// <summary>
            /// Initializes the reader for a list of manifests.
            /// </summary>
            public void Init(IReadOnlyList<PreloadManifest> manifests) {
                Clear();
                Reserve(manifests.Count);

                for(int i = 0; i < manifests.Count; i++) {
                    m_Manifests[i] = manifests[i];
                    m_ReadingManifestMask.Set(i, manifests[i].Buckets.Length > 0);
                }
                m_ManifestCount = manifests.Count;
            }

            private void Reserve(int count) {
                if (count > 64) {
                    throw new ArgumentOutOfRangeException("count", "Cannot read from more than 64 manifests at once");
                }

                if (m_Manifests.Length < count) {
                    Array.Resize(ref m_Manifests, count);
                    Array.Resize(ref m_BucketIndices, count);
                }
            }

            /// <summary>
            /// Reads the next block of preloaders.
            /// </summary>
            public int Read(IList<IScenePreload> nextBlock) {
                BitSet64 readMask = GetNextManifestReadMask();
                if (readMask) {
                    return ReadManifests(readMask, nextBlock);
                }
                return 0;
            }

            private BitSet64 GetNextManifestReadMask() {
                if (m_ReadingManifestMask.IsEmpty) {
                    return default;
                }

                int lowest = int.MaxValue;
                int length = m_ManifestCount;
                BitSet64 toRead = default;
                
                for (int i = 0; i < length; i++) {
                    if (!m_ReadingManifestMask.IsSet(i)) {
                        continue;
                    }

                    PreloadManifest manifest = m_Manifests[i];
                    int idx = m_BucketIndices[i];
                    int order = manifest.Buckets[idx].Order;
                    if (order < lowest) {
                        lowest = order;
                        toRead.Clear();
                        toRead.Set(i);
                    } else if (order == lowest) {
                        toRead.Set(i);
                    }
                }

                return toRead;
            }

            private int ReadManifests(BitSet64 mask, IList<IScenePreload> preloadBlock) {
                int length = m_ManifestCount;
                int count = 0;
                for (int i = 0; i < length; i++) {
                    if (!mask.IsSet(i)) {
                        continue;
                    }

                    PreloadManifest manifest = m_Manifests[i];
                    ref int idx = ref m_BucketIndices[i];
                    var range = manifest.Buckets[idx];
                    for(int j = 0; j < range.LoaderCount; j++) {
                        preloadBlock.Add((IScenePreload) manifest.Loaders[range.LoaderOffset + j]);
                    }
                    count += range.LoaderCount;

                    if (++idx >= manifest.Buckets.Length) {
                        m_ReadingManifestMask.Unset(i);
                    }
                }
                return count;
            }

            /// <summary>
            /// Clears the reader state.
            /// </summary>
            public void Clear() {
                int idx = m_ManifestCount;
                while(idx-- > 0) {
                    m_Manifests[idx] = null;
                    m_BucketIndices[idx] = 0;
                }
                m_ManifestCount = 0;
                m_ReadingManifestMask.Clear();
            }
        }

        /// <summary>
        /// WorkSlicer operation.
        /// </summary>
        static public readonly WorkSlicer.EnumeratedElementOperation<IScenePreload> ExecutePreloader = (a) => a.Preload();

        #endregion // Reader
    }
}