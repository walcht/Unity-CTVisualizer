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
        /// </summary>
        /// <param name="colorControls">List of provided color controls. These are going to be interpolated to generate the texture colors.</param>
        /// <param name="alphaControls">List of provided alphas (opacities). These are going to be interpolated to generate the texture opacities.</param>
        /// <returns>A 512 X 1 color and opacities (RGBA) 2D texture.</returns>
        public static Texture2D TransferFunction1D(
            List<ControlColor> colorControls,
            List<ControlAlpha> alphaControls
        ) { }
    }
}
