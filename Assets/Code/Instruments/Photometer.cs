using FieldDay;
using UnityEngine;

namespace Astro {
    [RequireComponent(typeof (LabInstrument))]
    public class Photometer : MonoBehaviour, IRegistrationCallbacks {
        public DataDisplay SharedDisplay;
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
            if (absoluteOn) {
                photometer.AbsoluteSlot.Displays[0] = photometer.SharedDisplay;
                photometer.ApparentSlot.Displays[0] = null;
                DataUtility.PopulateDisplay(photometer.SharedDisplay, photometer.AbsoluteSlot.CurrentData);
            } else {
                photometer.AbsoluteSlot.Displays[0] = null;
                photometer.ApparentSlot.Displays[0] = photometer.SharedDisplay;
                DataUtility.PopulateDisplay(photometer.SharedDisplay, photometer.ApparentSlot.CurrentData);
            }
        }
    }
}