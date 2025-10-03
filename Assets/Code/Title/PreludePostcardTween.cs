using UnityEngine;
using System.Collections;

using BeauRoutine;
using FieldDay.Scripting;
using Leaf.Runtime;


public class PreludePostcardTween : ScriptActorComponent {
    public Transform postcard;

    private void Awake() {
        postcard = this.transform;
    }

    [LeafMember("SlideIn")]
    private IEnumerator SlideIn(float duration = 1.5f) {
        Vector3 endPos = new Vector3(2, 14, -4);
        yield return postcard.MoveTo(endPos, duration).Ease(Curve.QuadInOut).ForceOnCancel();
    }

    [LeafMember("SlideOut")]
    private IEnumerator SlideOut(float duration = 1f) {
        Vector3 endPos = new Vector3(2, -5, -4);
        yield return postcard.MoveTo(endPos, duration).Ease(Curve.QuadInOut).ForceOnCancel();
    }    
}
