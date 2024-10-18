using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Variants;
using UnityEngine;
using System.Runtime.CompilerServices;

#if UNITY_EDITOR
using System.IO;
#endif // UNITY_EDITOR

namespace FieldDay.Data {
    /// <summary>
    /// Command line argument retrieval.
    /// Use the file "UserSettings/MockCommandLineArgs.txt" to test these in the editor.
    /// </summary>
    static public class CommandLineArgs {
#if UNITY_EDITOR
        private const string ConfigFile = "UserSettings/MockCommandLineArgs.txt";
#endif // UNITY_EDITOR

        static private readonly QueryParams s_Params = new QueryParams();
        static private bool s_Initialized;

        #region Initialization

        static internal void Initialize() {
            if (s_Initialized) {
                return;
            }

            Log.Msg("[CommandLineArgs] Reading command line arguments...");
            s_Params.TryParse(GetUrl());
            s_Initialized = true;
        }

        static private string GetUrl() {
#if UNITY_EDITOR
            if (File.Exists(ConfigFile))
                return File.ReadAllText(ConfigFile);
#endif// UNITY_EDITOR
            return Application.absoluteURL;
        }

        #endregion // Initialization

        #region Accessors

        /// <summary>
        /// Returns if the given flag is defined in the query parameters.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool HasFlag(string flagName) {
            Initialize();
            return s_Params.Contains(flagName);
        }

        /// <summary>
        /// Attempts to read a value from the configuration.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool ReadValue(string valueName, out string outValue) {
            Initialize();
            if (s_Params.Contains(valueName)) {
                outValue = s_Params.Get(valueName);
                return true;
            } else {
                outValue = null;
                return false;
            }
        }

        /// <summary>
        /// Attempts to read a value from the configuration.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool ReadValue(string valueName, out Variant outValue) {
            Initialize();
            if (s_Params.Contains(valueName)) {
                outValue = s_Params.GetVariant(valueName);
                return true;
            } else {
                outValue = Variant.Null;
                return false;
            }
        }

        #endregion // Accessors
    }
}