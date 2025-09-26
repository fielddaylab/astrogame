
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.HID;
using FieldDay.Systems;
using System.Collections;
using UnityEngine;

namespace Astro {
    [SysUpdate(GameLoopPhase.Update, 1, AllowExecutionDuringLoad = true)]
    public class DocumentLoadSystem : SharedStateSystemBehaviour<DocumentBoardState> {
        public override bool HasWork() {
            return base.HasWork() && m_State.DocumentLoadQueue.Count > 0;
        }

        public override void ProcessWork(float deltaTime) {
            // Check if we are still processing the first document in our load queue
            if (m_State.DocumentLoadQueue.PeekFront().Exists()) return;

            // Move to the next document
            m_State.DocumentLoadQueue.PopFront();
        }
    }
}