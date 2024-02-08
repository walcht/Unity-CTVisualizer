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
        public event Action<Texture2D> TransferFunctionTexChange;

        [SerializeField]
        List<ControlPoint<float, Color>> m_ColorControls = new();

        [SerializeField]
        List<ControlPoint<float, float>> m_AlphaControls = new();

        bool m_DirtyFlag = true;

        public void Init()
        {
            // default setup
            AddColorControlPoint(new(0.0f, Color.white));
            AddColorControlPoint(new(1.0f, Color.black));
            AddAlphaControlPoint(new(0.0f, 0.0f));
            AddAlphaControlPoint(new(0.5f, 0.0f));
            AddAlphaControlPoint(new(0.6f, 0.7f));
            AddAlphaControlPoint(new(1.0f, 1.0f));
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////// GETTERS //////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////

        public int GetColorControlsCount() => m_ColorControls.Count;

        public int GetAlphaControlsCount() => m_AlphaControls.Count;

        public ControlPoint<float, Color> GetColorControlPointAt(int cpIndex) =>
            m_ColorControls[cpIndex];

        public ControlPoint<float, float> GetAlphaControlPointAt(int cpIndex) =>
            m_AlphaControls[cpIndex];

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
        /// Add a new color control point from which color (without alpha) will be interpolated.
        /// </summary>
        /// <remark>
        /// Even internally, it's preferable to use this function to add new control points rather than adding
        /// them directly to the ColorControlPoints list.
        /// </remark>
        /// <param name="colorCP">color control point data to be added to the ColorControlPoints list</param>
        /// <returns>index of newly added control point in the ColorControlPoints array</returns>
        public int AddColorControlPoint(ControlPoint<float, Color> colorCP)
        {
            colorCP.OnValueChange += () => m_DirtyFlag = true;
            m_ColorControls.Add(colorCP);
            m_DirtyFlag = true;
            return m_ColorControls.Count - 1;
        }

        public int AddAlphaControlPoint(ControlPoint<float, float> alphaCP)
        {
            alphaCP.OnValueChange += () => m_DirtyFlag = true;
            m_AlphaControls.Add(alphaCP);
            m_DirtyFlag = true;
            return m_AlphaControls.Count - 1;
        }

        public void ClearColorControlPoint(int colorIndex)
        {
            if (colorIndex < m_ColorControls.Count && colorIndex >= 0)
            {
                m_ColorControls.RemoveAt(colorIndex);
                m_DirtyFlag = true;
                return;
            }
            Debug.LogError($"Invalide color control point index: {colorIndex}");
        }

        public void ClearAlphaControlPoint(int alphaIndex)
        {
            if (alphaIndex < m_AlphaControls.Count && alphaIndex >= 0)
            {
                m_AlphaControls.RemoveAt(alphaIndex);
                m_DirtyFlag = true;
                return;
            }
            Debug.LogError($"Invalide alpha control point index: {alphaIndex}");
        }

        public void ClearColorControlPoints()
        {
            m_ColorControls.Clear();
            m_DirtyFlag = true;
        }

        public void ClearAlphaControlPoints()
        {
            m_AlphaControls.Clear();
            m_DirtyFlag = true;
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

            Color[] pixelColorData = new Color[textureWidth * textureHeight];
            List<ControlPoint<float, Color>> sortedColorControls = new(m_ColorControls);
            List<ControlPoint<float, float>> sortedAlphaControls = new(m_AlphaControls);
            sortedColorControls.Sort((x, y) => x.Position.CompareTo(y.Position));
            sortedAlphaControls.Sort((x, y) => x.Position.CompareTo(y.Position));
            if (sortedColorControls.Count == 0)
            {
                sortedColorControls.Add(new(1.0f, Color.white));
            }
            if (sortedAlphaControls.Count == 0)
            {
                sortedAlphaControls.Add(new(1.0f, 1.0f));
            }

            var lastColorControlPoint = sortedColorControls[sortedColorControls.Count - 1];
            if (lastColorControlPoint.Position < 1.0f)
            {
                sortedColorControls.Add(new(1.0f, lastColorControlPoint.Value));
            }
            var firstColorControlPoint = sortedColorControls[0];
            if (firstColorControlPoint.Position > 0.0f)
            {
                sortedColorControls.Insert(0, new(0.0f, firstColorControlPoint.Value));
            }
            var lastAlphaControlPoint = sortedAlphaControls[sortedAlphaControls.Count - 1];
            if (lastAlphaControlPoint.Position < 1.0f)
            {
                sortedAlphaControls.Add(new(1.0f, lastAlphaControlPoint.Value));
            }
            var firstAlphaControlPoint = sortedAlphaControls[0];
            if (firstAlphaControlPoint.Position > 0.0f)
            {
                sortedAlphaControls.Insert(0, new(0.0f, firstAlphaControlPoint.Value));
            }
            int leftColorControlIndex = 0;
            int leftAlphaControlIndex = 0;
            int numOfColors = sortedColorControls.Count;
            int numOfAlphas = sortedAlphaControls.Count;
            // 3. map texture width index to the range [0.0, 1.0]. We call that value t
            for (int textureIndex = 0; textureIndex < textureWidth; textureIndex++)
            {
                float currentDensity = textureIndex / (float)(textureWidth - 1);
                // find nearest left color control point to density
                while (
                    leftColorControlIndex < numOfColors - 2
                    && sortedColorControls[leftColorControlIndex + 1].Position < currentDensity
                )
                {
                    leftColorControlIndex++;
                }
                // find nearest left alpha control point to density
                while (
                    leftAlphaControlIndex < numOfColors - 2
                    && sortedAlphaControls[leftAlphaControlIndex + 1].Position < currentDensity
                )
                {
                    leftAlphaControlIndex++;
                }
                var leftColorControl = sortedColorControls[leftColorControlIndex];
                var rightColorControl = sortedColorControls[leftColorControlIndex + 1];
                var leftAlphaControl = sortedAlphaControls[leftAlphaControlIndex];
                var rightAlphaControl = sortedAlphaControls[leftAlphaControlIndex + 1];
                float tColor =
                    (currentDensity - leftColorControl.Position)
                    / (rightColorControl.Position - leftColorControl.Position);
                float tAlpha =
                    (currentDensity - leftAlphaControl.Position)
                    / (rightAlphaControl.Position - leftAlphaControl.Position);
                // normalizedDensityForColor = Mathf.SmoothStep(0.0f, 1.0f, normalizedDensityForColor);
                // normalizedDensityForAlpha = Mathf.SmoothStep(0.0f, 1.0f, normalizedDensityForAlpha);
                // linear interpolation
                Color pixelColor = Color.Lerp(
                    leftColorControl.Value,
                    rightColorControl.Value,
                    tColor
                );
                pixelColor.a = Mathf.Lerp(leftAlphaControl.Value, rightAlphaControl.Value, tAlpha);
                pixelColorData[textureIndex] =
                    QualitySettings.activeColorSpace == ColorSpace.Linear
                        ? pixelColor.linear
                        : pixelColor;
            }
            m_ColorLookupTexture.SetPixels(pixelColorData);
            m_ColorLookupTexture.Apply();
            Debug.Log("texutre creation done");
        }

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            foreach (var item in m_ColorControls)
            {
                item.OnValueChange += () => m_DirtyFlag = true;
            }

            foreach (var item in m_AlphaControls)
            {
                item.OnValueChange += () => m_DirtyFlag = true;
            }
        }
    }
}
