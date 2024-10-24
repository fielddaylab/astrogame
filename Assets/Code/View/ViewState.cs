using System;
using System.Collections;
using System.Collections.Generic;
using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Components;
using FieldDay.SharedState;
using UnityEngine;

namespace Astro {
    public sealed class ViewState : SharedStateComponent, IRegistrationCallbacks {
        public CameraRig Camera;
        public TweenSettings DefaultTransition = new TweenSettings(0.3f, Curve.Smooth);
        public ViewNode DefaultNode;

        public Dictionary<StringHash32, ViewNode> NamedNodes = MapUtils.Create<StringHash32, ViewNode>(64);
        public RingBuffer<ViewLink> AllLinks = new RingBuffer<ViewLink>(256);

        [NonSerialized] public ViewNode ActiveNode;
        [NonSerialized] public HashSet<StringHash32> ActiveNodeLinkGroups = SetUtils.Create<StringHash32>(16);
        [NonSerialized] public HashSet<StringHash32> ActiveScriptLinkGroups = SetUtils.Create<StringHash32>(16);
        [NonSerialized] public RingBuffer<ViewLink> ActiveLinks = new RingBuffer<ViewLink>(32);

        public Routine ActiveTransitionRoutine;

        void IRegistrationCallbacks.OnDeregister() {
        }

        void IRegistrationCallbacks.OnRegister() {
            Game.Scenes.QueueOnLoad(this, () => {
                ViewNavUtility.SnapToNode(this, DefaultNode);
            });
        }
    }

    static public partial class ViewNavUtility {
        /// <summary>
        /// Deactivates all currently active links.
        /// </summary>
        static public void DeactivateAllLinks(ViewState state) {
            while(state.ActiveLinks.TryPopBack(out ViewLink link)) {
                link.LastKnownActiveState = false;
                link.ObjectGroup.SetActive(false);
            }
            state.ActiveNodeLinkGroups.Clear();
        }

        /// <summary>
        /// Updates active links.
        /// </summary>
        static public void UpdateActiveLinks(ViewState state) {
            DeactivateAllLinks(state);

            if (state.ActiveNode) {
                foreach (var groupId in state.ActiveNode.LinkGroups) {
                    state.ActiveNodeLinkGroups.Add(groupId);
                }
            }

            foreach(var link in state.AllLinks) {
                RefreshLinkState(link, state);
            }
        }

        /// <summary>
        /// Moves the view to a new node.
        /// </summary>
        static public void MoveToNode(ViewState state, ViewNode node) {
            if (state.ActiveNode == node) {
                return;
            }

            state.ActiveTransitionRoutine.Replace(TransitionRoutine(state, node, null));
        }

        /// <summary>
        /// Snaps directly to a node.
        /// </summary>
        static public void SnapToNode(ViewState state, ViewNode node) {
            if (state.ActiveNode == node) {
                return;
            }

            state.ActiveTransitionRoutine.Stop();
            InstantTransition(state, node);
        }

        /// <summary>
        /// Moves the view over a link.
        /// </summary>
        static public void MoveByLink(ViewState state, ViewLink link) {
            state.ActiveTransitionRoutine.Replace(TransitionRoutine(state, link.TargetNode, link));
        }

        static private IEnumerator TransitionRoutine(ViewState state, ViewNode nextNode, ViewLink byLink) {
            Transform controlPoint = null;
            TweenSettings tween = state.DefaultTransition;
            if (byLink) {
                controlPoint = byLink.TransitionControlPoint;
                if (byLink.Transition.Time > 0) {
                    tween = byLink.Transition;
                }
            }

            IEnumerator cameraTransition = null;
            if (controlPoint) {
                cameraTransition = CameraRigUtility.MoveToPoseWithControlPoint(state.Camera, nextNode.Camera, controlPoint.position, tween.Time, tween.Curve);
            } else {
                cameraTransition = CameraRigUtility.MoveToPose(state.Camera, nextNode.Camera, tween.Time, tween.Curve);
            }

            DeactivateAllLinks(state);

            ViewNode oldNode = state.ActiveNode;
            state.ActiveNode = nextNode;

            if (oldNode) {
                oldNode.OnExit.Invoke(oldNode);
            }
            nextNode.OnLoad.Invoke(nextNode);

            yield return cameraTransition;

            if (oldNode) {
                DeactivateNode(oldNode, false);
            }

            ActivateNode(nextNode, true);
            UpdateActiveLinks(state);
        }

        static private void InstantTransition(ViewState state, ViewNode nextNode) {
            DeactivateAllLinks(state);

            ViewNode oldNode = state.ActiveNode;
            state.ActiveNode = nextNode;

            CameraRigUtility.MoveToPose(state.Camera, nextNode.Camera, 0);

            if (oldNode) {
                oldNode.OnExit.Invoke(oldNode);
            }
            nextNode.OnLoad.Invoke(nextNode);

            if (oldNode) {
                DeactivateNode(oldNode, false);
            }
            ActivateNode(nextNode, true);
            UpdateActiveLinks(state);
        }
    }
}