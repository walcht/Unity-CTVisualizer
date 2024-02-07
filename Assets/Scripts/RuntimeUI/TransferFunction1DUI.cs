// #define DEBUG_UI
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityCTVisualizer
{
    [RequireComponent(typeof(RectTransform), typeof(Canvas))]
    public class TransferFunction1DUI : MonoBehaviour
    {
#if !DEBUG_UI
        [HideInInspector]
#endif
        public VolumetricDataset m_VolumetricDataset;

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////// IN-CURRENT or IN-CHILDREN REFERENCES //////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////

        [SerializeField]
        RawImage m_HistogramImage;

        [SerializeField]
        RectTransform m_ColorGradientControlRange;

        [SerializeField]
        RawImage m_GradientColorImage;

        [SerializeField]
        Button m_ClearColors;

        [SerializeField]
        Button m_ClearAlphas;

        [SerializeField]
        ColorPickerButton m_ColorPicker;

        [SerializeField]
        ColorPickerWrapper m_ColorPickerWrapper;

        [SerializeField]
        RectTransform m_TF1DCanvasTransform;

        [SerializeField]
        RectTransform m_ColorPickerTransform;

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////// PREFABS /////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////

        public GameObject m_ColorControlPointUIPrefab;

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////// MISC ///////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////

        public Vector3 m_RelativeColorPickerPos;
        public Quaternion m_RelativeColorPickerRot;

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////// CACHED COMPONENTS ////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////

        int MAIN_TEX_SHADER_ID;

        // -1 means None is selected which in turn means that we have no control points (empty list)
        int m_CurrControlPointID = -1;

#if DEBUG_UI
        [SerializeField]
#endif
        TransferFunction1D m_TransferFunctionData = null;

#if DEBUG_UI
        [SerializeField]
#endif
        List<ColorControlPointUI> m_ColorControlPoints = new();

        void Awake()
        {
            m_ClearColors.onClick.AddListener(OnClearColorsClick);
            m_ColorPicker.OnClick += OnColorPickerClick;
            m_ColorPickerWrapper.gameObject.SetActive(false);

            MAIN_TEX_SHADER_ID = Shader.PropertyToID("_MainTex");
        }

#if DEBUG_UI
        void Start()
        {
            Init(m_TransferFunctionData);
        }
#endif

        public void Init(TransferFunction1D transferFunctionData)
        {
            m_TransferFunctionData = transferFunctionData;
            m_ColorControlPoints.Clear();
            // construct initial control points UI arrays
            for (int i = 0; i < m_TransferFunctionData.GetColorControlsCount(); i++)
            {
                AddColorControlPointInternal(m_TransferFunctionData.GetColorControlPointAt(i));
            }
            // select first element by default
            if (m_ColorControlPoints.Count > 0)
            {
                UpdateCurrControlPointID(0);
            }
            else
            {
                UpdateCurrControlPointID(-1);
            }
            m_TransferFunctionData.TransferFunctionTexChange += OnTFTexChange;
            m_TransferFunctionData.TryUpdateColorLookupTexture();
        }

        void AddColorControlPoint(ControlPoint<float, Color> cp)
        {
            AddColorControlPointInternal(cp);
            m_TransferFunctionData.AddColorControlPoint(cp);
        }

        void AddColorControlPointInternal(ControlPoint<float, Color> cp)
        {
            var newCp = Instantiate(
                    m_ColorControlPointUIPrefab,
                    parent: m_ColorGradientControlRange
                )
                .GetComponent<ColorControlPointUI>();
            newCp.Init(m_ColorControlPoints.Count, cp);
            newCp.ControlPointSelected += OnControlPointSelect;
            newCp.ControlPointData.OnValueChange += OnControlPointDataChange;
            m_ColorControlPoints.Add(newCp);
        }

        void RemoveControlPoint(int cpID)
        {
            var cpToRemove = m_ColorControlPoints[cpID];
            cpToRemove.ControlPointSelected -= OnControlPointSelect;
            cpToRemove.ControlPointData.OnValueChange -= OnControlPointDataChange;
            m_ColorControlPoints.RemoveAt(cpID);
            m_TransferFunctionData.ClearColorControlPoint(cpID);
        }

        void UpdateCurrControlPointID(int newId)
        {
            if (newId < 0 || newId >= m_ColorControlPoints.Count)
            {
                m_CurrControlPointID = -1;
                m_ColorPicker.SetInteractiveness(false);
                return;
            }
            m_CurrControlPointID = newId;
            m_ColorPicker.SetColor(m_ColorControlPoints[newId].ControlPointData.Value);
            m_ColorPicker.SetInteractiveness(true);
        }

        void OnEnable()
        {
            if (m_TransferFunctionData == null)
            {
#if DEBUG_UI
                Debug.LogError(
                    "Transfer Function data should be supplied (can be done through editor)."
                );
#else
                Debug.LogError("Init should be appropriately called before enabling this UI.");
#endif
                return;
            }
        }

        void OnDisable()
        {
            m_TransferFunctionData.TransferFunctionTexChange -= OnTFTexChange;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////// LISTENERS /////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////

        void OnClearColorsClick()
        {
            m_TransferFunctionData.ClearColorControlPoints();
            foreach (var item in m_ColorControlPoints)
            {
                item.gameObject.SetActive(false);
                Destroy(item);
            }
            m_ColorControlPoints.Clear();
#if DEBUG_UI
            Debug.Log("Removed all color control points");
#endif
            m_TransferFunctionData.TryUpdateColorLookupTexture();
        }

        void OnColorPickerClick()
        {
            if (m_CurrControlPointID < 0)
            {
                Debug.LogWarning(
                    "Color picker button is active although no ColorControl is selected!"
                );
                return;
            }
            m_ColorPickerWrapper.ColorPickerDone += OnColorPickerDoneClick;

            m_ColorPickerTransform.position = m_TF1DCanvasTransform.TransformPoint(
                m_RelativeColorPickerPos
            );
            m_ColorPickerTransform.rotation =
                m_TF1DCanvasTransform.rotation * m_RelativeColorPickerRot;
            // don't forget to set the ColorPicker's initial color
            m_ColorPickerWrapper.Init(
                m_ColorControlPoints[m_CurrControlPointID].ControlPointData.Value
            );
            m_ColorPickerWrapper.gameObject.SetActive(true);
        }

        void OnColorPickerDoneClick(Color finalColor)
        {
            m_ColorPickerWrapper.ColorPickerDone -= OnColorPickerDoneClick;

            m_ColorControlPoints[m_CurrControlPointID].SetColor(finalColor);
            m_ColorPicker.SetColor(finalColor);

            m_ColorPickerWrapper.gameObject.SetActive(false);
        }

        void OnControlPointSelect(int cpID)
        {
            UpdateCurrControlPointID(cpID);
#if DEBUG_UI
            Debug.Log($"Current control point ID: {m_CurrControlPointID}");
#endif
        }

        // expensive method!
        void OnControlPointDataChange()
        {
            Debug.Log("Control point updated!");
            m_TransferFunctionData.TryUpdateColorLookupTexture();
        }

        void OnTFTexChange(Texture2D newTex)
        {
            m_GradientColorImage.texture = newTex;
        }
    }
}
