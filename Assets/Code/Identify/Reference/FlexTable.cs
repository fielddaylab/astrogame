using FieldDay;
using UnityEngine;
using UnityEngine.UI;

namespace Astro {
    [RequireComponent(typeof(GridLayoutGroup))]
    public class FlexTable : MonoBehaviour, IRegistrationCallbacks {
        public GridLayoutGroup Grid;
        public void OnDeregister() {
        }

        public void OnRegister() {
            Grid = GetComponent<GridLayoutGroup>();
        }
    }
}