using UnityEngine;

namespace Astro {
    public class CameraDrift : MonoBehaviour {
        public float Scale = 1;
        public float DriftX;
        public float DriftY;
        public float PeriodX;
        public float PeriodY;
        public float OffsetX;
        public float OffsetY;

        private void Update() {
            transform.localPosition = new Vector3(
                Mathf.Cos(OffsetX + (Time.time * Mathf.PI * 2 / PeriodX)) * DriftX * Scale,
                Mathf.Sin(OffsetY + (Time.time * Mathf.PI * 2 / PeriodY)) * DriftY * Scale,
                0);
        }

        private void OnDisable() {
            transform.localPosition = default;
        }
    }
}