using System.Collections;
using System.Collections.Generic;
using System.IO;
using BeauUtil;
using UnityEditor;

namespace FieldDay.Editor {
    static public class FilePathUtility {
        static public IEnumerable<StringSlice> GetParentDirectoryNames(string fullPath) {
            int end = fullPath.Length;
            int start = end - 2;
            while(start >= 0) {
                if (fullPath[start] == '/' || fullPath[start] == '\\') {
                    yield return new StringSlice(fullPath, start + 1, end - start - 1);
                    end = start;
                }
                start--;
            }
        }
    }
}