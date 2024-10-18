using System.Collections;
using System.Collections.Generic;
using BeauRoutine;
using BeauUtil;
using FieldDay.Scenes;
using FieldDay.SharedState;
using UnityEngine;

namespace FieldDay.Vox {
    public class SubtitleDatabase : ISharedState, IRegistrationCallbacks, ISceneLoadDependency {
        internal Dictionary<StringHash32, SubtitleEntry> TextMap = new Dictionary<StringHash32, SubtitleEntry>();
        internal AsyncHandle FileLoader;

        #region IRegistrationCallbacks

        void IRegistrationCallbacks.OnDeregister() {
            Game.Scenes?.DeregisterLoadDependency(this);
        }

        void IRegistrationCallbacks.OnRegister() {
            Game.Scenes.RegisterLoadDependency(this);
        }

        #endregion // IRegistrationCallbacks

        #region ISceneLoadDependency

        bool ISceneLoadDependency.IsLoaded(SceneLoadPhase loadPhase) {
            if (loadPhase == SceneLoadPhase.BeforeReady) {
                return !FileLoader.IsRunning();
            } else {
                return true;
            }
        }

        #endregion // ISceneLoadDependency
    }

    public struct SubtitleEntry {
        public string Data;
        public SubtitleTimeCodePair[] TimeCodes;
        public bool IsFallback;

        public SubtitleEntry(string data) {
            Data = data;
            TimeCodes = null;
            IsFallback = true;
        }

        public SubtitleEntry(string data, SubtitleTimeCodePair[] timing) {
            Data = data;
            TimeCodes = timing;
            IsFallback = false;
        }

        public SubtitleEntry(string data, SubtitleTimeCodePair[] timing, bool isFallback) {
            Data = data;
            TimeCodes = timing;
            IsFallback = isFallback;
        }
    }

    public struct SubtitleTimeCodePair {
        public float Offset;
        public float Duration;
        public OffsetLengthU16 Range;
    }

    static public partial class SubtitleUtility {
        [SharedStateReference] static public SubtitleDatabase DB { get; private set; }

        [InvokePreBoot]
        static private void Initialize() {
            Game.SharedState.Register(new SubtitleDatabase());
        }

        /// <summary>
        /// Finds the subtitle for the given line.
        /// </summary>
        static public SubtitleEntry FindSubtitle(StringHash32 lineCode, string fallback) {
            SubtitleEntry entry;
            if (!DB.TextMap.TryGetValue(lineCode, out entry)) {
                entry.Data = fallback;
                entry.TimeCodes = null;
            }
            return entry;
        }

        /// <summary>
        /// Finds the subtitle for the given line.
        /// </summary>
        static public SubtitleEntry FindSubtitle(StringHash32 lineCode, SubtitleEntry fallback) {
            SubtitleEntry entry;
            if (!DB.TextMap.TryGetValue(lineCode, out entry)) {
                entry = fallback;
            }
            return entry;
        }

        #region Parser

        // TODO: Implement subtitle file parsing
        // probably CSV file
        // possibly split it into multiple pages/chunks
        // so we don't have to have all subtitles loaded all the time

        #endregion // Parser
    }
}