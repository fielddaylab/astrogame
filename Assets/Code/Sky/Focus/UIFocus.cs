using FieldDay.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Astro
{
    public class UIFocus : BatchedComponent
    {
        public Transform Target;

        public RectTransform Rect;
        public Image Represent2D;
        // public Image Outline;
        // public Button Button;
    }

    public static class FocusableUtility
    {
        public static void InitFocusable(UIFocus focus, Transform target, Sprite represent2D)
        {
            focus.Target = target;
            focus.Represent2D.sprite = represent2D;
        }
    }
}
