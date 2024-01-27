using System.Collections.Generic;
using UnityEngine;

namespace UnityCTVisualizer
{
    public struct ControlColor
    {
        float position;
        Color color;
    }

    public struct ControlAlpha
    {
        float position;
        float alpha;
    }

    public static class Name
    {
        /// <summary>
        /// Generates a color look-up 1D (only needs one parameter: density) texture
        ///
        /// The returned 2D texture (which is actually a 1D texture but Shaders don't accept a 1D texture
        /// that's why we use a 2D with a height of 1) will be used as a look-up array to determine the
        /// color (RGB) and the opacity (A) for a given density (i.e., index by density
        /// texture_array[density] to return an RGBA color). The first color of the texture array is mapped
        /// to the lowest density value. The last color is mapped to the highest density.
        ///     minDensity(0.0)                     => 0
        ///     maxDensity(1.0)                     => TEXURE_WIDTH - 1
        ///     density (belongs to [0.0, 1.0])     => (int)(density * (TEXURE_WIDTH - 1))
        ///
        /// </summary>
        /// <param name="colorControls">List of provided color controls. These are going to be interpolated
        /// to generate the texture colors.</param>
        /// <param name="alphaControls">List of provided alphas (opacities). These are going to be
        /// interpolated to generate the texture opacities.</param>
        /// <returns>A 512 X 1 color and opacities (RGBA) 2D texture.</returns>
        public static Texture2D TransferFunction1D(
            List<ControlColor> colorControls,
            List<ControlAlpha> alphaControls
        )
        {
            const int textureWidth = 512;
            const int textureHeight = 1;
            TextureFormat texFormat = SystemInfo.SupportsTextureFormat(TextureFormat.RGBAHalf)
                ? TextureFormat.RGBAHalf
                : TextureFormat.RGBAFloat;
            Texture2D texture = new Texture2D(textureWidth, textureHeight, texFormat, false);
            Color[] pixelColorData = new Color[textureWidth * textureHeight];
            // TODO: [Adrienne] populate the pixelColorData array by interpolating provided control
            //                  colors and opacities.

            // 1.   sort both control arrays according to their position
            // 2.   add color and opacity controls at the extremities if they don't exist.
            //      make sure these extend their closes control points

            // 3.   map texture width index to the range [0.0, 1.0]. We call that value t
            // 4.   determine left control color point index and left control opacity index
            // 5.   initialize left and right color and alpha control points
            // 6.   map current t value to range [0.0, 1.0] where 0.0 means t = leftColor.position
            //      and 1.0 means t = rightColor.position
            // 7.   do the same as previous step for opacity
            // 8.   maybe, smooth the result of previous operation? (like using smoothstep)
            // 9.   use simple linear interpolation to interpolate between left and right points
            // 10.  populate pixelColorData array at current index and don't forget to take the color
            //      space into account!

            texture.SetPixels(pixelColorData);
            return texture;
        }
    }
}
