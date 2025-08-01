using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using System;
using System.Collections;
using UnityEngine;

namespace Astro
{
    public class ArchiveState : SharedStateComponent, ISharedState
    {
        public GameObject ArchivePrefab;
        public Mesh[] StackMeshes;
        public Transform[] StackPoses;
        public Transform ArchiveParent;

        [NonSerialized] public int DayOffset = 1; // number of days without stacks (e.g. prelude)
        [NonSerialized] public int CurrArchiveIndex = -1;

        public Routine LoadRoutine;
    }

    public static class ArchiveUtility { 
        public static void LoadArchive(ArchiveState archiveState, DocumentBoardState boardState, int dayIndex)
        {
            throw new NotImplementedException();

            //if (dayIndex == archiveState.CurrArchiveIndex) { return; }
            //if (archiveState.LoadRoutine.Exists()) { return; }

            //HideCurrentArchive(archiveState, boardState);

            //PlayerProgressState playerState = Find.State<PlayerProgressState>();
            //StoryAsset story = Find.GlobalAsset<StoryAsset>();

            //var archiveIndex = dayIndex; // - archiveState.DayOffset;
            //DayConfigAsset day = Find.NamedAsset<DayConfigAsset>(story.Days[archiveIndex]);
            //var currLayout = day.DocLayout[0];

            //archiveState.LoadRoutine.Replace(LoadArchiveRoutine(archiveState, boardState, dayIndex, currLayout, archiveIndex));
        }

        public static IEnumerator LoadArchiveRoutine(ArchiveState archiveState, DocumentBoardState boardState, int dayIndex, ArchiveLayout currLayout, int archiveIndex)
        {
            Transform[] transforms = new Transform[currLayout.Documents.Count];

            while (boardState.DocumentLoadRoutine.Exists()) { yield return null; }

            int docIndex = 0;
            foreach (var doc in currLayout.Documents)
            {
                // spawn the asset at the position
                var spawned = DocumentUtility.SpawnDocument(doc, doc.AssetId, out Vector3 pinnedPos, boardState, false, true, archiveIndex);
                transforms[docIndex] = spawned.transform;
                spawned.transform.position = new Vector3(-500, -500, 500); // place somewhere offscreen while loading
                docIndex++;

                while (boardState.DocumentLoadRoutine.Exists()) { yield return null; }
            }

            PlayerProgressState progressState = Find.State<PlayerProgressState>();

            docIndex = 0;
            foreach (var doc in currLayout.Documents)
            {

                var localPos = doc.DefaultPinnedPos;
                bool fromArchive = archiveIndex != -1;
                bool fromCurrDayArchive = progressState.DayIndex == archiveIndex; // + archiveState.DayOffset;

                // override with init position if doc has different init position and it's being spawned to the current day
                if (doc.DifInitPos && (!fromArchive || (fromArchive && fromCurrDayArchive))) {
                    localPos = doc.InitPos;
                }

                transforms[docIndex].localPosition = localPos;
                docIndex++;
            }

            archiveState.CurrArchiveIndex = dayIndex;
        }

        public static void HideCurrentArchive(ArchiveState archiveState, DocumentBoardState boardState)
        {
            if (archiveState.CurrArchiveIndex == -1) return;

            // save current layout
            // SaveCurrentLayout();

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
