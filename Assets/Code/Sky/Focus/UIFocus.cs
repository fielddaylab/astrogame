
using System;
using FieldDay;
using FieldDay.Components;
using BeauUtil.UI;
using UnityEngine;
using BeauRoutine;

namespace Astro {
    public class UIFocus : BatchedComponent {
        [NonSerialized] public Transform Target;
        [NonSerialized] public CelestialAsset TargetData;
        [NonSerialized] public bool IsVisibleInCurrentFilter;

        [NonSerialized] public bool HasHighlight;
        [NonSerialized] public SpriteRenderer Highlight;

        public Transform Root;
        public SpriteRenderer Represent2D;
        public SpriteRenderer TrackerSprite;
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

    public static partial class FocusableUtility {
        public static void InitFocusable(FocusState state, UIFocus focus, Transform target, CelestialAsset asset, Sprite represent2D, Sprite trackerSprite = null) {
#if UNITY_EDITOR
            focus.gameObject.name = asset.DisplayName;
            focus.Button.name = asset.DisplayName + " (Button)";
#endif // UNITY_EDITOR

            focus.Target = target;
            if (represent2D == null) {
                focus.Represent2D.enabled = false;
            }
            focus.Represent2D.sprite = represent2D;
            focus.Represent2D.sprite = trackerSprite;

            focus.TargetData = asset;

            float baseVal = 0.88f;
            float minVal = 0.05f;
            float maxVal = 0.32f;
            float scaleFactor = Mathf.Clamp(Mathf.Pow(baseVal, asset.ApparentMagnitude) - 0.45f, minVal, maxVal);
            focus.Root.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
            Vector3 currTrackerScale = focus.TrackerSprite.GetComponent<Transform>().localScale;
            focus.TrackerSprite.GetComponent<Transform>().localScale = new Vector3(currTrackerScale.x / scaleFactor, currTrackerScale.y / scaleFactor, 1f);

            float clickableRadius = baseVal * Math.Min(1, 1 / scaleFactor);

            focus.Clickable.radius = clickableRadius / scaleFactor;
        }
    }
}
