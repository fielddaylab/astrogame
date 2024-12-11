using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class TextIO
{
    public static void WriteString(string path, string contents) {
        //Write some text to the test.txt file
        StreamWriter writer = new StreamWriter(path, false);
        writer.WriteLine(contents);
        writer.Close();
        //Re-import the file to update the reference in the editor
#if UNITY_EDITOR
        AssetDatabase.ImportAsset(path);
#endif
    }
    public static string ReadString(string path) {
        //Read the text from directly from the test.txt file
        StreamReader reader = new StreamReader(path);
        string inputStr = reader.ReadToEnd();
        //Debug.Log(reader.ReadToEnd());
        reader.Close();

        return inputStr;
    }

    public static List<string> TextAssetToList(TextAsset ta, string delim) {
        return new List<string>(ta.text.Split(delim));
    }
}