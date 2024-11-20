using BeauUtil;
using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    public class FocusState : SharedStateComponent
    {
        public RingBuffer<UIFocus> ActiveFocii = new RingBuffer<UIFocus>(8);
    }
}