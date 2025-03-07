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

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            Director.played += (p) => OnCutsceneBegin();
            Director.stopped += (p) => OnCutsceneEnd();
            return null;
        }

        [LeafMember("BeginCutscene")]
        public void BeginCutscene() {
            Director.gameObject.SetActive(true);
            Director.Play();
        }

        [LeafMember("ExitCutscene")]
        public void ExitCutscene() {
            Director.Stop();
        }

        private void OnCutsceneBegin() {
            Camera.gameObject.SetActive(true);
            AssetGroup.SetActive(true);
        }

        private void OnCutsceneEnd() {
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