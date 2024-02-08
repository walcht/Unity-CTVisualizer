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

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////// IN-CURRENT or IN-CHILDREN REFERENCES //////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////

        [SerializeField]
        RawImage m_HistogramImage;

        [SerializeField]
        RectTransform m_HistogramTransform;

        [SerializeField]
        RectTransform m_ColorGradientControlRange;

        [SerializeField]
        RawImage m_GradientColorImage;

        [SerializeField]
        Button m_RemoveAlpha;

        [SerializeField]
        Button m_RemoveColor;

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

        [Tooltip(
            "Color control point UI prefab that will be instantiated by this TF UI in the color gradient "
                + "range UI once a new color control point is added."
        )]
        public GameObject m_ColorControlPointUIPrefab;

        [Tooltip(
            "Alpha control point UI prefab that will be instantiated by this TF UI in the histogram child UI"
                + " once a new color control point is added."
        )]
        public GameObject m_AlphaControlPointUIPrefab;

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////// CACHED COMPONENTS ////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////

        int MAIN_TEX_SHADER_ID;

        VolumetricDataset m_VolumetricDataset = null;

        // -1 means None is selected which in turn means that we have no control points (empty list)
        int m_CurrColorControlPointID = -1;
        int m_CurrAlphaControlPointID = -1;

#if DEBUG_UI
        [SerializeField]
#endif
        TransferFunction1D m_TransferFunctionData = null;

#if DEBUG_UI
        [SerializeField]
