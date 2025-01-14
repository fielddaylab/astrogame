using FieldDay;
using FieldDay.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Astro
{
    public class UIFocus : BatchedComponent
    {
        [HideInInspector] public Transform Target;
        [HideInInspector] public CelestialAsset TargetData;

        public RectTransform Rect;
        public Image Represent2D;
        public Button Button;
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

            float scaleFactor = 1.5f * Mathf.Pow(0.63f, asset.ApparentMagnitude);
            focus.Rect.localScale = new Vector3(scaleFactor, scaleFactor, 1);

            focus.Button.onClick.RemoveAllListeners();
            focus.Button.onClick.AddListener(() => { FocusableUtility.SetCurrentFocus(state, focus); });
        }
    }
}
