using System.IO;
using BeauUtil;
using FieldDay.Data;
using UnityEditor;
using UnityEngine;

namespace FieldDay.Editor.Tests {
    static public class CompressionTest {
        [MenuItem("Field Day/Testing/Compression Test")]
        static private void Test() {
            string filePath = EditorUtility.OpenFilePanel("Open File To Compress", "Assets", string.Empty);
            if (string.IsNullOrEmpty(filePath)) {
                return;
            }

            byte[] fileBytes = File.ReadAllBytes(filePath);
            var compressionResult = LZCompression.Compress(fileBytes, out byte[] compressedBytes);

            if (compressionResult != LZCompressionResult.Success) {
                EditorUtility.DisplayDialog("Unable to compress", "Reason: " + compressionResult, "okay");
                return;
            }

            File.WriteAllBytes(filePath + ".compressed", compressedBytes);

            var decompressionResult = LZCompression.Decompress(compressedBytes, out byte[] decompressedBytes);
            if (decompressionResult != LZDecompressionResult.Success) {
                EditorUtility.DisplayDialog("Unable to decompress", "Reason: " + decompressionResult, "okay");
                return;
            }

            if (!ArrayUtils.ContentEquals(decompressedBytes, fileBytes)) {
                EditorUtility.DisplayDialog("Whoops!", "Decompression did not match", "okay");
            } else {
                EditorUtility.DisplayDialog("Success!!", string.Format("Decompression worked! Compression Ratio {0}%", 100f * fileBytes.Length / compressedBytes.Length), "hell yeah");
            }

            EditorUtility.OpenWithDefaultApp(Path.GetDirectoryName(filePath));
        }
    }
}