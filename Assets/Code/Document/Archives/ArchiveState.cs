using BeauRoutine;
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
    public class ArchiveState : SharedStateComponent, IRegistrationCallbacks, ISharedState
    {
        public GameObject ArchivePrefab;
        public Mesh[] StackMeshes;
        public Transform[] StackPoses;
        public Transform ArchiveParent;

        [NonSerialized] public int DayOffset = 1; // number of days without stacks (e.g. prelude)
        [NonSerialized] public int CurrArchiveIndex = -1;

        public Routine LoadRoutine;

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
            var currList = playerState.DayLayouts[archiveState.CurrArchiveIndex - archiveState.DayOffset];
            currList.AssetPositions.Add(assetId, assetPos);
            playerState.DayLayouts[playerState.DayIndex - archiveState.DayOffset] = currList;
        }

        public static void SetAssetPosInArchive(ArchiveState archiveState, StringHash32 assetId, Vector3 assetPos)
        {
            PlayerProgressState playerState = Find.State<PlayerProgressState>();
            var currList = playerState.DayLayouts[archiveState.CurrArchiveIndex - archiveState.DayOffset];
            if (!currList.AssetPositions.ContainsKey(assetId)) { return; }
            currList.AssetPositions[assetId] = assetPos;
        }

        public static void SaveCurrentLayout()
        {
            var archiveState = Find.State<ArchiveState>();
            var boardState = Find.State<DocumentBoardState>();

            foreach (var doc in boardState.SpawnedDocuments) {
                if (doc.PreserveInArchive) {
                    ArchiveUtility.SetAssetPosInArchive(archiveState, doc.Interactable.AssetName, doc.transform.localPosition);
                }
            }
        }

        public static void LoadArchive(ArchiveState archiveState, DocumentBoardState boardState, int dayIndex)
        {
            if (dayIndex == archiveState.CurrArchiveIndex) { return; }
            if (archiveState.LoadRoutine.Exists()) { return; }

            HideCurrentArchive(archiveState, boardState);

            PlayerProgressState playerState = Find.State<PlayerProgressState>();
            var currLayout = playerState.DayLayouts[dayIndex - archiveState.DayOffset];

            archiveState.LoadRoutine.Replace(LoadArchiveRoutine(archiveState, boardState, dayIndex, currLayout));
        }

        public static IEnumerator LoadArchiveRoutine(ArchiveState archiveState, DocumentBoardState boardState, int dayIndex, ArchiveLayout currLayout)
        {
            Transform[] transforms = new Transform[currLayout.AssetPositions.Count];

            while (boardState.DocumentLoadRoutine.Exists()) { yield return null; }

            int pairIndex = 0;
            foreach (KeyValuePair<StringHash32, Vector3> pair in currLayout.AssetPositions)
            {
                // spawn the asset at the position
                var spawned = DocumentUtility.SpawnDocument(Find.NamedAsset<DocumentAsset>(pair.Key), pair.Key, out Vector3 pinnedPos, boardState, false, true);
                transforms[pairIndex] = spawned.transform;
                spawned.transform.position = new Vector3(-500, -500, 500); // place somewhere offscreen while loading
                pairIndex++;

                while (boardState.DocumentLoadRoutine.Exists()) { yield return null; }
            }

            pairIndex = 0;
            foreach (KeyValuePair<StringHash32, Vector3> pair in currLayout.AssetPositions)
            {
                transforms[pairIndex].localPosition = pair.Value;
                pairIndex++;
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
            boardState.SpawnedDocuments.Clear();

            archiveState.CurrArchiveIndex = -1;
        }

        public static void CreateStack(ArchiveState archiveState, int archiveIndex)
        {
            var newStack = GameObject.Instantiate(archiveState.ArchivePrefab, archiveState.ArchiveParent).GetComponent<ArchiveInteractable>();
            newStack.ArchiveIndex = archiveIndex;
            if (archiveState.StackMeshes.Length > archiveIndex - archiveState.DayOffset) {
                newStack.Mesh = archiveState.StackMeshes[archiveIndex - archiveState.DayOffset];
            }

            if (archiveState.StackPoses.Length > archiveIndex - archiveState.DayOffset) {
                newStack.transform.position = archiveState.StackPoses[archiveIndex - archiveState.DayOffset].position;
                newStack.transform.rotation = archiveState.StackPoses[archiveIndex - archiveState.DayOffset].rotation;
            }
        }
    }

}
