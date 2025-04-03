using System;
using BeauUtil;
using BeauUtil.UI;
using EasyAssetStreaming;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FieldDay.Layout {
    public struct PrefabDecompressor {
        #region Types

        public delegate GameObject NewObjectDelegate(string name, CompressedPrefabObjectFlags flags, CompressedComponentTypes componentTypes, GameObject parent);
        public delegate Component NewComponentDelegate(GameObject obj, CompressedComponentTypes componentType);

        #endregion // Types

        // Compression Bounds
        public CompressedTransformBounds TransformBounds;
        public CompressedRectTransformBounds RectTransformBounds;

        // Functions
        public NewObjectDelegate NewObject;
        public NewComponentDelegate NewComponent;

        // Resource Cache
        public CompressedPackageAssetCache Cache;

        #region Defaults

        static public readonly NewObjectDelegate DefaultNewObject = (n, f, c, p) => {
            GameObject go = new GameObject(n);
            if (p != null) {
                go.transform.SetParent(p.transform);
            }
            return go;
        };

        static public readonly NewComponentDelegate DefaultNewComponent = (o, t) => {
            switch (t) {
                case CompressedComponentTypes.Transform: {
                    return o.transform;
                }
                case CompressedComponentTypes.RectTransform: {
                    return o.EnsureComponent<RectTransform>();
                }
                case CompressedComponentTypes.RectGraphic: {
                    return o.EnsureComponent<RectGraphic>();
                }
                case CompressedComponentTypes.Image: {
                    return o.EnsureComponent<Image>();
                }
                case CompressedComponentTypes.StreamingUGUITexture: {
                    return o.EnsureComponent<StreamingUGUITexture>();
                }
                case CompressedComponentTypes.TextMeshPro: {
                    return o.EnsureComponent<TextMeshProUGUI>();
                }
                //case CompressedComponentTypes.LocText: {
                //    return o.EnsureComponent<LocText>();
                //}
                default: {
                    throw new ArgumentException("Unable to instantiate component of type " + t.ToString());
                }
            }
        };

        #endregion // Default
    }
}