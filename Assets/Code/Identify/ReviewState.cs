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
        public SpectrographMaterialMask Materials;
    }

    public enum ReviewSubmissionType {
        None,
        Identification,
        Puzzle
    }

    public enum ReviewResult {
        Success = 0,
        SuccessNotInNeutrinoEvent,
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
            Assert.True(!submission.AssetId.IsEmpty, "Empty submission");

            CelestialAsset asset = Find.NamedAsset<CelestialAsset>(submission.AssetId);
            DayConfigAsset config = DayConfigUtil.GetConfigForState();
            
            if (!submission.Classification.IsEmpty) {
                return EvaluateClassificationSubmission(submission, progress, asset, config);
            } else if (submission.Materials != 0) {
                return EvaluateClassificationMaterialMask(submission, progress, asset, config);
            } else {
                Assert.Fail("Empty submission - didn't have classification or material mask");
                return ReviewResult.ClassificationNotInAccepted;
            }
        }

        static private ReviewResult EvaluateClassificationSubmission(ReviewSubmissionClassification submission, PlayerProgressState progress, CelestialAsset asset, DayConfigAsset config) {
            ReferenceClassification classification = Find.NamedAsset<ReferenceClassification>(submission.Classification);

            PlayerKnowledgeQueryResult query = PlayerKnowledgeUtility.MarkNewClassification(asset, submission.Classification, out PlayerCelestialAssetKnowledge knowledgeRecord);

            // check if this is an accepted submission type
            if (!NeutrinoEventUtil.IsIdInNeutrinoEvent(submission.AssetId)) {
                switch (query) {
                    case PlayerKnowledgeQueryResult.InvalidData:
                        return ReviewResult.AssetNotInNeutrinoEvent;
                    case PlayerKnowledgeQueryResult.Known:
                        return ReviewResult.Duplicate;
                    case PlayerKnowledgeQueryResult.NewKnowledge:
                    default:
                        return ReviewResult.SuccessNotInNeutrinoEvent;
                }
            } else if ((classification.Type & config.AcceptedIDSubmissions) == 0) {
                return ReviewResult.ClassificationNotInAccepted;
            }

            switch (query) {
                case PlayerKnowledgeQueryResult.InvalidData:
                    return ReviewResult.ClassificationNotFound;
                case PlayerKnowledgeQueryResult.Known:
                    return ReviewResult.Duplicate;
                case PlayerKnowledgeQueryResult.NewKnowledge:
                default:
                    return ReviewResult.Success;
            }
        }

        static private ReviewResult EvaluateClassificationMaterialMask(ReviewSubmissionClassification submission, PlayerProgressState progress, CelestialAsset asset, DayConfigAsset config) {
            // check if this is an accepted submission type

            PlayerKnowledgeQueryResult query = PlayerKnowledgeUtility.KnowsFlags(asset.AssetId, PlayerCelestialAssetKnowledgeFlags.IdentifiedResources, out PlayerCelestialAssetKnowledge knowledgeRecord);

            if (!NeutrinoEventUtil.IsIdInNeutrinoEvent(submission.AssetId)) {
                switch (query) {
                    case PlayerKnowledgeQueryResult.InvalidData:
                        return ReviewResult.AssetNotInNeutrinoEvent;
                    case PlayerKnowledgeQueryResult.Known:
                        return ReviewResult.Duplicate;
                    case PlayerKnowledgeQueryResult.NewKnowledge:
                    default:
                        return ReviewResult.SuccessNotInNeutrinoEvent;
                }
            } else if ((ClassificationTypeMask.Spectrometer & config.AcceptedIDSubmissions) == 0) {
                return ReviewResult.ClassificationNotInAccepted;
            }

            if (submission.Materials != asset.Spectrograph) {
                return ReviewResult.ClassificationNotFound;
            }

            switch (query) {
                case PlayerKnowledgeQueryResult.Known:
                    return ReviewResult.Duplicate;
                default:
                    knowledgeRecord.Flags |= PlayerCelestialAssetKnowledgeFlags.IdentifiedResources;
                    PlayerKnowledgeUtility.UpdateKnowledge(asset.AssetId, knowledgeRecord);
                    return ReviewResult.Success;
            }
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
                // UpdatePointDisplay(Find.State<ReviewState>().ReviewModule, state);
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