using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace FieldDay.Perf {
    public sealed class DeviceProfile : ScriptableObject {

    }

    public struct DeviceProfileInput {
        public ulong GPUDeviceId;
    }
}