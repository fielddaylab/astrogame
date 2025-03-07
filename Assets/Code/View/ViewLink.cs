using System;
using System.Collections.Generic;
using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Components;
using FieldDay.Scenes;
using ScriptableBake;
using UnityEngine;

namespace Astro {
    /// <summary>
    /// Link to a ViewNode.
    /// </summary>
    [PreloadOrder(100)]
    public sealed class ViewLink : BatchedComponent, IRegistrationCallbacks, IBaked, IScenePreload {
        public ViewNode TargetNode;
        public SerializedHash32 TargetNodeId;

        [Header("Transition")]
        public TweenSettings Transition = new TweenSettings(0.3f, Curve.Smooth);
        public Transform TransitionControlPoint;

        [Header("Grouping")]
        public Collider Clickable;
        public ActiveGroup ObjectGroup;
        public SerializedHash32 GroupId;

        [NonSerialized] public bool LastKnownActiveState;

        void IRegistrationCallbacks.OnDeregister() {
            if (Game.IsShuttingDown) {
                return;
            }

            //TODO Deregister earlier
            // var mgr = Find.State<ViewState>();
            // mgr.AllLinks.FastRemove(this);
            // if (LastKnownActiveState) {
            //     LastKnownActiveState = false;
            //     mgr.ActiveLinks.FastRemove(this);
            // }
        }

        void IRegistrationCallbacks.OnRegister() {
            var mgr = Find.State<ViewState>();
            mgr.AllLinks.PushBack(this);
            ViewNavUtility.RefreshLinkState(this, mgr);
        }

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            if (!TargetNode) {
                TargetNode = ViewNavUtility.GetNodeById(TargetNodeId);
            }
            return null;
        }

#if UNITY_EDITOR
        int IBaked.Order { get { return 10000; } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            ObjectGroup.SetActive(false);
            if (Clickable) {
                Clickable.enabled = false;
            }
            return false;
        }

        private void Reset() {
            if (!Clickable) {
                Clickable = GetComponent<Collider>();
            }
        }
#endif // UNITY_EDITOR
    }

    static public partial class ViewNavUtility {
        /// <summary>
        /// Returns if the given link should be active.
        /// </summary>
        static public bool LinkShouldBeActive(ViewLink link, ViewState state) {
            if (link.GroupId.IsEmpty) {
                return true;
            }

            if (state.ActiveNodeLinkGroups.Contains(link.GroupId) || state.ActiveScriptLinkGroups.Contains(link.GroupId)) {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Refreshes the active state of the given link.
        /// </summary>
        static public void RefreshLinkState(ViewLink link, ViewState state) {
            bool shouldBeActive = LinkShouldBeActive(link, state);
            link.ObjectGroup.SetActive(shouldBeActive);
            if (link.Clickable) {
                link.Clickable.enabled = shouldBeActive;
            }
            if (Ref.Replace(ref link.LastKnownActiveState, shouldBeActive)) {
                if (shouldBeActive) {
                    state.ActiveLinks.PushBack(link);
                } else {
                    state.ActiveLinks.FastRemove(link);
                }
            }
        }
    }
}