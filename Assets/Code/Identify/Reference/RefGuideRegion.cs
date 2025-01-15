using FieldDay.Components;
using UnityEngine;

namespace Astro {
    public class RefGuideRegion : BatchedComponent {
        public ReferenceClassification ConnectedEntry;
        public RefGuidePageChange PageChange;
    }

    public enum RefGuidePageChange {
        None,
        Previous,
        Next
    }
}