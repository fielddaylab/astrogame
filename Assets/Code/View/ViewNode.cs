using System;
using BeauUtil;
using FieldDay;
using FieldDay.Components;
using FieldDay.Scripting;
using UnityEngine;

namespace Astro {
    public sealed class ViewNode : BatchedComponent, IRegistrationCallbacks {
        public SerializedHash32 Id;

        [Header("Camera")]
        public CameraPose Camera;

        [Header("Objects To Activate")]
        public ActiveGroup Group;

        [Header("Links")]
        public SerializedHash32[] LinkGroups;

        public CastableEvent<ViewNode> OnLoad = new CastableEvent<ViewNode>();
        public CastableEvent<ViewNode> OnEnter = new CastableEvent<ViewNode>();
        public CastableEvent<ViewNode> OnExit = new CastableEvent<ViewNode>();

        void IRegistrationCallbacks.OnDeregister() {
            if (Game.IsShuttingDown) {
                return;
            }

            ViewState state = Find.State<ViewState>();
            state.NamedNodes.Remove(Id);
        }

        void IRegistrationCallbacks.OnRegister() {
            ViewState state = Find.State<ViewState>();
            state.NamedNodes.Add(Id, this);
        }
    }

    static public partial class ViewNavUtility {
        #region Node

        static public void ActivateNode(ViewNode node, bool invokeCallbacks) {
            node.Group.SetActive(true, false);
            if (invokeCallbacks) {
                node.OnEnter.Invoke(node);
            }
        }

        static public void DeactivateNode(ViewNode node, bool invokeCallbacks) {
            if (invokeCallbacks) {
                node.OnExit.Invoke(node);
            }
            node.Group.SetActive(false, false);
        }

        /// <summary>
        /// Finds the ViewNode with the given id.
        /// </summary>
        static public ViewNode GetNodeById(StringHash32 nodeId) {
            ViewState state = Find.State<ViewState>();
            state.NamedNodes.TryGetValue(nodeId, out ViewNode node);
            return node;
        }

        #endregion // Node
    }
}