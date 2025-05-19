using System;
using System.Text;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Components;
using FieldDay.UI;
using ScriptableBake;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

namespace Astro {
    public sealed class DataDisplay : BatchedComponent, IBaked {
        [AutoEnum] public DataFormattingFlags Formatting;
        public string NullText;

        [Header("Components")]
        public TMP_Text DefaultOutput;
        public Transform OutputTransform;
        public RenderAtlasOutput OutputAtlas;

        public readonly CastableEvent<DataPacket, DataFormattingFlags> OnDisplayRequested = new CastableEvent<DataPacket, DataFormattingFlags>();
        public readonly ActionEvent OnDisplayCleared = new ActionEvent();

#if UNITY_EDITOR

        int IBaked.Order => 100;

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            DataUtility.ClearDisplay(this);
            return true;
        }

#endif // UNITY_EDITOR
    }

    [Flags]
    public enum DataFormattingFlags : uint {
        RightAscension = 0x01,
        Declination = 0x02,
        Shorten = 0x04,

        [Hidden] FullCoordinate = RightAscension | Declination
    }

    static public partial class DataUtility {
        static readonly string EMPTY_OUTPUT = "[Null]";
        /// <summary>
        /// Populates a data display.
        /// </summary>
        static public void PopulateDisplay(DataDisplay display, DataPacket packet) {
            bool displayedDefault = false;
            if (display == null) return;
            if (display.DefaultOutput) {
                using(PooledStringBuilder psb = PooledStringBuilder.Create()) {
                    if (packet.IsValid) {
                        displayedDefault = TryFormatForDefaultOutput(packet, display.Formatting, psb);
                    }
                    else {
                        string nullTxt = display.NullText;
                        if (string.IsNullOrEmpty(nullTxt)) {
                            nullTxt = EMPTY_OUTPUT;
                        }
                        psb.Builder.Append(nullTxt);
                        displayedDefault = true;
                    }
                    display.DefaultOutput.SetText(psb);
                }
            }

            if (!displayedDefault && display.OnDisplayRequested.IsEmpty) {
                Log.Error("[DataUtility] Data display accepted type '{0}' but was unable to display data", packet.Type);
            } else {
                display.OnDisplayRequested.Invoke(packet, display.Formatting);
            }

            if (display.OutputAtlas) {
                display.OutputAtlas.MarkDirty();
            }
        }

        static public void ClearDisplay(DataDisplay display) {
            if (display.DefaultOutput) {
                string nullTxt = display.NullText;
                if (string.IsNullOrEmpty(nullTxt)) {
                    nullTxt = EMPTY_OUTPUT;
                }
                display.DefaultOutput.SetText(nullTxt);
            }

            display.OnDisplayCleared.Invoke();
            if (display.OutputAtlas) {
                display.OutputAtlas.MarkDirty();
            }
        }

        static public void SetDisplayHidden(DataDisplay display, bool hide) {
            if (display.DefaultOutput) {
                if (hide) {
                    display.DefaultOutput.gameObject.SetActive(false);
                } else {
                    display.DefaultOutput.gameObject.SetActive(true);
                }
            } else {
                throw new NotImplementedException("Hiding non-text data display not yet implemented");
            }
        }

        static private bool TryFormatForDefaultOutput(DataPacket packet, DataFormattingFlags flags, StringBuilder sb) {
            bool shorten = (flags & DataFormattingFlags.Shorten) != 0;
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

                case DataTypeMask.ColorIndex: {
                    sb.AppendNoAlloc(packet.Value.ColorIndex, 2);
                    return true;
                }

                case DataTypeMask.ApparentMagnitude:
                case DataTypeMask.BlueMagnitude:
                case DataTypeMask.InfraredMagnitude:
                case DataTypeMask.AbsoluteMagnitude: {
                    sb.AppendNoAlloc(packet.Value.Magnitude, 2);
                    return true;
                }

                case DataTypeMask.Temperature: {
                    sb.AppendNoAlloc(packet.Value.Temperature, 0).Append("K");
                    return true;
                }

                case DataTypeMask.Distance: {
                    sb.AppendNoAlloc(packet.Value.Distance, shorten ? 0 : 1);
                    if (!shorten) {
                        sb.Append(" lightyears");
                    } else {
                        sb.Append(" ly");
                    }
                    return true;
                }

                case DataTypeMask.Coordinates: {
                    EqCoords coords = packet.Value.Coordinates;
                    coords.Declination.Sanitize();
                    coords.RightAscension.Sanitize();

                    bool rightAsc = flags == 0 || (flags & DataFormattingFlags.RightAscension) != 0;
                    bool decl = flags == 0 || (flags & DataFormattingFlags.Declination) != 0;
                    bool hasSeparator = rightAsc & decl;
                    if (rightAsc) {
                        coords.RightAscension.ToString(sb);
                    }
                    if (hasSeparator) {
                        sb.Append(",\n");
                    }
                    if (decl) {
                        coords.Declination.ToString(sb);
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