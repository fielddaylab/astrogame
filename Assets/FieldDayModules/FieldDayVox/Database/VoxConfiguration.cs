using FieldDay.Assets;
using UnityEngine;

namespace FieldDay.Vox {
    [CreateAssetMenu(menuName = "Field Day/Voiceover/Vox Configuration")]
    public sealed class VoxConfiguration : GlobalAsset {
        public string StreamingPathRoot = "vox";
        public string FileExtension = ".mp3";

        public override void Mount() {
            VoxUtility.ConfigureStreamingPaths(StreamingPathRoot, FileExtension);
        }

        public override void Unmount() {
        }
    }
}