using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    public class ArchiveInteractSystem : ComponentSystemBehaviour<LabInteractable, ArchiveInteractable>
    {
        public override void ProcessWork(float deltaTime)
        {
            base.ProcessWork(deltaTime);

            var archiveState = Find.State<ArchiveState>();
            var boardState = Find.State<DocumentBoardState>();

            foreach (var component in m_Components)
            {
                if (!component.Primary.InteractReceived) { return; }

                // load the relevant archive history
                ArchiveUtility.LoadArchive(archiveState, boardState, component.Secondary.ArchiveIndex);
            }
        }
    }
}