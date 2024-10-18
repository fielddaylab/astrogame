using BeauUtil.Debugger;
using UnityEngine;

namespace FieldDay.Rendering {
    /// <summary>
    /// LUT (Look-up table) texture generation utilities.
    /// </summary>
    static public class LUTUtility {
        static public void WriteLUT(Gradient gradient, Texture2D dst, int x, int y, int width, int height) {
            Assert.NotNull(gradient);
            Assert.NotNull(dst);
            Assert.True(dst.format == TextureFormat.RGBA32);
            Assert.True(width >= 2 && height >= 1, "Invalid width/height specified");
            
            var pixels = dst.GetPixelData<Color32>(0);
            int stride = dst.width;

            int baseIdx = y * stride;
            for (int ix = 0; ix < width; ix++) {
                pixels[baseIdx + x + ix] = gradient.Evaluate((float) ix / (width - 1));
            }

            for(int iy = 1; iy < height; iy++) {
                baseIdx = iy * stride;
                for (int ix = 0; ix < width; ix++) {
                    pixels[baseIdx + x + ix] = pixels[baseIdx - stride + x + ix];
                }
            }

            dst.Apply();
        }

        static public void WriteLUT(Gradient gradient, Texture2D dst) {
            Assert.NotNull(dst);
            WriteLUT(gradient, dst, 0, 0, dst.width, dst.height);
        }

        static public Texture2D CreateLUT(int width, int height, FilterMode filter = FilterMode.Bilinear, TextureWrapMode wrap = TextureWrapMode.Clamp) {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = filter;
            tex.wrapMode = wrap;
            return tex;
        }
    }
}