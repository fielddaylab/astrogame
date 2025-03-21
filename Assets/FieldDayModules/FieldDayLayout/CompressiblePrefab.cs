using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using BeauUtil;
using BeauUtil.UI;
using BeauUtil.Variants;
using EasyAssetStreaming;
using FieldDay.Data;
using ScriptableBake;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FieldDay.Layout {
    public sealed class CompressiblePrefab : MonoBehaviour {
        public const int MaxObjects = 200;
        public const int MaxMetadata = 256;
        public const int MaxUncompressedSize = 16 * Unsafe.KiB;

        #region Types

        public struct PrefabHeader {
            public byte CustomFieldCount;
            public byte ObjectCount;
        }

        public struct ObjectHeader {
            public byte ParentIndex;
            public byte Flags;
            public ushort NameIdx;
            public uint ComponentTypes;
        }

        public struct CustomField {
            public StringHash32 Id;
            public Variant Value;
        }

        #endregion // Types

        #region Inspector

        public bool IgnoreInactive;
        public bool PreserveNames;

        #endregion // Inspector

        #region Compress

#if UNITY_EDITOR

        internal unsafe byte[] Compress(CompressedPackageBuilder bank, in CompressedTransformBounds transformBounds, in CompressedRectTransformBounds rectBounds) {
            Undo.IncrementCurrentGroup();

            byte* tempBuff = stackalloc byte[MaxUncompressedSize];
            ByteWriter tempWriter = new ByteWriter(tempBuff, MaxUncompressedSize);

            Baking.UnpackPrefabIfNecessary(transform);
            Baking.BakeHierarchy(gameObject);

            PrefabHeader header;
            header.ObjectCount = 0;
            header.CustomFieldCount = 0;

            uint headerMarker = tempWriter.GetMarker();
            tempWriter.Write(header);

            var customFields = gameObject.GetComponentsInChildren<ICompressiblePrefabCustomDataProvider>(!IgnoreInactive);
            if (customFields.Length > 0) {
                int count = 0;
                foreach(var fieldProvider in customFields) {
                    foreach(var field in fieldProvider.GetCustomFields()) {
                        tempWriter.Write(field);
                        count++;
                    }
                }
                header.CustomFieldCount = (byte) count;
            }

            int objCount = 0;
            WriteHierarchy(transform, -1, ref tempWriter, bank, transformBounds, rectBounds, ref objCount);
            header.ObjectCount = (byte)objCount;

            tempWriter.Overwrite(header, headerMarker);

            Undo.RevertAllInCurrentGroup();
            return tempWriter.GetDataCopy();
        }

        private unsafe void WriteHierarchy(Transform obj, int parentIdx, ref ByteWriter writer, CompressedPackageBuilder bank, in CompressedTransformBounds transformBounds, in CompressedRectTransformBounds rectBounds, ref int objCount) {
            if (IgnoreInactive && !obj.gameObject.activeInHierarchy) {
                return;
            }

            CompressedComponentTypes componentsOfInterest = ComponentsOfInterest(obj);
            int childCount = obj.childCount;

            if (childCount > 0 || (componentsOfInterest & ~CompressedComponentTypes.AnyTransform) != 0) {
                int objId = objCount++;

                ObjectHeader objHeader;
                objHeader.ComponentTypes = (uint) componentsOfInterest;
                objHeader.ParentIndex = (byte) (parentIdx == -1 ? 0 : parentIdx);
                objHeader.Flags = (byte) (parentIdx == -1 ? CompressedPrefabObjectFlags.IsRoot : 0);
                objHeader.NameIdx = bank.AddString(PreserveNames ? obj.gameObject.name : "$");

                writer.Write(objHeader);

                if (HasComponent(objHeader.ComponentTypes, CompressedComponentTypes.Transform)) {
                    CompressedTransform.Compress(obj, transformBounds, out CompressedTransform data);
                    writer.Write(data);
                }

                if (HasComponent(objHeader.ComponentTypes, CompressedComponentTypes.RectTransform)) {
                    RectTransform rect = (RectTransform) obj;
                    CompressedRectTransform.Compress(rect, rectBounds, out CompressedRectTransform data);
                    writer.Write(data);
                }

                if (HasComponent(objHeader.ComponentTypes, CompressedComponentTypes.RectGraphic)) {
                    RectGraphic graphic = obj.GetComponent<RectGraphic>();
                    CompressedRectGraphic.Compress(graphic, out CompressedRectGraphic data);
                    writer.Write(data);
                }

                if (HasComponent(objHeader.ComponentTypes, CompressedComponentTypes.Image)) {
                    Image graphic = obj.GetComponent<Image>();
                    CompressedImage.Compress(bank, graphic, out CompressedImage data);
                    writer.Write(data);
                }

                if (HasComponent(objHeader.ComponentTypes, CompressedComponentTypes.StreamingUGUITexture)) {
                    StreamingUGUITexture graphic = obj.GetComponent<StreamingUGUITexture>();
                    CompressedStreamingUGUITexture.Compress(bank, graphic, out CompressedStreamingUGUITexture data);
                    writer.Write(data);
                }

                if (HasComponent(objHeader.ComponentTypes, CompressedComponentTypes.TextMeshPro)) {
                    TMP_Text graphic = obj.GetComponent<TMP_Text>();
                    CompressedTextMeshPro.Compress(bank, graphic, out CompressedTextMeshPro data);
                    writer.Write(data);
                }

                //if (HasComponent(objHeader.ComponentTypes, CompressedComponentTypes.LocText)) {
                //    LocText locText = obj.GetComponent<LocText>();
                //    CompressedLocText.Compress(locText, out CompressedLocText data);
                //    writer.Write(data);
                //}

                for (int i = 0; i < childCount; i++) {
                    WriteHierarchy(obj.GetChild(i), objId, ref writer, bank, transformBounds, rectBounds, ref objCount);
                }
            }
        }

        static private CompressedComponentTypes ComponentsOfInterest(Transform transform) {
            CompressedComponentTypes types = 0;

            if (transform.GetType() == typeof(RectTransform)) {
                types |= CompressedComponentTypes.RectTransform;
            } else {
                types |= CompressedComponentTypes.Transform;
            }

            if (HasBehavior<RectGraphic>(transform)) {
                types |= CompressedComponentTypes.RectGraphic;
            } else if (HasBehavior<Image>(transform)) {
                types |= CompressedComponentTypes.Image;
            } else if (HasBehavior<StreamingUGUITexture>(transform)) {
                types |= CompressedComponentTypes.StreamingUGUITexture;
            } else if (HasBehavior<TMP_Text>(transform)) {
                types |= CompressedComponentTypes.TextMeshPro;
            }

            if (HasRenderer<SpriteRenderer>(transform)) {
                types |= CompressedComponentTypes.SpriteRenderer;
            }

            //if (HasBehavior<LocText>(transform)) {
            //    types |= CompressedComponentTypes.LocText;
            //}

            return types;
        }

        static private bool HasBehavior<T>(Transform transform) where T : Behaviour {
            T obj = transform.GetComponent<T>();
            return obj && obj.enabled;
        }

        static private bool HasRenderer<T>(Transform transform) where T : Renderer {
            T obj = transform.GetComponent<T>();
            return obj && obj.enabled;
        }

#endif // UNITY_EDITOR

        #endregion // Compress

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private bool HasComponent(CompressedComponentTypes all, CompressedComponentTypes type) {
            return (all & type) == type;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private bool HasComponent(uint all, CompressedComponentTypes type) {
            return ((CompressedComponentTypes) all & type) == type;
        }
    }

    /// <summary>
    /// Interface that provides metadata to a CompressiblePrefab.
    /// </summary>
    public interface ICompressiblePrefabCustomDataProvider {
        IEnumerable<CompressiblePrefab.CustomField> GetCustomFields();
    }

    /// <summary>
    /// Object flags.
    /// </summary>
    [Flags]
    public enum CompressedPrefabObjectFlags : byte {
        IsRoot = 0x01
    }

    [Flags]
    public enum CompressedComponentTypes : uint {
        Transform = 0x01,
        RectTransform = 0x02,
        RectGraphic = 0x04,
        Image = 0x08,
        StreamingUGUITexture = 0x10,
        TextMeshPro = 0x20,
        LocText = 0x40,
        SpriteRenderer = 0x80,

        [Hidden] AnyTransform = Transform | RectTransform
    }
}