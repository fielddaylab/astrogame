using System;
using System.Runtime.InteropServices;
using FieldDay.Data;
using UnityEngine;

namespace FieldDay.Layout {
    #region Transform

    public struct CompressedTransform {
        public ushort PosX;
        public ushort PosY;
        public ushort PosZ;
        public ushort ScaleX;
        public ushort ScaleY;
        public ushort ScaleZ;
        public ushort RotationX;
        public ushort RotationY;
        public ushort RotationZ;

        static public void Compress(Transform transform, in CompressedTransformBounds bounds, out CompressedTransform data) {
            Vector3 localPos = transform.localPosition;
            data.PosX = CompressionRange.Encode16(bounds.Pos, localPos.x);
            data.PosY = CompressionRange.Encode16(bounds.Pos, localPos.y);
            data.PosZ = CompressionRange.Encode16(bounds.Pos, localPos.z);

            Vector3 localScale = transform.localScale;
            data.ScaleX = CompressionRange.Encode16(bounds.Scale, localScale.x);
            data.ScaleY = CompressionRange.Encode16(bounds.Scale, localScale.y);
            data.ScaleZ = CompressionRange.Encode16(bounds.Scale, localScale.z);

            Vector3 localRot = transform.localEulerAngles;
            data.RotationX = CompressionRange.Encode16(CompressedTransformBounds.Rotation, localRot.x);
            data.RotationY = CompressionRange.Encode16(CompressedTransformBounds.Rotation, localRot.y);
            data.RotationZ = CompressionRange.Encode16(CompressedTransformBounds.Rotation, localRot.z);
        }

        static public void Decompress(in CompressedTransform data, in CompressedTransformBounds bounds, Transform transform) {
            transform.localPosition = new Vector3(CompressionRange.Decode16(bounds.Pos, data.PosX), CompressionRange.Decode16(bounds.Pos, data.PosY), CompressionRange.Decode16(bounds.Pos, data.PosZ));
            transform.localScale = new Vector3(CompressionRange.Decode16(bounds.Scale, data.ScaleX), CompressionRange.Decode16(bounds.Scale, data.ScaleY), CompressionRange.Decode16(bounds.Scale, data.ScaleZ));
            transform.localEulerAngles = new Vector3(CompressionRange.Decode16(CompressedTransformBounds.Rotation, data.RotationX), CompressionRange.Decode16(CompressedTransformBounds.Rotation, data.RotationY), CompressionRange.Decode16(CompressedTransformBounds.Rotation, data.RotationZ));
        }
    }

    [Serializable]
    public struct CompressedTransformBounds {
        public CompressionRange Pos;
        public CompressionRange Scale;

        // rotation range fixed from 0-360 
        static public readonly CompressionRange Rotation = new CompressionRange(0, 360);

        static private readonly CompressedTransformBounds s_Default = new CompressedTransformBounds() {
            Pos = new CompressionRange(-2048, 2048),
            Scale = new CompressionRange(-128, 128)
        };

        static public CompressedTransformBounds Default { get { return s_Default; } }
    }

    #endregion // Transform

    #region RectTransform

    [StructLayout(LayoutKind.Explicit, Size = 20)]
    public struct CompressedRectTransform {
        [FieldOffset(0)] public ushort AnchorPosX;
        [FieldOffset(2)] public ushort AnchorPosY;
        [FieldOffset(4)] public ushort SizeDeltaX;
        [FieldOffset(6)] public ushort SizeDeltaY;
        [FieldOffset(8)] public ushort ScaleX;
        [FieldOffset(10)] public ushort ScaleY;
        [FieldOffset(12)] public ushort RotationZ;
        [FieldOffset(14)] public byte AnchorMinX;
        [FieldOffset(15)] public byte AnchorMinY;
        [FieldOffset(16)] public byte AnchorMaxX;
        [FieldOffset(17)] public byte AnchorMaxY;
        [FieldOffset(18)] public byte PivotX;
        [FieldOffset(19)] public byte PivotY;

