using System;
using System.Collections.Generic;
using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Components;
using FieldDay.SharedState;

namespace Astro {
    public sealed class ViewState : SharedStateComponent {
        public TweenSettings DefaultTransition = new TweenSettings(0.3f, Curve.Smooth);

        public Dictionary<StringHash32, ViewNode> NamedNodes = MapUtils.Create<StringHash32, ViewNode>(64);
        public RingBuffer<ViewLink> AllLinks = new RingBuffer<ViewLink>(256);

        [NonSerialized] public ViewNode ActiveNode;
        [NonSerialized] public HashSet<StringHash32> ActiveNodeLinkGroups = SetUtils.Create<StringHash32>(16);
        [NonSerialized] public HashSet<StringHash32> ActiveScriptLinkGroups = SetUtils.Create<StringHash32>(16);
        [NonSerialized] public RingBuffer<ViewLink> ActiveLinks = new RingBuffer<ViewLink>(32);
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
        }
    }
}