using BeauUtil;
using FieldDay.Components;
using FieldDay.SharedState;

namespace Astro {
    public sealed class ScienceMailboxState : SharedStateComponent {
        public ScienceSubmissionClassification Identification;
    }

    public struct ScienceSubmissionClassification {
        public StringHash32 AssetId;
        public StringHash32 Classification;
        public float Progress;
        public float Duration;
    }
}