#endif
        List<ColorControlPointUI> m_ColorControlPoints = new();
        List<AlphaControlPointUI> m_AlphaControlPoints = new();

        void Awake()
        {
            m_RemoveAlpha.onClick.AddListener(OnRemoveAlphaControlPoint);
            m_RemoveColor.onClick.AddListener(OnRemoveColorControlPoint);
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

        public void Init(
            TransferFunction1D transferFunctionData,
            VolumetricDataset volumetricDataset
        )
        {
            m_TransferFunctionData = transferFunctionData;
            m_VolumetricDataset = volumetricDataset;
            m_ColorControlPoints.Clear();
            // construct initial control points UI arrays
            for (int i = 0; i < m_TransferFunctionData.GetColorControlsCount(); i++)
            {
                AddColorControlPointInternal(m_TransferFunctionData.GetColorControlPointAt(i));
            }
            for (int i = 0; i < m_TransferFunctionData.GetAlphaControlsCount(); i++)
            {
                AddAlphaControlPointInternal(m_TransferFunctionData.GetAlphaControlPointAt(i));
            }
            // select first element by default
            if (m_ColorControlPoints.Count > 0)
            {
                UpdateCurrColorControlPointID(0);
            }
            else
            {
                UpdateCurrColorControlPointID(-1);
            }
            m_TransferFunctionData.TransferFunctionTexChange += OnTFTexChange;
            m_TransferFunctionData.TryUpdateColorLookupTexture();
            // subscribe to density frequencies change (for histogram texture)
            m_VolumetricDataset.OnDensitiesFreqChange += OnDensitiesFreqChange;
            // request histogram texture
            m_VolumetricDataset.TryUpdateDensityFreqTexture();
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
            newCp.ControlPointSelected += OnColorControlPointSelect;
            newCp.ControlPointData.OnValueChange += OnControlPointDataChange;
            m_ColorControlPoints.Add(newCp);
        }

        void AddAlphaControlPointInternal(ControlPoint<float, float> cp)
        {
            var newCp = Instantiate(m_AlphaControlPointUIPrefab, parent: m_HistogramTransform)
                .GetComponent<AlphaControlPointUI>();
            newCp.Init(m_AlphaControlPoints.Count, cp);
            newCp.ControlPointSelected += OnAlphaControlPointSelect;
            newCp.ControlPointData.OnValueChange += OnControlPointDataChange;
            m_AlphaControlPoints.Add(newCp);
        }

        void UpdateCurrColorControlPointID(int newId)
        {
            if (newId < 0 || newId >= m_ColorControlPoints.Count)
            {
                m_CurrColorControlPointID = -1;
                m_RemoveColor.interactable = false;
                m_ColorPicker.SetInteractiveness(false);
                return;
            }
            m_CurrColorControlPointID = newId;
            m_ColorPicker.SetColor(m_ColorControlPoints[newId].ControlPointData.Value);
            m_RemoveColor.interactable = true;
            m_ColorPicker.SetInteractiveness(true);
        }

        void UpdateCurrAlphaControlPointID(int newId)
        {
            if (newId < 0 || newId >= m_AlphaControlPoints.Count)
            {
                m_CurrAlphaControlPointID = -1;
                m_RemoveAlpha.interactable = false;
                return;
            }
            m_CurrAlphaControlPointID = newId;
            m_RemoveAlpha.interactable = true;
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
            m_VolumetricDataset.OnDensitiesFreqChange -= OnDensitiesFreqChange;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////// LISTENERS /////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////

        void OnRemoveAlphaControlPoint()
        {
            if (m_CurrAlphaControlPointID == -1)
            {
                Debug.LogError(
                    "No alpha control point is currently selected."
                        + " This alpha control point remove handler should not have been active!"
                );
                return;
            }
            var cpToRemove = m_AlphaControlPoints[m_CurrAlphaControlPointID];
            // remove it from alpha control points UI list
            m_AlphaControlPoints.RemoveAt(m_CurrAlphaControlPointID);
            // destroy UI element (no need to unsubscribe)
            Destroy(cpToRemove.gameObject);
            // remove alpha from underlying Transfer function data
            m_TransferFunctionData.ClearAlphaControlPoint(m_CurrAlphaControlPointID);
            // no alpha control point is currently selected
            UpdateCurrAlphaControlPointID(-1);
            // request an update for TF
            m_TransferFunctionData.TryUpdateColorLookupTexture();
        }

        void OnRemoveColorControlPoint()
        {
            if (m_CurrColorControlPointID == -1)
            {
                Debug.LogError(
                    "No color control point is currently selected."
                        + " This color control point remove handler should not have been active!"
                );
                return;
            }
            var cpToRemove = m_ColorControlPoints[m_CurrColorControlPointID];
            // remove it from color control points UI list
            m_ColorControlPoints.RemoveAt(m_CurrColorControlPointID);
            // destroy UI element (no need to unsubscribe)
            Destroy(cpToRemove.gameObject);
            // remove from underlying Transfer function data
            m_TransferFunctionData.ClearColorControlPoint(m_CurrColorControlPointID);
            // no color control point is currently selected
            UpdateCurrColorControlPointID(-1);
            // request an update for TF
            m_TransferFunctionData.TryUpdateColorLookupTexture();
        }

        void OnClearColorsClick()
        {
            m_TransferFunctionData.ClearColorControlPoints();
            foreach (var item in m_ColorControlPoints)
            {
                item.gameObject.SetActive(false);
                Destroy(item.gameObject);
            }
            m_ColorControlPoints.Clear();
#if DEBUG_UI
            Debug.Log("Removed all color control points");
#endif
            m_TransferFunctionData.TryUpdateColorLookupTexture();
        }

        void OnColorPickerClick()
        {
            if (m_CurrColorControlPointID < 0)
            {
                Debug.LogWarning(
                    "Color picker button is active although no ColorControl is selected!"
                );
                return;
            }
            m_ColorPickerWrapper.ColorPickerDone += OnColorPickerDoneClick;
            // don't forget to set the ColorPicker's initial color
            m_ColorPickerWrapper.Init(
                m_ColorControlPoints[m_CurrColorControlPointID].ControlPointData.Value
            );
            // activate whole color picker Canvas UI
            m_ColorPickerWrapper.gameObject.SetActive(true);
            // TODO: disable TransferFunction1DUI interactiveness
        }

        void OnColorPickerDoneClick(Color finalColor)
        {
            m_ColorPickerWrapper.ColorPickerDone -= OnColorPickerDoneClick;
            // update currently selected color control point UI's color. This will automatically update
            // underlying transfer function data
            m_ColorControlPoints[m_CurrColorControlPointID].SetColor(finalColor);
            // set the UI color for the color picker button
            m_ColorPicker.SetColor(finalColor);
            // disable whole color picker Canvas UI
            m_ColorPickerWrapper.gameObject.SetActive(false);
            // TODO: enable TransferFunction1DUI interactiveness
            // don't forget to request a TF texture update
            m_TransferFunctionData.TryUpdateColorLookupTexture();
        }

        void OnColorControlPointSelect(int cpID)
        {
            UpdateCurrColorControlPointID(cpID);
#if DEBUG_UI
            Debug.Log($"Current color control point ID: {m_CurrColorControlPointID}");
#endif
        }

        void OnAlphaControlPointSelect(int cpID)
        {
            UpdateCurrAlphaControlPointID(cpID);
#if DEBUG_UI
            Debug.Log($"Current alpha control point ID: {m_CurrAlphaControlPointID}");
#endif
        }

        // Called whenever underlying data for alpha or color control points change. Expensive method!
        void OnControlPointDataChange()
        {
#if DEBUG_UI
            Debug.Log("Alpha or color control point updated!");
#endif
            m_TransferFunctionData.TryUpdateColorLookupTexture();
        }

        void OnTFTexChange(Texture2D newTex)
        {
            m_GradientColorImage.texture = newTex;
        }

        void OnDensitiesFreqChange(Texture2D newDensityFreq)
        {
            m_HistogramImage.texture = newDensityFreq;
        }
    }
}
