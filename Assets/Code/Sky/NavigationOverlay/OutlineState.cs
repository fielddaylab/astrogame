using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Astro {
    public class OutlineState : SharedStateComponent {
        [NonSerialized] public RingBuffer<UIFocus> ActiveOutlines = new RingBuffer<UIFocus>(8);
        public Graphic FocusOutline;
        public Prefab Connection;

        protected override void OnEnable() {
            base.OnEnable();
            ActiveOutlines.BufferMode = RingBufferMode.Expand;
        }
    }
}