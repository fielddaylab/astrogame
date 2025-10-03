using System.Runtime.CompilerServices;
using System.Text;
using BeauUtil;
using BeauUtil.Debugger;

namespace FieldDay.Localization {
    static public class Loc {
        #region Cached Vars

        static private LanguageId s_DefaultLang;
        static private LanguageId s_CurrentLang;

        #endregion // Cached Vars

        #region Current Language

        /// <summary>
        /// Current language id.
        /// </summary>
        static public LanguageId Language {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return s_CurrentLang; }
        }

        #endregion // Current Language

        #region Defaults

        /// <summary>
        /// Configures the default language.
        /// </summary>
        static public void ConfigureDefaultLanguage(LanguageId defaultLanguageId) {
            Assert.True(s_DefaultLang.IsEmpty && s_CurrentLang.IsEmpty, "Defaults have already been configured!");
            s_DefaultLang = s_CurrentLang = defaultLanguageId;
        }

        /// <summary>
        /// Is the localization system currently running with its default language.
        /// </summary>
        static public bool IsDefaultLanguage() {
            return s_DefaultLang == s_CurrentLang;
        }

        #endregion // Defaults

        #region File Paths

        /// <summary>
        /// Returns if the given path is localized.
        /// </summary>
        static public unsafe bool IsLocalizedPath(string path) {
            Assert.NotNull(path);

            int pathLen = path.Length;
            if (pathLen < 3) {
                return false;
            }

            fixed (char* buff = path) {

                s_DefaultLang.ToChars(out char checkA, out char checkB);

                int idx = 0;

                if (buff[0] == checkA && buff[1] == checkB && buff[2] == '/') {
                    return true;
                }

                for (; idx < pathLen - 2; idx++) {
                    char c = buff[idx];
                    if (c == '/' && idx + 3 < pathLen && buff[idx + 3] == '/') {
                        // two character path
                        if (buff[idx + 1] == checkA && buff[idx + 2] == checkB) {
                            return true;
                        }
                        idx += 3;
                    } else if (c == '.') {
                        if ((idx + 2 == pathLen) || ((idx + 3) < pathLen && buff[idx + 3] == '.')) {
                            // two character extension
                            if (buff[idx + 1] == checkA && buff[idx + 2] == checkB) {
                                return true;
                            }
                            idx += 3;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Returns if the given path is localized.
        /// </summary>
        static public unsafe bool IsLocalizedPath(StringBuilder path) {
            Assert.NotNull(path);

            int pathLen = path.Length;
            if (pathLen < 3) {
                return false;
            }

            s_DefaultLang.ToChars(out char checkA, out char checkB);

            int idx = 0;

            if (path[0] == checkA && path[1] == checkB && path[2] == '/') {
                return true;
            }

            for (; idx < pathLen - 2; idx++) {
                char c = path[idx];
                if (c == '/' && idx + 3 < pathLen && path[idx + 3] == '/') {
                    // two character path
                    if (path[idx + 1] == checkA && path[idx + 2] == checkB) {
                        return true;
                    }
                    idx += 3;
                } else if (c == '.') {
                    if ((idx + 2 == pathLen) || ((idx + 3) < pathLen && path[idx + 3] == '.')) {
                        // two character extension
                        if (path[idx + 1] == checkA && path[idx + 2] == checkB) {
                            return true;
                        }
                        idx += 3;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Localizes a file path. This replaces instances of
        /// the default language's two-letter code with the current language.
        /// Specifically, the formats /en/, en/, and .en
        /// </summary>
        static public unsafe string Path(string path, out bool changed) {
            Assert.NotNull(path);

            if (s_CurrentLang == s_DefaultLang) {
                changed = false;
                return path;
            }

            int pathLen = path.Length;
            if (pathLen < 3) {
                changed = false;
                return path;
            }

            char* buff = stackalloc char[pathLen];
            fixed (char* p = path) {
                Unsafe.FastCopyArray(p, pathLen, buff);
            }

            s_DefaultLang.ToChars(out char checkA, out char checkB);
            s_CurrentLang.ToChars(out char newA, out char newB);

            changed = false;
            int idx = 0;

            if (buff[0] == checkA && buff[1] == checkB && buff[2] == '/') {
                buff[0] = newA;
                buff[1] = newB;
                changed = true;
                idx += 3;
            }

            for (; idx < pathLen - 2; idx++) {
                char c = buff[idx];
                if (c == '/' && idx + 3 < pathLen && buff[idx + 3] == '/') {
                    // two character path
                    if (buff[idx + 1] == checkA && buff[idx + 2] == checkB) {
                        buff[idx + 1] = newA;
                        buff[idx + 2] = newB;
                        changed = true;
                    }
                    idx += 3;
                } else if (c == '.') {
                    if ((idx + 2 == pathLen) || ((idx + 3) < pathLen && buff[idx + 3] == '.')) {
                        // two character extension
                        if (buff[idx + 1] == checkA && buff[idx + 2] == checkB) {
                            buff[idx + 1] = newA;
                            buff[idx + 2] = newB;
                            changed = true;
                        }
                        idx += 3;
                    }
                }
            }

            if (changed) {
                return new string(buff, 0, pathLen);
            }

            return path;
        }

        /// <summary>
        /// Localizes a file path. This replaces instances of
        /// the default language's two-letter code with the current language.
        /// Specifically, the formats /en/, en/, and .en
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public unsafe string Path(string path) {
            return Path(path, out bool _);
        }

        /// <summary>
        /// Localizes a file path. This replaces instances of
        /// the default language's two-letter code with the current language.
        /// Specifically, the formats /en/, en/, and .en
        /// </summary>
        static public unsafe bool Path(StringBuilder path) {
            Assert.NotNull(path);

            if (s_CurrentLang == s_DefaultLang) {
                return false;
            }

            int pathLen = path.Length;
            if (pathLen < 3) {
                return false;
            }

            s_DefaultLang.ToChars(out char checkA, out char checkB);
            s_CurrentLang.ToChars(out char newA, out char newB);

            bool changed = false;
            int idx = 0;

            if (path[0] == checkA && path[1] == checkB && path[2] == '/') {
                path[0] = newA;
                path[1] = newB;
                changed = true;
                idx += 3;
            }

            for (; idx < pathLen - 2; idx++) {
                char c = path[idx];
                if (c == '/' && idx + 3 < pathLen && path[idx + 3] == '/') {
                    // two character path
                    if (path[idx + 1] == checkA && path[idx + 2] == checkB) {
                        path[idx + 1] = newA;
                        path[idx + 2] = newB;
                        changed = true;
                    }
                    idx += 3;
                } else if (c == '.') {
                    if ((idx + 2 == pathLen) || ((idx + 3) < pathLen && path[idx + 3] == '.')) {
                        // two character extension
                        if (path[idx + 1] == checkA && path[idx + 2] == checkB) {
                            path[idx + 1] = newA;
                            path[idx + 2] = newB;
                            changed = true;
                        }
                        idx += 3;
                    }
                }
            }

            return changed;
        }

        /// <summary>
        /// Localizes a file path. This replaces instances of
        /// the default language's two-letter code with the current language.
        /// Specifically, the formats /en/, en/, and .en
        /// </summary>
        static public unsafe bool Path(char* path, int pathLength) {
            Assert.NotNull(path);

            if (s_CurrentLang == s_DefaultLang) {
                return false;
            }

            int pathLen = pathLength;
            if (pathLen < 3) {
                return false;
            }

            s_DefaultLang.ToChars(out char checkA, out char checkB);
            s_CurrentLang.ToChars(out char newA, out char newB);

            bool changed = false;
            int idx = 0;

            if (path[0] == checkA && path[1] == checkB && path[2] == '/') {
                path[0] = newA;
                path[1] = newB;
                changed = true;
                idx += 3;
            }

            for (; idx < pathLen - 2; idx++) {
                char c = path[idx];
                if (c == '/' && idx + 3 < pathLen && path[idx + 3] == '/') {
                    // two character path
                    if (path[idx + 1] == checkA && path[idx + 2] == checkB) {
                        path[idx + 1] = newA;
                        path[idx + 2] = newB;
                        changed = true;
                    }
                    idx += 3;
                } else if (c == '.') {
                    if ((idx + 2 == pathLen) || ((idx + 3) < pathLen && path[idx + 3] == '.')) {
                        // two character extension
                        if (path[idx + 1] == checkA && path[idx + 2] == checkB) {
                            path[idx + 1] = newA;
                            path[idx + 2] = newB;
                            changed = true;
                        }
                        idx += 3;
                    }
                }
            }

            return changed;
        }

        #endregion // File Paths
    }
}