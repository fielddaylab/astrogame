using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.IO.Archive;
using UnityEngine;

namespace Astro
{
    public class ArchiveState : SharedStateComponent, IRegistrationCallbacks
    {
        public GameObject ArchivePrefab;
        public Transform ArchiveParent;

        [NonSerialized] public List<ArchiveLayout> DayLayouts;
        [NonSerialized] public int CurrArchiveIndex = 0;

        public void OnDeregister()
        {
            Game.Events.Deregister(GameEvents.BeforeNextDayLoad, ArchiveUtility.SaveCurrentLayout);
        }

        public void OnRegister()
        {
            Game.Events.Register(GameEvents.BeforeNextDayLoad, ArchiveUtility.SaveCurrentLayout);
        }
    }

    public static class ArchiveUtility { 
    
        public static void AddAssetToArchive(ArchiveState archiveState, StringHash32 assetId, Vector3 assetPos)
        {
            PlayerProgressState playerState = Find.State<PlayerProgressState>();
            var currList = archiveState.DayLayouts[playerState.DayIndex];
            currList.AssetPositions.Add(assetId, assetPos);
            archiveState.DayLayouts[playerState.DayIndex] = currList;
        }

        public static void SetAssetPosInArchive(ArchiveState archiveState, StringHash32 assetId, Vector3 assetPos)
        {
            PlayerProgressState playerState = Find.State<PlayerProgressState>();
            var currList = archiveState.DayLayouts[playerState.DayIndex];
            currList.AssetPositions[assetId] = assetPos;
        }

        public static void SaveCurrentLayout()
        {
            var archiveState = Find.State<ArchiveState>();
            var boardState = Find.State<DocumentBoardState>();

            foreach (var doc in boardState.SpawnedDocuments) {
                ArchiveUtility.SetAssetPosInArchive(archiveState, doc.Interactable.AssetName, doc.transform.localPosition);
            }
        }

        public static void LoadArchive(ArchiveState archiveState, DocumentBoardState boardState, int dayIndex)
        {
            if (dayIndex == archiveState.CurrArchiveIndex) { return; }

            HideCurrentArchive(archiveState, boardState);

            // TODO: Expand transition routine

            var currLayout = archiveState.DayLayouts[dayIndex];

            foreach (KeyValuePair<StringHash32, Vector3> pair in currLayout.AssetPositions) {
                // spawn the asset at the position
                var spawned = DocumentUtility.SpawnDocument(Find.NamedAsset<DocumentAsset>(pair.Key), pair.Key, boardState, false);
                spawned.transform.localPosition = pair.Value;
            }

            archiveState.CurrArchiveIndex = dayIndex;
        }

        public static void HideCurrentArchive(ArchiveState archiveState, DocumentBoardState boardState)
        {
            if (archiveState.CurrArchiveIndex == -1) return;

            // save current layout
            SaveCurrentLayout();

            // TODO: Collapse transition routine

            foreach (var doc in boardState.SpawnedDocuments) {
                GameObject.Destroy(doc.gameObject);
            }

            archiveState.CurrArchiveIndex = -1;
        }

        public static void CreateStack(ArchiveState archiveState, int archiveIndex)
        {
            var newStack = GameObject.Instantiate(archiveState.ArchivePrefab, archiveState.ArchiveParent).GetComponent<ArchiveInteractable>();
            newStack.ArchiveIndex = archiveIndex;

            newStack.transform.localPosition += Vector3.right * (archiveIndex + 0.6f);

            // TODO: assign day number visual
        }
    }

}
