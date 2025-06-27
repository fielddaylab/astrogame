using System;
using System.Collections.Generic;
using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Processes;
using FieldDay.Scenes;
using FieldDay.Scripting;
using Leaf.Runtime;
using ScriptableBake;
using UnityEngine;
using UnityEngine.Playables;

namespace Astro {
    public sealed class CutscenePlayer : ScriptActorComponent, IBaked, IScenePreload {
        [Required] public PlayableDirector Director;
        [Required] public CutsceneCamera Camera;
        public ActiveGroup AssetGroup;

        [Header("Transition Back To Gameplay")]
        public SerializedHash32 EndNode;
        public TweenSettings EndNodeTransitionOverride;

        [NonSerialized] private bool m_PrepareLock;

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            Director.played += (p) => OnCutsceneBegin();
            Director.stopped += (p) => OnCutsceneEnd();
            return null;
        }

        [LeafMember("PrepareCutscene")]
        public void PrepareCutscene() {
            m_PrepareLock = true;

            Director.gameObject.SetActive(true);
            Director.time = 0;
            Director.Evaluate();

            ViewState state = Find.State<ViewState>();
            state.DefaultNode = null;
            ViewNavUtility.ClearCurrentNode(state);
            CutsceneUtility.SyncCamera(Camera, state);

            Director.gameObject.SetActive(false);
        }

        [LeafMember("BeginCutscene")]
        public void BeginCutscene() {
            m_PrepareLock = false;
            Director.gameObject.SetActive(true);
            Director.Play();
        }

        [LeafMember("ExitCutscene")]
        public void ExitCutscene() {
            Director.Stop();          
        }

        private void OnCutsceneBegin() {
            if (m_PrepareLock) {
                return;
            }

            Camera.gameObject.SetActive(true);
            AssetGroup.SetActive(true);

            ViewState state = Find.State<ViewState>();
            ViewNavUtility.ClearCurrentNode(state);
            CutsceneUtility.SyncCamera(Camera, state);

            using (var table = TempVarTable.Alloc()) {
                table.Set("cutsceneId", this.Actor.Id);
                ScriptUtility.Trigger(ScriptEvents.CutsceneBegin, table);
            }
        }

        private void OnCutsceneEnd() {
            if (m_PrepareLock) {
                return;
            }

            if (!Director || !Camera || Game.IsShuttingDown || !this.Actor || GameLoop.IsLoading) {
                return;
            }

            Director.gameObject.SetActive(false);
            AssetGroup.SetActive(false);
            Camera.gameObject.SetActive(false);

            using (var table = TempVarTable.Alloc()) {
                table.Set("cutsceneId", this.Actor.Id);
                ScriptUtility.Trigger(ScriptEvents.CutsceneEnd, table);
            }
            
            if (!EndNode.IsEmpty) {
                ViewNavUtility.MoveToNode(Find.State<ViewState>(), ViewNavUtility.GetNodeById(EndNode), EndNodeTransitionOverride);
            }
        }

#if UNITY_EDITOR
        int IBaked.Order { get { return 1000; } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            Director.playOnAwake = false;
            Director.Stop();
            gameObject.SetActive(false);
            AssetGroup.SetActive(false);
            Camera.gameObject.SetActive(false);
            return true;
        }

        private void Reset() {
            Director = GetComponent<PlayableDirector>();
        }

#endif // UNITY_EDITOR
    }
}