using BeauUtil.UI;
using FieldDay;
using FieldDay.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Astro
{
    public class UIFocus : BatchedComponent
    {
        [NonSerialized] public Transform Target;
        [NonSerialized] public CelestialAsset TargetData;

        [NonSerialized] public Renderer TargetRenderer;

        public RectTransform Rect;
        public RectTransform HighlightRect;
        public Image Represent2D;
        public PointerListener Button;
        [NonSerialized] public Image NeutrinoHighlight;
    }

    public struct UIFocusPackedData {
        public Vector3 TargetPos;
        public Vector3 TargetVector;
    }

    public static partial class FocusableUtility
    {
        public static void InitFocusable(FocusState state, UIFocus focus, Transform target, CelestialAsset asset, Sprite represent2D)
        {
            focus.Target = target;
            focus.Represent2D.sprite = represent2D;
            if (represent2D == null) {
                focus.Represent2D.enabled = false;
            }
            focus.TargetData = asset;
            focus.TargetRenderer = target.GetComponent<Renderer>();

            float scaleFactor = Mathf.Pow(0.6f, asset.ApparentMagnitude);
            focus.Rect.localScale = new Vector3(scaleFactor, scaleFactor, 1);

            float invScaleFactor = Mathf.Sqrt(1f / scaleFactor);
            focus.Button.transform.localScale = new Vector3(invScaleFactor, invScaleFactor, 1);
            focus.Button.onClick.RemoveAllListeners();
            focus.Button.onClick.AddListener(() => { FocusableUtility.SetCurrentFocus(state, focus); });
        }
    }
}
