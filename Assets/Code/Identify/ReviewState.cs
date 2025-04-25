using Astro.Reference;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Components;
using FieldDay.Debugging;
using FieldDay.SharedState;
using System;

namespace Astro {
    public sealed class ReviewState : SharedStateComponent {
        public ReviewModule ReviewModule;

        [NonSerialized] public int ActivePips;

        public Timer ReviewTimer;
        public Timer ReviewCooldown;

        [NonSerialized] public bool OverrideReviewModule;

        [NonSerialized] public ReviewSubmissionType CurrentSubmission;
        [NonSerialized] public ReviewSubmissionClassification Identification;
    }

    public struct ReviewSubmissionClassification {
        public StringHash32 AssetId;
        public StringHash32 Classification;
    }

    public enum ReviewSubmissionType {
        None,
        Identification,
        Puzzle
    }

    public enum ReviewResult {
        Success = 0,

        AssetNotInNeutrinoEvent,
        Duplicate,
        ClassificationNotInAccepted,
        ClassificationNotFound
    }

    public static partial class ReviewUtility {
        public static bool ReviewInProgress() {
            ReviewState state = Find.State<ReviewState>();
            return state.CurrentSubmission != ReviewSubmissionType.None;
        }

        public static bool CanSubmitReview() {
            ReviewState state = Find.State<ReviewState>();
            return state.CurrentSubmission == ReviewSubmissionType.None && !state.OverrideReviewModule;
        }

        #region Open Identification Submission

        static public ReviewResult EvaluateSubmission(ReviewSubmissionClassification submission, PlayerProgressState progress) {
            Assert.True(!submission.AssetId.IsEmpty && !submission.Classification.IsEmpty, "Empty submission");

            CelestialAsset asset = Find.NamedAsset<CelestialAsset>(submission.AssetId);
            ReferenceClassification classification = Find.NamedAsset<ReferenceClassification>(submission.Classification);
            DayConfigAsset config = DayConfigUtil.GetConfigForState();

            // check if this is an accepted submission type
            if ((classification.Type & config.AcceptedIDSubmissions) == 0) {
                return ReviewResult.ClassificationNotInAccepted;
            } else if (!ArrayUtils.Contains(config.NeutrinoEvent.RelevantObjectIds, submission.AssetId)) {
                return ReviewResult.AssetNotInNeutrinoEvent;
            }

            progress.Classifications.TryGetValue(submission.AssetId, out BitSet32 completed);
            int classificationIdx = Array.IndexOf(asset.ClassIds, submission.Classification);
            if (classificationIdx < 0) {
                return ReviewResult.ClassificationNotFound;
            }

            if (completed[classificationIdx]) {
                return ReviewResult.Duplicate;
            }

            completed[classificationIdx] = true;
            progress.Classifications[asset.AssetId] = completed;
            return ReviewResult.Success;
        }

        public static bool AssetSubmissionCompleted() {
            ReferenceClassification refClass = Find.State<RefGuideState>().SelectedRefClassification;
            PlayerProgressState progress = Find.State<PlayerProgressState>();
            UIFocus focus = Find.State<FocusState>().CurrentFocus;
            if (refClass == null || focus == null) {
                return false;
            }
            progress.Classifications.TryGetValue(focus.TargetData.AssetId, out BitSet32 completed);
            for (int i = 0; i < focus.TargetData.ClassIds.Length; i++) {
                if (completed[i]) {
                    return true;
                }
            }
            return false;
        }

        #endregion // Open Identification Submission

        #region Points

        public static int GetPoints() {
            PlayerPointsState state = Find.State<PlayerPointsState>();
            return state.SciencePoints;
        }

        public static void SetPoints(int newPoints) {
            InternalSetPoints(Find.State<PlayerPointsState>(), newPoints);
        }

        public static void AddPoints(int delta) {
            PlayerPointsState state = Find.State<PlayerPointsState>();
            InternalSetPoints(state, state.SciencePoints + delta);
        }

        private static void InternalSetPoints(PlayerPointsState state, int newPoints) {
            if (state.SciencePoints != newPoints) {
                state.SciencePoints = newPoints;
                OnPointsUpdated.Invoke(newPoints);
                UpdatePointDisplay(Find.State<ReviewState>().ReviewModule, state);
            }
        }

        [DebugMenuFactory]
        private static DMInfo PointsMenu() {
            DMInfo info = new DMInfo("Points");
            info.AddButton("Add Point", () => {
                AddPoints(1);
            });
            return info;
        }

        static public readonly CastableEvent<int> OnPointsUpdated = new CastableEvent<int>();
        static public readonly CastableEvent<string> OnCorrectPuzzleSubmission = new CastableEvent<string>();

        #endregion // Points
    }
}