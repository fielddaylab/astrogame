
using System;
using System.Collections.Generic;
using BeauUtil;
using FieldDay;
using FieldDay.Scenes;
using FieldDay.Scripting;
using UnityEngine;

namespace Astro
{
    public sealed class ArchiveLoader : MonoBehaviour, IScenePreload
    {
        public IEnumerator<WorkSlicer.Result?> Preload()
        {
            PlayerProgressState playerState = Find.State<PlayerProgressState>();
            DayConfigAsset config = DayConfigUtil.GetConfigForState();
            ArchiveState archiveState = Find.State<ArchiveState>();

            archiveState.CurrArchiveIndex = playerState.DayIndex;
            int neededLength = 1 + archiveState.CurrArchiveIndex - archiveState.DayOffset;
            while (playerState.DayLayouts.Count < neededLength) {
                playerState.DayLayouts.Add(new ArchiveLayout());
            }
            while(playerState.DayLayouts.Count > neededLength) {
                playerState.DayLayouts.RemoveAt(playerState.DayLayouts.Count - 1);
            }

            // create archive stacks according to day (skip intro day)
            for (int i = archiveState.DayOffset; i <= archiveState.CurrArchiveIndex; i++)
            {
                ArchiveUtility.CreateStack(archiveState, i);
            }

            return null;
        }

    }
}