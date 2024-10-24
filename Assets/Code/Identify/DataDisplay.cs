using System;
using System.Text;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Components;
using TMPro;
using UnityEngine;

namespace Astro {
    public sealed class DataDisplay : BatchedComponent {
        [AutoEnum] public DataFormattingFlags Formatting;

        [Header("Components")]
        public TMP_Text DefaultOutput;

        public readonly CastableEvent<DataPacket, DataFormattingFlags> OnDisplayRequested = new CastableEvent<DataPacket, DataFormattingFlags>();
        public readonly ActionEvent OnDisplayCleared = new ActionEvent();
    }

    [Flags]
    public enum DataFormattingFlags : uint {
        RightAscension = 0x01,
        Declination = 0x02,

        [Hidden] FullCoordinate = RightAscension | Declination
    }

    static public partial class DataUtility {
        /// <summary>
        /// Populates a data display.
        /// </summary>
        static public void PopulateDisplay(DataDisplay display, DataPacket packet) {
            bool displayedDefault = false;
            if (display.DefaultOutput) {
                using(PooledStringBuilder psb = PooledStringBuilder.Create()) {
                    displayedDefault = TryFormatForDefaultOutput(packet, display.Formatting, psb);
                    display.DefaultOutput.SetText(psb);
                }
            }

            if (!displayedDefault && display.OnDisplayRequested.IsEmpty) {
                Log.Error("[DataUtility] Data display accepted type '{0}' but was unable to display data");
            } else {
                display.OnDisplayRequested.Invoke(packet, display.Formatting);
            }
        }

        static public void ClearDisplay(DataDisplay display) {
            if (display.DefaultOutput) {
                display.DefaultOutput.SetText(string.Empty);
            }

            display.OnDisplayCleared.Invoke();
        }

        static private bool TryFormatForDefaultOutput(DataPacket packet, DataFormattingFlags flags, StringBuilder sb) {
            switch (packet.Type) {
                case DataTypeMask.Name: {
                    CelestialAsset asset = Find.NamedAsset<CelestialAsset>(packet.Value.AssetId);
                    sb.Append(asset.DisplayName);
                    return true;
                }

                case DataTypeMask.Color: {
                    ReferenceColor asset = Find.NamedAsset<ReferenceColor>(packet.Value.AssetId);
                    sb.Append(asset.Label);
                    return true;
                }

                case DataTypeMask.ApparentMagnitude:
                case DataTypeMask.AbsoluteMagnitude: {
                    sb.AppendNoAlloc(packet.Value.Magnitude, 1);
                    return true;
                }

                case DataTypeMask.Temperature: {
                    sb.AppendNoAlloc(packet.Value.Temperature, 0).Append("° K");
                    return true;
                }

                case DataTypeMask.Distance: {
                    sb.AppendNoAlloc(packet.Value.Distance, 2).Append("pc");
                    return true;
                }

                case DataTypeMask.Coordinates: {
                    EqCoords coords = packet.Value.Coordinates;
                    bool rightAsc = flags == 0 || (flags & DataFormattingFlags.RightAscension) != 0;
                    bool decl = flags == 0 || (flags & DataFormattingFlags.Declination) != 0;
                    bool hasSeparator = rightAsc & decl;
                    if (rightAsc) {
                        coords.RightAscension.ToString(sb, HmsPrefix.Hms);
                    }
                    if (hasSeparator) {
                        sb.Append(", ");
                    }
                    if (decl) {
                        coords.Declination.ToString(sb, HmsPrefix.Quotes);
                    }

                    return true;
                }

                case DataTypeMask.MaterialSpectrum: {
                    SpectrographMaterialMask materials = packet.Value.Materials;
                    if (materials != 0) {
                        if ((materials & SpectrographMaterialMask.Hydrogen) != 0) {
                            sb.Append("H, ");
                        }
                        if ((materials & SpectrographMaterialMask.Helium) != 0) {
                            sb.Append("He, ");
                        }
                        if ((materials & SpectrographMaterialMask.Carbon) != 0) {
                            sb.Append("C, ");
                        }
                        if ((materials & SpectrographMaterialMask.Oxygen) != 0) {
                            sb.Append("O, ");
                        }
                        if ((materials & SpectrographMaterialMask.Sodium) != 0) {
                            sb.Append("Na, ");
                        }
                        if ((materials & SpectrographMaterialMask.Magnesium) != 0) {
                            sb.Append("Mg, ");
                        }
                        if ((materials & SpectrographMaterialMask.Calcium) != 0) {
                            sb.Append("Ca, ");
                        }
                        if ((materials & SpectrographMaterialMask.Iron) != 0) {
                            sb.Append("Fe, ");
                        }
                        sb.Length -= 2; // trim last delim
                    }
                    return true;
                }

                default: {
                    return false;
                }
            }
        }
    }
}