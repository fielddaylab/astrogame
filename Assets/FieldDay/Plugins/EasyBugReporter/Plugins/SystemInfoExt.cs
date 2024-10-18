/*
 * Copyright (C) 2022. Autumn Beauchesne. All rights reserved.
 * Author:  Autumn Beauchesne
 * Date:    22 May 2022
 * 
 * File:    SystemInfoExt.cs
 * Purpose: Class for exporting SystemInfo properties to a string.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace UnityEngine {
    /// <summary>
    /// API for writing all SystemInfo properties to a string or stream.
    /// </summary>
    static public class SystemInfoExt {
        static public void Report(StringBuilder writer, bool includeSecureInfo = false) {

            foreach(var prop in GetProperties(includeSecureInfo)) {
                Property(writer, prop.Name, prop.GetValue(null));
            }

            // handle trailing newline
            writer.Length--;
        }

        static public string Report(bool includeSecureInfo = false) {
            StringBuilder sb = new StringBuilder(256);
            Report(sb, includeSecureInfo);
            return sb.ToString();
        }

        static public void Report(TextWriter writer, bool includeSecureInfo = false) {
            foreach (var prop in GetProperties(includeSecureInfo)) {
                Property(writer, prop.Name, prop.GetValue(null));
            }
        }

        #region Helpers

        static private IEnumerable<PropertyInfo> GetProperties(bool includeSecureInfo) {
            foreach (var prop in typeof(SystemInfo).GetProperties(BindingFlags.Public | BindingFlags.Static)) {
                if (prop.IsDefined(typeof(ObsoleteAttribute))) {
                    continue;
                }

                if (!includeSecureInfo) {
                    if (prop.Name == "deviceName" || prop.Name == "deviceUniqueIdentifier") {
                        continue;
                    }
                }

                yield return prop;
            }
        }

        static private void Property(StringBuilder writer, string propertyName, object value) {
            writer.Append(propertyName).Append(": ").Append(value).Append('\n');
        }

        static private void Property(TextWriter writer, string propertyName, object value) {
            writer.Write(propertyName);
            writer.Write(": ");
            writer.Write(value);
            writer.Write('\n');
        }

        #endregion // Helpers
    }
}