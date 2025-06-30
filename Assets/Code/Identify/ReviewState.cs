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
        CorrectNotInNeutrinoEvent,
        IncorrectNotInNeutrinoEvent,
        Duplicate,
        CorrectNotAccepted,
        Incorrect
    }

    [Flags]
    public enum ClassificationReviewFlags {
        Correct = 0x01,
        InNeutrinoEvent = 0x02,
        Duplicate = 0x04,
        AcceptedType = 0x08
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
                return ReviewResult.CorrectNotAccepted;
            }
        }

        // Because we have multiple independent "scorings" for a classification (correct or not, in neutrino event or not, duplicate or not, accepted or not), it might be worth streamlining into flags.
        // Commenting out for now because it would mess with a lot of stuff!
        /*
        static private ClassificationReviewFlags EvaluateClassificationFlags(ReviewSubmissionClassification submission, PlayerProgressState progress, CelestialAsset asset, DayConfigAsset config) {
            ReferenceClassification classification = Find.NamedAsset<ReferenceClassification>(submission.Classification);
            PlayerKnowledgeQueryResult query = PlayerKnowledgeUtility.MarkNewClassification(asset, submission.Classification, out PlayerCelestialAssetKnowledge knowledgeRecord);

            ClassificationReviewFlags result = 0;
            if (NeutrinoEventUtil.IsIdInNeutrinoEvent(submission.AssetId)) {
                result |= ClassificationReviewFlags.InNeutrinoEvent;
            }
            if ((classification.Type & config.AcceptedIDSubmissions) != 0) {
                result |= ClassificationReviewFlags.AcceptedType;
            }
            switch (query) {
                case PlayerKnowledgeQueryResult.Known:
                    result |= ClassificationReviewFlags.Duplicate;
                    result |= ClassificationReviewFlags.Correct;
                    break;
                case PlayerKnowledgeQueryResult.NewKnowledge:
                    result |= ClassificationReviewFlags.Correct;
                    break;
                case PlayerKnowledgeQueryResult.InvalidData: // neither duplicate nor correct
                default:
                    break;
            }
            return result;
        }
        */

        static private ReviewResult EvaluateClassificationSubmission(ReviewSubmissionClassification submission, PlayerProgressState progress, CelestialAsset asset, DayConfigAsset config) {
            ReferenceClassification classification = Find.NamedAsset<ReferenceClassification>(submission.Classification);

            PlayerKnowledgeQueryResult query = PlayerKnowledgeUtility.MarkNewClassification(asset, submission.Classification, out PlayerCelestialAssetKnowledge knowledgeRecord);

            // check if this is an accepted submission type
            if (!NeutrinoEventUtil.IsIdInNeutrinoEvent(submission.AssetId)) {
                switch (query) {
                    case PlayerKnowledgeQueryResult.InvalidData:
                        return ReviewResult.IncorrectNotInNeutrinoEvent;
                    case PlayerKnowledgeQueryResult.Known:
                        return ReviewResult.Duplicate;
                    case PlayerKnowledgeQueryResult.NewKnowledge:
                    default:
                        return ReviewResult.CorrectNotInNeutrinoEvent;
                }
            } else if ((classification.Type & config.AcceptedIDSubmissions) == 0) {
                // in neutrino event, but not accepted type
                switch (query) {
                    case PlayerKnowledgeQueryResult.InvalidData:
                        return ReviewResult.Incorrect;
                    case PlayerKnowledgeQueryResult.Known:
                        return ReviewResult.Duplicate;
                    case PlayerKnowledgeQueryResult.NewKnowledge:
                    default:
                        return ReviewResult.CorrectNotAccepted;
                }
            }

            switch (query) {
                case PlayerKnowledgeQueryResult.InvalidData:
                    return ReviewResult.Incorrect;
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
                        return ReviewResult.IncorrectNotInNeutrinoEvent;
                    case PlayerKnowledgeQueryResult.Known:
                        return ReviewResult.Duplicate;
                    case PlayerKnowledgeQueryResult.NewKnowledge:
                    default:
                        return ReviewResult.CorrectNotInNeutrinoEvent;
                }
            } else if ((ClassificationTypeMask.Spectrometer & config.AcceptedIDSubmissions) == 0) {
                return ReviewResult.CorrectNotAccepted;
            }

            if (submission.Materials != asset.Spectrograph) {
                return ReviewResult.Incorrect;
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