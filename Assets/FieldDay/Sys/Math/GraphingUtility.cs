using System;
using System.Runtime.CompilerServices;
using BeauUtil;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;

namespace FieldDay.Mathematics {
    /// <summary>
    /// Utility methods for calculating graphs.
    /// </summary>
    static public class GraphingUtility {
        static private readonly double[] GoodNormalizedTicks = new double[] { 1, 1.5, 2, 2.5, 5, 10 };
        static private readonly int GoodNormalizedTickCount = 6;

        /// <summary>
        /// Calculates a good range and tick count for displaying values in the given range.
        /// </summary>
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.DivideByZeroChecks, false)]
        static public GraphAxisRange CalculateAxis(float min, float max, uint targetTickCount) {
            // algorithm adapted from: https://stackoverflow.com/a/49911176

            if (targetTickCount < 2) {
                throw new ArgumentOutOfRangeException("targetTickCount", "Target ticks should be at least 2");
            }

            double minD = min, maxD = max;
            double epsilon = (max - min) / 1e6;
            maxD += epsilon;
            minD -= epsilon;
            double range = maxD - minD;

            double roughStep = range / (targetTickCount - 1);
            double absRoughStep = Math.Abs(roughStep);

            double stepPower = Math.Pow(10, -Math.Floor(Math.Log10(absRoughStep)));
            double normalizedStep = roughStep * stepPower;
            double goodNormalizedStep = normalizedStep;
            for(int i = 0; i < GoodNormalizedTickCount; i++) {
                if (GoodNormalizedTicks[i] >= normalizedStep) {
                    goodNormalizedStep = GoodNormalizedTicks[i];
                    break;
                }
            }

            double step = goodNormalizedStep / stepPower;

            double rangeMin = Math.Floor(minD / step) * step;
            double rangeMax = Math.Ceiling(maxD / step) * step;
            uint tickCount = 1 + (uint) ((rangeMax - rangeMin) / step);

            GraphAxisRange axis;
            axis.Min = (float) rangeMin;
            axis.Max = (float) rangeMax;
            axis.TickCount = tickCount;
            axis.TickInterval = (float) step;
            return axis;
        }

        /// <summary>
        /// Calculates good axis values for the given 2d range.
        /// </summary>
        static public GraphAxisRange2 CalculateAxes(Rect range, uint targetTickCountX, uint targetTickCountY) {
            GraphAxisRange2 range2;
            range2.X = CalculateAxis(range.xMin, range.xMax, targetTickCountX);
            range2.Y = CalculateAxis(range.yMin, range.yMax, targetTickCountY);
            return range2;
        }

        /// <summary>
        /// Calculates good axis values for the given 3d range.
        /// </summary>
        static public GraphAxisRange3 CalculateAxes(Bounds range, uint targetTickCountX, uint targetTickCountY, uint targetTickCountZ) {
            GraphAxisRange3 range3;
            Vector3 min = range.min, max = range.max;
            range3.X = CalculateAxis(min.x, max.x, targetTickCountX);
            range3.Y = CalculateAxis(min.y, max.y, targetTickCountY);
            range3.Z = CalculateAxis(min.z, max.z, targetTickCountZ);
            return range3;
        }
    }

    /// <summary>
    /// Describes the layout of a single axis on a graph.
    /// </summary>
    public struct GraphAxisRange {
        public float Min;
        public float Max;
        public uint TickCount;
        public float TickInterval;

        public float Size {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Max - Min; }
        }

        public void AdjustMinToOrigin() {
            Max -= Min;
            Min = 0;
        }

        public override string ToString() {
            return string.Format("{0} -> {1} ({2} ticks, interval {3})", Min, Max, TickCount, TickInterval);
        }
    }

    /// <summary>
    /// Two-axis range.
    /// </summary>
    public struct GraphAxisRange2 {
        public GraphAxisRange X;
        public GraphAxisRange Y;

        public Vector2 Min {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Vector2(X.Min, Y.Min); }
        }

        public Vector2 Max {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Vector2(X.Max, Y.Max); }
        }

        public Vector2 Size {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Vector2(X.Size, Y.Size); }
        }

        public float Width {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return X.Size; }
        }

        public float Height {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Y.Size; }
        }

        public Rect ToRect() {
            return Rect.MinMaxRect(X.Min, Y.Min, X.Max, Y.Max);
        }
    }

    /// <summary>
    /// Three-axis range.
    /// </summary>
    public struct GraphAxisRange3 {
        public GraphAxisRange X;
        public GraphAxisRange Y;
        public GraphAxisRange Z;

        public Vector3 Min {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Vector3(X.Min, Y.Min, Z.Min); }
        }

        public Vector3 Max {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Vector3(X.Max, Y.Max, Z.Max); }
        }

        public Vector3 Size {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Vector3(X.Size, Y.Size, Z.Size); }
        }

        public float Width {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return X.Size; }
        }

        public float Height {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Y.Size; }
        }

        public float Depth {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Z.Size; }
        }

        public Bounds ToBounds() {
            return Geom.AABB(new Vector3(X.Min, Y.Min, Z.Min), new Vector3(X.Max, Y.Max, Z.Max));
        }
    }
}