using FieldDay;
using UnityEngine;

namespace Astro {
    [RequireComponent(typeof (LabInstrument))]
    public class Photometer : MonoBehaviour, IRegistrationCallbacks {
        //public DataDisplay SharedDisplay;
        public DataSlot ApparentSlot;
        public DataSlot AbsoluteSlot;
        public GameObject AbsoluteIndicator;

        public void OnDeregister() {
        }

        public void OnRegister() {
            PhotometerUtility.TogglePhotometerMode(false, this);
        }
    }

    public static partial class PhotometerUtility {
        public static void TogglePhotometerMode(bool absoluteOn, Photometer photometer){
            photometer.AbsoluteIndicator.SetActive(absoluteOn);
            photometer.ApparentSlot.gameObject.SetActive(!absoluteOn);
            photometer.AbsoluteSlot.gameObject.SetActive(absoluteOn);
        }
    }
}