        static public void Compress(RectTransform rect, in CompressedRectTransformBounds bounds, out CompressedRectTransform data) {
            data.AnchorPosX = CompressionRange.Encode16(bounds.AnchorPos, rect.anchoredPosition.x);
            data.AnchorPosY = CompressionRange.Encode16(bounds.AnchorPos, rect.anchoredPosition.y);
            data.SizeDeltaX = CompressionRange.Encode16(bounds.SizeDelta, rect.sizeDelta.x);
            data.SizeDeltaY = CompressionRange.Encode16(bounds.SizeDelta, rect.sizeDelta.y);
            data.AnchorMinX = CompressionRange.Encode8(CompressionRange.ZeroToOne, rect.anchorMin.x);
            data.AnchorMinY = CompressionRange.Encode8(CompressionRange.ZeroToOne, rect.anchorMin.y);
            data.AnchorMaxX = CompressionRange.Encode8(CompressionRange.ZeroToOne, rect.anchorMax.x);
            data.AnchorMaxY = CompressionRange.Encode8(CompressionRange.ZeroToOne, rect.anchorMax.y);
            data.PivotX = CompressionRange.Encode8(CompressionRange.ZeroToOne, rect.pivot.x);
            data.PivotY = CompressionRange.Encode8(CompressionRange.ZeroToOne, rect.pivot.y);
            data.ScaleX = CompressionRange.Encode16(bounds.Scale, rect.localScale.x);
            data.ScaleY = CompressionRange.Encode16(bounds.Scale, rect.localScale.y);
            data.RotationZ = CompressionRange.Encode16(CompressedRectTransformBounds.Rotation, rect.localEulerAngles.z);
        }

        static public void Decompress(in CompressedRectTransform data, in CompressedRectTransformBounds bounds, RectTransform rect) {
            rect.anchoredPosition = new Vector2(CompressionRange.Decode16(bounds.AnchorPos, data.AnchorPosX, 0.25f), CompressionRange.Decode16(bounds.AnchorPos, data.AnchorPosY, 0.25f));
            rect.sizeDelta = new Vector2(CompressionRange.Decode16(bounds.SizeDelta, data.SizeDeltaX, 0.5f), CompressionRange.Decode16(bounds.SizeDelta, data.SizeDeltaY, 0.5f));
            rect.anchorMin = new Vector2(CompressionRange.Decode8(CompressionRange.ZeroToOne, data.AnchorMinX), CompressionRange.Decode8(CompressionRange.ZeroToOne, data.AnchorMinY));
            rect.anchorMax = new Vector2(CompressionRange.Decode8(CompressionRange.ZeroToOne, data.AnchorMaxX), CompressionRange.Decode8(CompressionRange.ZeroToOne, data.AnchorMaxY));
            rect.pivot = new Vector2(CompressionRange.Decode8(CompressionRange.ZeroToOne, data.PivotX), CompressionRange.Decode8(CompressionRange.ZeroToOne, data.PivotY));
            rect.localScale = new Vector3(CompressionRange.Decode16(bounds.Scale, data.ScaleX, 0.005f), CompressionRange.Decode16(bounds.Scale, data.ScaleY, 0.005f), 1);
            rect.localEulerAngles = new Vector3(0, 0, CompressionRange.Decode16(CompressedRectTransformBounds.Rotation, data.RotationZ));
        }
    }

    [Serializable]
    public struct CompressedRectTransformBounds {
        public CompressionRange AnchorPos;
        public CompressionRange SizeDelta;
        public CompressionRange Scale;
        
        static public readonly CompressionRange Rotation = new CompressionRange(0, 360);

        static private readonly CompressedRectTransformBounds s_Default = new CompressedRectTransformBounds() {
            AnchorPos = new CompressionRange(-2048, 2048),
            SizeDelta = new CompressionRange(-2048, 2048),
            Scale = new CompressionRange(-128, 128)
        };

        static public CompressedRectTransformBounds Default { get { return s_Default; } }
    }

    #endregion // RectTransform
}