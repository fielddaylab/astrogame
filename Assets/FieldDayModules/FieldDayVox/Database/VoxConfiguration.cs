using BeauUtil.Debugger;
using BeauUtil.Streaming;
using FieldDay.Assets;
using UnityEngine;

namespace FieldDay.Vox {
    [CreateAssetMenu(menuName = "Field Day/Voiceover/Vox Configuration")]
    public sealed class VoxConfiguration : GlobalAsset {
        public string StreamingPathRoot = "vox";
        public string FileExtension = ".mp3";
        public TextAsset MappingFile;

        public override void Mount() {
            VoxUtility.ConfigureStreamingPaths(StreamingPathRoot, FileExtension);
            if (MappingFile != null) {
                VoxUtility.ReadHumanReadableMappingFile(MappingFile);
                AssetUtility.DestroyAsset(MappingFile);
#if !UNITY_EDITOR
                MappingFile = null;
#endif // !UNITY_EDITOR
            }
        }

        public override void Unmount() {
            Assert.True(Game.IsShuttingDown);
        }
    }
}