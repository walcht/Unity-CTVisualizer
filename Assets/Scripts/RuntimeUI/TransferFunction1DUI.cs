using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityCTVisualizer
{
    public class TransferFunction1DUI : MonoBehaviour
    {
        public RawImage m_HistogramImage;
        public RawImage m_GradientColorImage;

        public GameObject m_ColorControlPointUIPrefab;

        // cached components
        Texture2D m_CurrTF1DTex = null;

        List<ControlPoint<Color>> m_ColorControlPoints = new();
        List<ControlPoint<float>> m_AlphaControlPoints = new();

        void OnAwake()
        {
            m_ColorControlPoints.Add(
                new ControlPoint<Color>(0.5f, new Color(1.0f, 0.0f, 0.0f, 1.0f))
            );
            m_AlphaControlPoints.Add(new ControlPoint<float>(0.5f, 1.0f));
        }

        void OnEnable() { }

        void Start()
        {
            UpdateGradientColorTexture();
        }

        void Update() { }

        // TODO: optimize this by sending minimal information to the Shaderes
        void UpdateGradientColorTexture()
        {
            // regenerate color gradient texture and send apply it
            Texture2D TF1DTex = TransferFunction1D.GenerateColorLookupTexture(
                m_ColorControlPoints,
                m_AlphaControlPoints
            );
            m_GradientColorImage.material.SetTexture("_GradientColorTex", TF1DTex);
        }

        void OnColorControlPointUpdate()
        {
            this.UpdateGradientColorTexture();
        }

        void OnAlphaControlPointUpdate()
        {
            this.UpdateGradientColorTexture();
        }
    }
}
