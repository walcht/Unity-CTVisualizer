using System.Collections.Generic;
using UnityEngine;

namespace UnityCTVisualizer
{
    public struct ControlColor
    {
        public float position;
        public Color color;
        public ControlColor(float position, Color color) {
            this.position = position;
            this.color = color;
        }
    }

    public struct ControlAlpha
    {
        public float position;
        public float alpha;

        public ControlAlpha(float position, float alpha) {
            this.position = position;
            this.alpha = alpha;
        }
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
            List<ControlColor> sortedColorControls = new(colorControls);
            List<ControlAlpha> sortedAlphaControls = new(alphaControls);
            // 1.   sort both control arrays according to their position
            sortedColorControls.Sort((x, y) => x.position.CompareTo(y.position));
            sortedAlphaControls.Sort((x, y) => x.position.CompareTo(y.position));
            
            // 2.   add color and opacity controls at the extremities if they don't exist.
            //      make sure these extend their closes control points
            if (sortedColorControls.Count == 0) {
                sortedColorControls.Add(new ControlColor(1.0f, Color.white));
            }
            ControlColor lastColorControlPoint = sortedColorControls[sortedColorControls.Count - 1];
            if (lastColorControlPoint.position < 1.0f) {
                sortedColorControls.Add(new ControlColor(1.0f, lastColorControlPoint.color));
            }

            ControlColor firstColorControlPoint = sortedColorControls[0];
            if (firstColorControlPoint.position > 1.0f) {
                sortedColorControls.Insert(0, new ControlColor(1.0f, firstColorControlPoint.color));
            }

            if (sortedAlphaControls.Count == 0) {
                sortedAlphaControls.Add(new ControlAlpha(1.0f, 1.0f));
            }

            ControlAlpha lastAlphaControlPoint = sortedAlphaControls[sortedAlphaControls.Count - 1];
            if (lastAlphaControlPoint.position < 1.0f) {
                sortedAlphaControls.Add(new ControlAlpha(1.0f, lastAlphaControlPoint.alpha));
            }

            ControlAlpha firstAlphaControlPoint = sortedAlphaControls[0];
            if (firstAlphaControlPoint.position > 1.0f) {
                sortedAlphaControls.Insert(0, new ControlAlpha(1.0f, firstAlphaControlPoint.alpha));
            }
           
            int colorControlIndex = 0;
            int alphaControlIndex = 0;
            int numOfColors = sortedColorControls.Count;
            int numOfAlphas = sortedAlphaControls.Count;

            // 3. map texture width index to the range [0.0, 1.0]. We call that value t
            for (int textureIndex = 0; textureIndex < textureWidth; textureIndex++) {
                float currentDensity = textureIndex / (float)(textureWidth - 1); // calculate density value for current index
                // find nearest left color control point to density
                while (colorControlIndex < numOfColors - 2 && sortedColorControls[colorControlIndex + 1].position < currentDensity) {
                    colorControlIndex++;
                }
                // find nearest left alpha control point to density
                while (alphaControlIndex < numOfColors - 2 && sortedAlphaControls[alphaControlIndex + 1].position < currentDensity) {
                    alphaControlIndex++;
                }

                ControlColor leftColorControl = sortedColorControls[colorControlIndex];
                ControlColor rightColorControl = sortedColorControls[colorControlIndex + 1];
                ControlAlpha leftAlphaControl = sortedAlphaControls[alphaControlIndex];
                ControlAlpha rightAlphaControl = sortedAlphaControls[alphaControlIndex + 1];
                // Min-Max scaling normalization for density, map density to range [0.0, 1.0]
                // 1. left color control point position - 0.0, right color control position - 1.0
                // 2. left alpha control point position - 0.0, right alpha control position - 1.0
                float normalizedDensityForColor = (Mathf.Clamp(currentDensity, leftColorControl.position, rightColorControl.position) - leftColorControl.position) /
                    (rightColorControl.position - leftColorControl.position);
                float normalizedDensityForAlpha = (Mathf.Clamp(currentDensity, leftAlphaControl.position, rightAlphaControl.position) - leftAlphaControl.position) /  
                    (rightAlphaControl.position - leftAlphaControl.position);

                // Interpolates between min and max with smoothing at the limits. smooth(min, max, t)
                // similar to Lerp, but the interpolation will gradually speed up from the start and slow down toward the end
                float smoothedDensityForColor = Mathf.SmoothStep(0.0f, 1.0f, normalizedDensityForColor);
                float smoothedAlphaForColor = Mathf.SmoothStep(0.0f, 1.0f, normalizedDensityForAlpha);

                // linear interpolation
                Color pixelColor = rightColorControl.color * smoothedDensityForColor + leftColorControl.color * (1.0f - smoothedDensityForColor);
                pixelColor.a = rightAlphaControl.alpha * smoothedAlphaForColor + leftAlphaControl.alpha * (1.0f - smoothedAlphaForColor);

                pixelColorData[textureIndex] = QualitySettings.activeColorSpace == ColorSpace.Linear ? pixelColor.linear : pixelColor;   
                
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.SetPixels(pixelColorData);
                texture.Apply();
            }


            // 4.   determine left control color point index and left control opacity index - done
            // 5.   initialize left and right color and alpha control points - done
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
