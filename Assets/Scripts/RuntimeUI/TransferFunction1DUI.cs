using System.Collections;
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
        Material m_HistogramImageMat;
        Material m_GradientColorImageMat;

        void OnEnable() { }

        void Start() { }

        void Update() { }

        public void OnColorControlPointUpdate() { }
    }
}
