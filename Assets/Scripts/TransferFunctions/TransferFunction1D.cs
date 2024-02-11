using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityCTVisualizer
{
    public static class TFConstants
    {
        public const int MAX_COLOR_CONTROL_POINTS = 16;
        public const int MAX_ALPHA_CONTROL_POINTS = 16;
    }

    [CreateAssetMenu(
        fileName = "transfer_function_1d",
        menuName = "UnityCTVisualizer/TransferFunction1D"
    )]
    public class TransferFunction1D
        : ScriptableObject,
            ITransferFunction,
            ISerializationCallbackReceiver
    {
        /// <summary>
        /// Invoked whenever the underlying 1D transfer function texture has changed.
        /// </summary>
        /// <remark>
        /// Intended workflow is to use to subscribe to this event to receive TF textures and to call
        /// TryUpdateColorLookupTexture to request an update for the TF texture.
        /// </remark>
        public event Action<Texture2D> TransferFunctionTexChange;

        Dictionary<int, ControlPoint<float, Color>> m_ColorControls = new();
        Dictionary<int, ControlPoint<float, float>> m_AlphaControls = new();
        int m_ColorControlPointsIDAccum = 0;
        int m_AlphaControlPointsIDAccum = 0;

        bool m_DirtyFlag = true;

        /// <summary>
        /// Initializes the 1D transfer function control points data.
        /// Make sure to call this before this ScriptableObject starts executing.
        /// </summary>
        public void Init()
        {
            // default color control points
            AddColorControlPoint(new(0.0f, Color.black));
            AddColorControlPoint(new(0.20f, Color.black));
            AddColorControlPoint(new(0.30f, Color.yellow));
            AddColorControlPoint(new(0.45f, Color.yellow));
            AddColorControlPoint(new(0.55f, Color.red));
            AddColorControlPoint(new(0.70f, Color.red));
            AddColorControlPoint(new(0.80f, Color.white));
            AddColorControlPoint(new(1.00f, Color.white));
            // default alpha control points
            AddAlphaControlPoint(new(0.0f, 0.0f));
            AddAlphaControlPoint(new(0.20f, 0.0f));
            AddAlphaControlPoint(new(0.30f, 0.3f));
            AddAlphaControlPoint(new(0.50f, 0.3f));
            AddAlphaControlPoint(new(0.60f, 0.5f));
            AddAlphaControlPoint(new(0.80f, 0.5f));
            AddAlphaControlPoint(new(0.90f, 0.75f));
            AddAlphaControlPoint(new(1.00f, 0.75f));
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////// GETTERS //////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////

        public IEnumerable<int> ColorControlPointIDs()
        {
            return m_ColorControls.Keys;
        }

        public IEnumerable<int> AlphaControlPointIDs()
        {
            return m_AlphaControls.Keys;
        }

        /// <summary>
        /// Gets the color control point by its unique ID.
        /// </summary>
        /// <param name="cpID">unique ID of the color control point.</param>
        /// <returns>color control point</returns>
        public ControlPoint<float, Color> GetColorControlPointAt(int cpID) => m_ColorControls[cpID];

        /// <summary>
        /// Gets the alpha control point by its unique ID.
        /// </summary>
        /// <param name="cpID">unique ID of the alpha control point.</param>
        /// <returns>alpha control point</returns>
        public ControlPoint<float, float> GetAlphaControlPointAt(int cpID) => m_AlphaControls[cpID];

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////// MODIFIERS /////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////

        public void UpdateColorControlPointValueAt(int cpIndex, Color newColor)
        {
            // dirty flag will be set in OnValueChange callback
            m_ColorControls[cpIndex].Value = newColor;
        }

        public void UpdateColorControlPointPositionAt(int cpIndex, float newPos)
        {
            // dirty flag will be set in OnValueChange callback
            m_ColorControls[cpIndex].Position = newPos;
        }

        /// <summary>
        /// Add a new color control point from which intermediate colors (no alpha) are interpolated.
        /// </summary>
        /// <remark>
        /// Even internally, it is preferable to use this function to add new color control points.
        /// </remark>
        /// <param name="colorCP">color control point data (position and color)</param>
        /// <returns>
        /// unique ID of the newly added color control point. Useful for identification purposes.
        /// </returns>
        public int AddColorControlPoint(ControlPoint<float, Color> colorCP)
        {
            colorCP.OnValueChange += () =>
            {
                m_DirtyFlag = true;
            };
            m_ColorControls.Add(m_ColorControlPointsIDAccum, colorCP);
            m_DirtyFlag = true;
            return m_ColorControlPointsIDAccum++;
        }

        /// <summary>
        /// Adds a new alpha control point from which intermediate alphas are interpolated.
        /// </summary>
        /// <remark>
        /// Even internally, it is preferable to use this function to add new color control points.
        /// </remark>
        /// <param name="alphaCP">alpha control point data (position and alpha)</param>
        /// <returns>
        /// unique ID of the newly added alpha control point. Useful for identification purposes.
        /// </returns>
        public int AddAlphaControlPoint(ControlPoint<float, float> alphaCP)
        {
            alphaCP.OnValueChange += () =>
            {
                m_DirtyFlag = true;
            };
            m_AlphaControls.Add(m_AlphaControlPointsIDAccum, alphaCP);
            m_DirtyFlag = true;
            return m_AlphaControlPointsIDAccum++;
        }

        /// <summary>
        /// Removes the color control point by unique ID
        /// </summary>
        /// <param name="cpID">unique ID of the color control point to be removed</param>
        public void RemoveColorControlPoint(int cpID)
        {
            m_ColorControls.Remove(cpID);
            m_DirtyFlag = true;
            return;
        }

        /// <summary>
        /// Removes the alpha color control point by unique ID
        /// </summary>
        /// <param name="cpID">unique ID of the alpha control point to be remove</param>
        public void RemoveAlphaControlPoint(int cpID)
        {
            m_AlphaControls.Remove(cpID);
            m_DirtyFlag = true;
            return;
        }

        /// <summary>
        /// Removes all color control points. Since at least one color control point is needed for TF texture
        /// generation, a white color control point is added at position 0.5.
        /// </summary>
        public void ClearColorControlPoints()
        {
            m_ColorControls.Clear();
            m_DirtyFlag = true;
            AddColorControlPoint(new(0.5f, Color.white));
        }

        /// <summary>
        /// Removes all alpha control points. Since at least one alpha control point is needed for TF texture
        /// generation, an alpha control point of 0.2 is added at position 0.5.
        /// </summary>
        public void ClearAlphaControlPoints()
        {
            m_AlphaControls.Clear();
            m_DirtyFlag = true;
            AddAlphaControlPoint(new(0.5f, 0.2f));
        }

        Texture2D m_ColorLookupTexture = null;

        /// <summary>
        /// Similar to TryUpdateColorLookupTexture. Except that it ignores the dirty flag and forces the
        /// regeneration of the internal transfer function. Use this function sparingly.
        /// </summary>
        public void ForceUpdateColorLookupTexture()
        {
            GenerateColorLookupTextureInternal();
            m_DirtyFlag = false;
            TransferFunctionTexChange?.Invoke(m_ColorLookupTexture);
        }

        /// <summary>
        /// Request an update for the internal transfer function. This checks a dirty flag then re-generates
        /// the texture if necessary. Intended workflow is to subscribe to TransferFunctionTexChange event
        /// to receive new color lookup textures and request textures updates by calling this function.
        /// </summary>
        public void TryUpdateColorLookupTexture()
        {
            if (m_ColorLookupTexture == null || m_DirtyFlag)
            {
                GenerateColorLookupTextureInternal();
                m_DirtyFlag = false;
                TransferFunctionTexChange?.Invoke(m_ColorLookupTexture);
                return;
            }
        }

        private void GenerateColorLookupTextureInternal()
        {
            // 2D texture setup
            const int textureWidth = 512;
            const int textureHeight = 1;

            if (m_ColorLookupTexture == null)
            {
                TextureFormat texFormat = SystemInfo.SupportsTextureFormat(TextureFormat.RGBAHalf)
                    ? TextureFormat.RGBAHalf
                    : TextureFormat.RGBAFloat;
                m_ColorLookupTexture = new Texture2D(textureWidth, textureHeight, texFormat, false);
                m_ColorLookupTexture.wrapMode = TextureWrapMode.Clamp;
            }
            if (m_ColorControls.Count == 0)
            {
                Debug.LogError("Color control points array is empty. Aborted TF generation.");
                return;
            }
            if (m_AlphaControls.Count == 0)
            {
                Debug.LogError("Alpha control points array is empty. Aborted TF generation.");
                return;
            }

            List<ControlPoint<float, Color>> sortedColors = new(m_ColorControls.Values);
            List<ControlPoint<float, float>> sortedAlphas = new(m_AlphaControls.Values);
            sortedColors.Sort((x, y) => x.Position.CompareTo(y.Position));
            sortedAlphas.Sort((x, y) => x.Position.CompareTo(y.Position));

            // add same color as first color at position 0 if no color is set at that position
            if (sortedColors[0].Position > 0)
            {
                var tmp = sortedColors[0].Value;
                sortedColors.Insert(0, new(0, tmp));
            }
            // add same color as last color at position 1 if no color is set at that position
            if (sortedColors[sortedColors.Count - 1].Position < 1)
            {
                var tmp = sortedColors[sortedColors.Count - 1].Value;
                sortedColors.Add(new(1, tmp));
            }
            // add same alpha as first alpha at position 0 if no alpha is set at that position
            if (sortedAlphas[0].Position > 0)
            {
                var tmp = sortedAlphas[0].Value;
                sortedAlphas.Insert(0, new(0, tmp));
            }
            // add same alpha as last alpha at position 1 if no alpha is set at that position
            if (sortedAlphas[sortedAlphas.Count - 1].Position < 1)
            {
                var tmp = sortedAlphas[sortedAlphas.Count - 1].Value;
                sortedAlphas.Add(new(1, tmp));
            }

            Color[] pixelColorData = new Color[textureWidth * textureHeight];
            int leftColorControlIndex = 0;
            int leftAlphaControlIndex = 0;
            int numOfColors = sortedColors.Count;
            int numOfAlphas = sortedAlphas.Count;

            // map texture width index to the range [0.0, 1.0]
            for (int textureIndex = 0; textureIndex < textureWidth; textureIndex++)
            {
                float currentDensity = textureIndex / (float)(textureWidth - 1);

                // find nearest left color control point to density
                while (
                    leftColorControlIndex < numOfColors - 2
                    && sortedColors[leftColorControlIndex + 1].Position < currentDensity
                )
                {
                    leftColorControlIndex++;
                }

                // find nearest left alpha control point to density
                while (
                    leftAlphaControlIndex < numOfAlphas - 2
                    && sortedAlphas[leftAlphaControlIndex + 1].Position < currentDensity
                )
                {
                    leftAlphaControlIndex++;
                }

                var leftColor = sortedColors[leftColorControlIndex];
                var rightColor = sortedColors[leftColorControlIndex + 1];
                var leftAlpha = sortedAlphas[leftAlphaControlIndex];
                var rightAlpha = sortedAlphas[leftAlphaControlIndex + 1];

                float tColor =
                    (currentDensity - leftColor.Position)
                    / (rightColor.Position - leftColor.Position);
                float tAlpha =
                    (currentDensity - leftAlpha.Position)
                    / (rightAlpha.Position - leftAlpha.Position);

                // color (without alpha) linear interpolation
                Color pixelColor = Color.Lerp(leftColor.Value, rightColor.Value, tColor);

                // alpha linear interpolation
                pixelColor.a = Mathf.Lerp(leftAlpha.Value, rightAlpha.Value, tAlpha);

                pixelColorData[textureIndex] =
                    QualitySettings.activeColorSpace == ColorSpace.Linear
                        ? pixelColor.linear
                        : pixelColor;
            }
            m_ColorLookupTexture.SetPixels(pixelColorData);
            m_ColorLookupTexture.Apply();
        }

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            foreach (var item in m_ColorControls.Values)
            {
                item.OnValueChange += () => m_DirtyFlag = true;
            }

            foreach (var item in m_AlphaControls.Values)
            {
                item.OnValueChange += () => m_DirtyFlag = true;
            }
        }
    }
}
