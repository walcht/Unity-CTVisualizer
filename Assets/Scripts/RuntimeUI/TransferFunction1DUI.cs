#define DEBUG_UI
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace UnityCTVisualizer {
    [RequireComponent(typeof(RectTransform), typeof(Canvas))]
    public class TransferFunction1DUI : MonoBehaviour {

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////// IN-CURRENT or IN-CHILDREN REFERENCES //////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////

        [SerializeField] RawImage m_HistogramImage;
        [SerializeField] RectTransform m_HistogramTransform;
        [SerializeField] HistogramUI m_HistogramUI;
        [SerializeField] RectTransform m_ColorGradientControlRange;
        [SerializeField] RawImage m_GradientColorImage;
        [SerializeField] ColorGradientRangeUI m_GradientColorUI;
        [SerializeField] Button m_RemoveAlpha;
        [SerializeField] Button m_RemoveColor;
        [SerializeField] Button m_ClearColors;
        [SerializeField] Button m_ClearAlphas;
        [SerializeField] ColorPickerButton m_ColorPicker;
        [SerializeField] ColorPickerWrapper m_ColorPickerWrapper;
        [SerializeField] RectTransform m_TF1DCanvasTransform;
        [SerializeField] RectTransform m_ColorPickerTransform;

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

        int MAIN_TEX_SHADER_ID = Shader.PropertyToID("_MainTex");
        int ALPHA_COUNT_SHADER_ID = Shader.PropertyToID("_AlphaCount");
        int ALPHA_POSITIONS_SHADER_ID = Shader.PropertyToID("_AlphaPositions");
        int ALPHA_VALUES_SHADER_ID = Shader.PropertyToID("_AlphaValues");

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
        Dictionary<int, ColorControlPointUI> m_ColorControlPoints = new();
        Dictionary<int, AlphaControlPointUI> m_AlphaControlPoints = new();

        void Awake() {
            m_RemoveAlpha.onClick.AddListener(OnRemoveAlphaControlPoint);
            m_RemoveColor.onClick.AddListener(OnRemoveColorControlPoint);
            m_ClearColors.onClick.AddListener(OnClearColorsClick);
            m_ClearAlphas.onClick.AddListener(OnClearAlphasClick);
            m_ColorPicker.OnClick += OnColorPickerClick;
            m_HistogramUI.OnAddAlphaControlPoint += OnAddAlphaControlPoint;
            m_GradientColorUI.OnAddColorControlPoint += OnAddColorControlPoint;
            m_ColorPickerWrapper.gameObject.SetActive(false);
        }

        public void Init(TransferFunction1D transferFunctionData) {
            m_TransferFunctionData = transferFunctionData;
            m_ColorControlPoints.Clear();
            // synchronize control points UI array with underlying transfer function data
            foreach (var colorCpID in m_TransferFunctionData.ColorControlPointIDs()) {
                AddColorControlPointInternal(
                    m_TransferFunctionData.GetColorControlPointAt(colorCpID),
                    colorCpID
                );
            }
            foreach (var alphaCpID in m_TransferFunctionData.AlphaControlPointIDs()) {
                AddAlphaControlPointInternal(
                    m_TransferFunctionData.GetAlphaControlPointAt(alphaCpID),
                    alphaCpID
                );
            }
            // select first element by default
            if (m_ColorControlPoints.Count > 0)
                UpdateCurrColorControlPointID(m_ColorControlPoints.Keys.First());
            else
                UpdateCurrColorControlPointID(-1);
            if (m_AlphaControlPoints.Count > 0)
                UpdateCurrAlphaControlPointID(m_AlphaControlPoints.Keys.First());
            else
                UpdateCurrAlphaControlPointID(-1);
            m_TransferFunctionData.TFColorsLookupTexChange += OnTFTexChange;
            m_TransferFunctionData.TryUpdateColorLookupTexture();
            // initialize histogram shader properties and request histogram texture
            OnAlphaControlPointDataChange();
            // m_VolumetricDataset.TryUpdateDensityFreqTexture();
        }

        void AddColorControlPoint(ControlPoint<float, Color> cp) {
            int newCpID = m_TransferFunctionData.AddColorControlPoint(cp);
            AddColorControlPointInternal(cp, newCpID);
        }

        void AddColorControlPointInternal(ControlPoint<float, Color> cp, int cpID) {
            var newCp = Instantiate(
                    m_ColorControlPointUIPrefab,
                    parent: m_ColorGradientControlRange
                )
                .GetComponent<ColorControlPointUI>();
            newCp.Init(cpID, cp);
            newCp.ControlPointSelected += OnColorControlPointSelect;
            newCp.ControlPointData.OnValueChange += OnColorControlPointDataChange;
            m_ColorControlPoints.Add(cpID, newCp);
        }

        void AddAlphaControlPoint(ControlPoint<float, float> cp) {
            int newCpID = m_TransferFunctionData.AddAlphaControlPoint(cp);
            AddAlphaControlPointInternal(cp, newCpID);
        }

        void AddAlphaControlPointInternal(ControlPoint<float, float> cp, int cpID) {
            var newCp = Instantiate(m_AlphaControlPointUIPrefab, parent: m_HistogramTransform)
                .GetComponent<AlphaControlPointUI>();
            newCp.Init(cpID, cp);
            newCp.ControlPointSelected += OnAlphaControlPointSelect;
            newCp.ControlPointData.OnValueChange += OnAlphaControlPointDataChange;
            m_AlphaControlPoints.Add(cpID, newCp);
        }

        void UpdateCurrColorControlPointID(int newId) {
            if (newId < 0) {
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

        void UpdateCurrAlphaControlPointID(int newId) {
            if (newId < 0) {
                m_CurrAlphaControlPointID = -1;
                m_RemoveAlpha.interactable = false;
                return;
            }
            m_CurrAlphaControlPointID = newId;
            m_RemoveAlpha.interactable = true;
        }

        void OnEnable() {
            if (m_TransferFunctionData == null) {
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

        void OnDisable() {
            m_TransferFunctionData.TFColorsLookupTexChange -= OnTFTexChange;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////// LISTENERS /////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////

        void OnRemoveAlphaControlPoint() {
            if (m_CurrAlphaControlPointID == -1) {
                Debug.LogError(
                    "No alpha control point is currently selected."
                        + " This alpha control point remove handler should not have been active!"
                );
                return;
            }
            var cpToRemove = m_AlphaControlPoints[m_CurrAlphaControlPointID];
            // remove it from alpha control points UI list
            m_AlphaControlPoints.Remove(m_CurrAlphaControlPointID);
            // destroy UI element (no need to unsubscribe)
            Destroy(cpToRemove.gameObject);
            if (m_AlphaControlPoints.Count == 0) {
                m_TransferFunctionData.ClearAlphaControlPoints();
                // ClearAlphaControlPoints adds new default alpha control point(s). We have to synchronize
                foreach (var alphaCpID in m_TransferFunctionData.AlphaControlPointIDs()) {
                    AddAlphaControlPointInternal(
                        m_TransferFunctionData.GetAlphaControlPointAt(alphaCpID),
                        alphaCpID
                    );
                }
            } else {
                // remove alpha from underlying Transfer function data
                m_TransferFunctionData.RemoveAlphaControlPoint(m_CurrAlphaControlPointID);
            }
            // no alpha control point is currently selected
            UpdateCurrAlphaControlPointID(-1);
            // don't forget to request a transfer function texture update
            OnAlphaControlPointDataChange();
        }

        void OnRemoveColorControlPoint() {
            if (m_CurrColorControlPointID == -1) {
                Debug.LogError(
                    "No color control point is currently selected."
                        + " This color control point remove handler should not have been active!"
                );
                return;
            }
            var cpToRemove = m_ColorControlPoints[m_CurrColorControlPointID];
            // remove it from color control points UI list
            m_ColorControlPoints.Remove(m_CurrColorControlPointID);
            // destroy UI element (no need to unsubscribe)
            Destroy(cpToRemove.gameObject);
            if (m_ColorControlPoints.Count == 0) {
                m_TransferFunctionData.ClearColorControlPoints();
                // ClearColorControlPoints adds new default color control point(s). We have to synchronize
                foreach (var colorCpID in m_TransferFunctionData.ColorControlPointIDs()) {
                    AddColorControlPointInternal(
                        m_TransferFunctionData.GetColorControlPointAt(colorCpID),
                        colorCpID
                    );
                }
            } else {
                // remove from underlying Transfer function data
                m_TransferFunctionData.RemoveColorControlPoint(m_CurrColorControlPointID);
            }
            // no color control point is currently selected
            UpdateCurrColorControlPointID(-1);
            // don't forget to request a transfer function texture update
            m_TransferFunctionData.TryUpdateColorLookupTexture();
        }

        void OnClearColorsClick() {
            m_TransferFunctionData.ClearColorControlPoints();
            foreach (var item in m_ColorControlPoints.Values) {
                item.gameObject.SetActive(false);
                Destroy(item.gameObject);
            }
            m_ColorControlPoints.Clear();
#if DEBUG_UI
            Debug.Log("Removed all color control points");
#endif
            // ClearColorControlPoints adds new default color control point(s). We have to synchronize
            foreach (var colorCpID in m_TransferFunctionData.ColorControlPointIDs()) {
                AddColorControlPointInternal(
                    m_TransferFunctionData.GetColorControlPointAt(colorCpID),
                    colorCpID
                );
            }
            // no color control point is currently selected
            UpdateCurrColorControlPointID(-1);
            // don't forget to request a transfer function texture update
            m_TransferFunctionData.TryUpdateColorLookupTexture();
        }

        void OnClearAlphasClick() {
            m_TransferFunctionData.ClearAlphaControlPoints();
            foreach (var item in m_AlphaControlPoints.Values) {
                item.gameObject.SetActive(false);
                Destroy(item.gameObject);
            }
            m_AlphaControlPoints.Clear();
#if DEBUG_UI
            Debug.Log("Removed all alpha control points");
#endif
            // ClearAlphaControlPoints adds new default alpha control point(s). We have to synchronize
            foreach (var alphaCpID in m_TransferFunctionData.AlphaControlPointIDs()) {
                AddAlphaControlPointInternal(
                    m_TransferFunctionData.GetAlphaControlPointAt(alphaCpID),
                    alphaCpID
                );
            }
            // no alpha control point is currently selected
            UpdateCurrAlphaControlPointID(-1);
            // don't forget to request a transfer function texture update
            OnAlphaControlPointDataChange();
        }

        void OnColorPickerClick() {
            if (m_CurrColorControlPointID < 0) {
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

        void OnColorPickerDoneClick(Color finalColor) {
            m_ColorPickerWrapper.ColorPickerDone -= OnColorPickerDoneClick;
            // update currently selected color control point UI's color. This will automatically update
            // underlying transfer function data
            m_ColorControlPoints[m_CurrColorControlPointID].SetColor(finalColor);
            // set the UI color for the color picker button
            m_ColorPicker.SetColor(finalColor);
            // disable whole color picker Canvas UI
            m_ColorPickerWrapper.gameObject.SetActive(false);
            // TODO: enable TransferFunction1DUI interactiveness
            // don't forget to request a transfer function texture update
            m_TransferFunctionData.TryUpdateColorLookupTexture();
        }

        void OnColorControlPointSelect(int cpID) {
            UpdateCurrColorControlPointID(cpID);
#if DEBUG_UI
            Debug.Log($"Current color control point ID: {m_CurrColorControlPointID}");
#endif
        }

        void OnAlphaControlPointSelect(int cpID) {
            UpdateCurrAlphaControlPointID(cpID);
#if DEBUG_UI
            Debug.Log($"Current alpha control point ID: {m_CurrAlphaControlPointID}");
            Debug.Log(
                "Currently selected alpha point: Position="
                    + $"{m_TransferFunctionData.GetAlphaControlPointAt(m_CurrAlphaControlPointID).Position}"
                    + " Value="
                    + $"{m_TransferFunctionData.GetAlphaControlPointAt(m_CurrAlphaControlPointID).Value}"
            );
#endif
        }

        // Called whenever underlying data for any alpha control point change. Expensive method!
        void OnAlphaControlPointDataChange() {
            // update histogram shader
            List<ControlPoint<float, float>> tmp = new();

            foreach (var alphaCpID in m_TransferFunctionData.AlphaControlPointIDs()) {
                tmp.Add(m_TransferFunctionData.GetAlphaControlPointAt(alphaCpID));
            }
            tmp.Sort((x, y) => x.Position.CompareTo(y.Position));
            if (tmp[0].Position > 0) {
                var firstVal = tmp[0].Value;
                tmp.Insert(0, new(0, firstVal));
            }
            if (tmp[tmp.Count - 1].Position < 1) {
                var lastVal = tmp[tmp.Count - 1].Value;
                tmp.Add(new(1, lastVal));
            }

            float[] alphaPositions = new float[TFConstants.MAX_ALPHA_CONTROL_POINTS];
            float[] alphaValues = new float[TFConstants.MAX_ALPHA_CONTROL_POINTS];
            for (int i = 0; i < tmp.Count; ++i) {
                alphaPositions[i] = tmp[i].Position;
                alphaValues[i] = tmp[i].Value;
            }

            m_HistogramImage.material.SetInteger(ALPHA_COUNT_SHADER_ID, tmp.Count);
            m_HistogramImage.material.SetFloatArray(ALPHA_POSITIONS_SHADER_ID, alphaPositions);
            m_HistogramImage.material.SetFloatArray(ALPHA_VALUES_SHADER_ID, alphaValues);
            m_TransferFunctionData.TryUpdateColorLookupTexture();
        }

        // Called whenever underlying data for any color control point change. Expensive method!
        void OnColorControlPointDataChange() {
            m_TransferFunctionData.TryUpdateColorLookupTexture();
        }

        void OnTFTexChange(Texture2D newTex) {
            m_GradientColorImage.texture = newTex;
        }

        void OnDensitiesFreqChange(Texture2D newDensityFreq) {
            m_HistogramImage.texture = newDensityFreq;
        }

        void OnAddAlphaControlPoint(Vector2 histogramPos) {
            // reason for the -2 is for the extreme points at position 0 and 1 respectively.
            if (m_AlphaControlPoints.Count < TFConstants.MAX_ALPHA_CONTROL_POINTS - 2) {
                AddAlphaControlPoint(
                    new ControlPoint<float, float>(histogramPos.x, histogramPos.y)
                );
                OnAlphaControlPointDataChange();
            }
        }

        void OnAddColorControlPoint(float xPos) {
            if (m_ColorControlPoints.Count < TFConstants.MAX_COLOR_CONTROL_POINTS - 2) {
                AddColorControlPoint(new ControlPoint<float, Color>(xPos, Color.white));
                m_TransferFunctionData.TryUpdateColorLookupTexture();
            }
        }
    }
}
