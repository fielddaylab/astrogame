using BeauUtil;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;

namespace FieldDay.Animation.StateMachine {
    [SharedBetweenAnimators]
    public class SetParametersOnEnter : StateMachineBehaviour {
        public AnimatorParamChange[] Changes;

        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            for(int i = 0, len = Changes.Length; i < len; i++) {
                Changes[i].Apply(animator);
            }
        }
    }
}