using BeauUtil;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;

namespace FieldDay.Animation.StateMachine {
    [SharedBetweenAnimators]
    public class SetParametersOnExit : StateMachineBehaviour {
        public AnimatorParamChange[] Changes;

        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
            for(int i = 0, len = Changes.Length; i < len; i++) {
                Changes[i].Apply(animator);
            }
        }
    }
}