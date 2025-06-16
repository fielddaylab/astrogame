#if DEVELOPMENT
#define ALLOW_VOX_FALLBACK
#endif // DEVELOPMENT

using BeauUtil.Debugger;
using BeauUtil.Streaming;
using FieldDay.Assets;
using ScriptableBake;
using UnityEngine;

namespace FieldDay.Vox {
    [CreateAssetMenu(menuName = "Field Day/Voiceover/Vox Configuration")]
    public sealed class VoxConfiguration : GlobalAsset, IBaked {
        [Header("Paths")]
        public string StreamingPathRoot = "vox";
        public string FileExtension = ".mp3";

        [Header("Mapping")]
        public TextAsset MappingFile;

        [Header("Fallbacks")]
        public AudioClip FallbackVox;

        public override void Mount() {
            VoxUtility.ConfigureStreamingPaths(StreamingPathRoot, FileExtension);
            VoxUtility.ConfigureFallbackClip(FallbackVox);
            if (MappingFile != null) {
                VoxUtility.ReadHumanReadableMappingFile(MappingFile);
                AssetUtility.DestroyAsset(ref MappingFile);
            }
        }

        public override void Unmount() {
            Assert.True(Game.IsShuttingDown);
        }

#if UNITY_EDITOR

        int IBaked.Order { get { return 0; } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
#if !ALLOW_VOX_FALLBACK
            FallbackVox = null;
            return true;
#else
            return false;
#endif // ALLOW_VOX_FALLBACK
        }

#endif // UNITY_EDITOR
    }
}