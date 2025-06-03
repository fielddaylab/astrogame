using BeauUtil;

namespace FieldDay.Files {
    public struct FileGroup {
        public readonly StringHash32 Id;
        public readonly StringHash32[] Children;
        public readonly string[] Paths;
    }
}