using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    [SysUpdate(GameLoopPhase.Update, 0, AstroGame.DocumentUpdateMask)]
    public class ArchiveInteractSystem : ComponentSystemBehaviour<LabInteractable, ArchiveInteractable>
    {
        public override void ProcessWork(float deltaTime)
        {
            base.ProcessWork(deltaTime);

            var archiveState = Find.State<ArchiveState>();
            var boardState = Find.State<DocumentBoardState>();

            foreach (var component in m_Components)
            {
                if (!component.Primary.InteractReceived) { continue; }

                // load the relevant archive history
                ArchiveUtility.LoadArchive(archiveState, boardState, component.Secondary.ArchiveIndex);
            }
        }
    }
}