using System;
using System.Collections.Generic;
using BeauUtil;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FieldDay.UI {
    [AddComponentMenu("Event/Filtered Graphic Raycaster")]
    public sealed class FilteredGraphicRaycaster : GraphicRaycaster {
        [SerializeField] private LayerMask m_EventMask = Bits.All32;

        public LayerMask eventMask {
            get { return m_EventMask; }
            set { m_EventMask = value; }
        }

        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList) {
            int prevSize = resultAppendList.Count;
            base.Raycast(eventData, resultAppendList);

            if (m_EventMask != Bits.All32) {
                for (int i = resultAppendList.Count - 1; i >= prevSize; i--) {
                    RaycastResult result = resultAppendList[i];
                    if ((m_EventMask & (1 << result.gameObject.layer)) == 0) {
                        resultAppendList.RemoveAt(i);
                    }
                }
            }
        }
    }
}