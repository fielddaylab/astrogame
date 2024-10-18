using System;
using System.Runtime.InteropServices;
using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace FieldDay.Audio {
    #region Enums

    [Flags]
    internal enum AudioPlaybackFlags : ushort {
        Loop = 0x01,
        UseProvidedSource = 0x02,
        RandomizePlaybackStart = 0x04,
        SecondaryClipOverride = 0x08
    }

    /// <summary>
    /// Audio command type.
    /// </summary>
    internal enum AudioCommandType : ushort {
        PlayClipFromName,
        PlayClipFromAssetRef,
        PlayFromHandle,
        StopWithHandle,
        StopWithAudioSource,
        StopWithTag,
        StopAll,
        SetVoiceFloatParameter,
        SetVoiceBoolParameter,
        SetBusFloatParameter,
        SetBusBoolParameter
    }

    #endregion // Enums

    #region Unions

    /// <summary>
    /// Reference to a playing instance, or a group.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal struct AudioIdRef {
        [FieldOffset(0)] public StringHash32 Id;
        [FieldOffset(0)] public UniqueId16 Handle;
        [FieldOffset(0)] public int InstanceId;

        static public implicit operator AudioIdRef(StringHash32 id) {
            return new AudioIdRef() { Id = id };
        }

        static public implicit operator AudioIdRef(UniqueId16 handle) {
            return new AudioIdRef() { Handle = handle };
        }

        static public implicit operator AudioIdRef(AudioSource source) {
            return new AudioIdRef() { InstanceId = UnityHelper.Id(source) };
        }
    }

    /// <summary>
    /// Reference to an asset by name, or explicitly as an instance.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal struct AudioAssetRef {
        [FieldOffset(0)] public StringHash32 AssetId;
        [FieldOffset(0)] public int InstanceId;

        static public implicit operator AudioAssetRef(StringHash32 id) {
            return new AudioAssetRef() { AssetId = id };
        }

        static public implicit operator AudioAssetRef(UnityEngine.Object obj) {
            return new AudioAssetRef() { InstanceId = UnityHelper.Id(obj) };
        }
    }

    #endregion // Unions

    #region Command Data

    /// <summary>
    /// Data for PlayClipFromName and PlayClipFromAssetRef
    /// </summary>
    internal struct PlayCommandData {
        public AudioAssetRef Asset;
        public AudioAssetRef SecondaryAsset;
        public float Volume;
        public float Pitch;
        public StringHash32 Tag;
        public AudioPlaybackFlags Flags;
        public UniqueId16 Handle;

        public int TransformOrAudioSourceId;
        public Vector3 TransformOffset;
        public Quaternion RotationOffset;
        public Space TransformOffsetSpace;

        private int m_Padding;
    }

    /// <summary>
    /// Data for PlayClipFromHandle
    /// </summary>
    internal struct PlayExistingCommandData {
        public UniqueId16 Handle;
    }

    /// <summary>
    /// Data for StopWithHandle and StopWithTag
    /// </summary>
    internal struct StopCommandData {
        public AudioIdRef Id;
        public float FadeOut;
        public Curve FadeOutCurve;
    }

    /// <summary>
    /// Data for SetFloatParameter
    /// </summary>
    internal struct FloatParamChangeCommandData {
        public UniqueId16 Handle;
        public AudioFloatPropertyType Property;
        public float Target;
        public float Duration;
        public Curve Easing;
    }

    /// <summary>
    /// Data for SetBoolParameter
    /// </summary>
    internal struct BoolParamChangeCommandData {
        public UniqueId16 Handle;
        public AudioBoolPropertyType Property;
        public bool Target;
    }

    #endregion // Command Data

    [StructLayout(LayoutKind.Explicit)]
    internal struct AudioCommand {
        [FieldOffset(0)] public AudioCommandType Type;
        [FieldOffset(4)] public PlayCommandData Play;
        [FieldOffset(4)] public PlayExistingCommandData Resume;
        [FieldOffset(4)] public StopCommandData Stop;
        [FieldOffset(4)] public FloatParamChangeCommandData FloatParam;
        [FieldOffset(4)] public BoolParamChangeCommandData BoolParam;
    }
}