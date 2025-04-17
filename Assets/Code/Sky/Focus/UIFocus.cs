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

        public Transform Root;
        public SpriteRenderer Represent2D;
        public SphereCollider Clickable;
        public PointerListener Button;

        private void Awake() {
            Button.onClick.Register(OnClicked);
        }

        private void OnClicked() {
            FocusableUtility.SetCurrentFocus(Find.State<FocusState>(), this);
        }
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

            float scaleFactor = Mathf.Pow(0.6f, asset.ApparentMagnitude);
            focus.Root.localScale = new Vector3(scaleFactor, scaleFactor, 1);

            float invScaleFactor = Mathf.Sqrt(1f / scaleFactor);
            focus.Clickable.radius = 0.5f * invScaleFactor;
        }
    }
}
