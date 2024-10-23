using System;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UnityCTVisualizer {
    public enum INTERPOLATION {
        NEAREST_NEIGHBOR = 0,
        TRILLINEAR,
        // TRILINEAR_POST_CLASSIFICATION, - yields worse performance and worse visuals
    }

    public enum MaxIterations {
        _128 = 0,
        _256,
        _512,
        _1024,
        _2048,
    }

    public class VisualizationParametersUI : MonoBehaviour {

        /////////////////////////////////
        // UI MODIFIERS
        /////////////////////////////////
        [SerializeField] TMP_Dropdown m_TFDropDown;
        [SerializeField] Slider m_AlphaCutoffSlider;
        [SerializeField] TMP_Dropdown m_InterpolationDropDown;
        [SerializeField] TMP_Dropdown m_MaxIterationsDropDown;

        /////////////////////////////////
        // CACHED COMPONENTS
        /////////////////////////////////
        int m_PrevTFIndex = -1;
        int m_PrevInterIndex = -1;
        int m_PrevMaxIterationsIndex = -1;


        private void Awake() {
            m_TFDropDown.options.Clear();
            foreach (string enumName in Enum.GetNames(typeof(TF))) {
                m_TFDropDown.options.Add(new TMP_Dropdown.OptionData(enumName));
            }
            m_TFDropDown.onValueChanged.AddListener(OnTFDropDownChange);
            m_InterpolationDropDown.options.Clear();
            foreach (string enumName in Enum.GetNames(typeof(INTERPOLATION))) {
                m_InterpolationDropDown.options.Add(new TMP_Dropdown.OptionData(enumName.Replace("_", " ").ToLower()));
            }
            m_MaxIterationsDropDown.options.Clear();
            foreach (string enumName in Enum.GetNames(typeof(MaxIterations))) {
                m_MaxIterationsDropDown.options.Add(new TMP_Dropdown.OptionData(enumName.Replace("_", "")));
            }
            m_InterpolationDropDown.onValueChanged.AddListener(OnInterDropDownChange);
            m_AlphaCutoffSlider.onValueChanged.AddListener(OnAlphaCutoffSliderChange);
            m_MaxIterationsDropDown.onValueChanged.AddListener(OnMaxIterationsDropDownChange);
        }

        private void OnEnable() {
            VisualizationParametersEvents.ModelTFChange += OnModelTFChange;
            VisualizationParametersEvents.ModelAlphaCutoffChange += OnModelAlphaCutoffChange;
            VisualizationParametersEvents.ModelMaxIterationsChange += OnModelMaxIterationsChange;
            VisualizationParametersEvents.ModelInterpolationChange += OnModelInterpolationChange;
        }

        private void OnDisable() {
            VisualizationParametersEvents.ModelTFChange -= OnModelTFChange;
            VisualizationParametersEvents.ModelAlphaCutoffChange -= OnModelAlphaCutoffChange;
            VisualizationParametersEvents.ModelMaxIterationsChange -= OnModelMaxIterationsChange;
            VisualizationParametersEvents.ModelInterpolationChange -= OnModelInterpolationChange;
        }

        /////////////////////////////////
        /// UI CALLBACKS (VIEW INVOKES)
        /////////////////////////////////
        private void OnTFDropDownChange(int tfIndex) {
            if (tfIndex != m_PrevTFIndex) {
                VisualizationParametersEvents.ViewTFChange?.Invoke((TF)tfIndex);
                m_PrevTFIndex = tfIndex;
            }
        }

        private void OnInterDropDownChange(int interIndex) {
            if (interIndex != m_PrevInterIndex) {
                m_PrevInterIndex = interIndex;
                VisualizationParametersEvents.ViewInterpolationChange?.Invoke((INTERPOLATION)interIndex);
            }
        }

        private void OnMaxIterationsDropDownChange(int maxIterationsIndex) {
            if (maxIterationsIndex != m_PrevMaxIterationsIndex) {
                m_PrevMaxIterationsIndex = maxIterationsIndex;
                VisualizationParametersEvents.ViewMaxIterationsChange?.Invoke(((MaxIterations)maxIterationsIndex));
            }
        }

        private void OnAlphaCutoffSliderChange(float newVal) {
            VisualizationParametersEvents.ViewAlphaCutoffChange?.Invoke(newVal);
        }


        /////////////////////////////////
        /// MODEL CALLBACKS
        /////////////////////////////////

        private void OnModelTFChange(TF new_tf, ITransferFunction _) {
            Debug.Log($"UI: {new_tf}");
            // do NOT set using value otherwise infinite event callbacks will occur!
            m_TFDropDown.SetValueWithoutNotify((int)new_tf);
        }


        private void OnModelAlphaCutoffChange(float value) {
            Debug.Log($"UI: {value}");
            // do NOT set using value otherwise infinite event callbacks will occur!
            m_AlphaCutoffSlider.SetValueWithoutNotify(value);
        }

        private void OnModelMaxIterationsChange(MaxIterations value) {
            Debug.Log($"UI: {value}");
            // do NOT set using value otherwise infinite event callbacks will occur!
            m_MaxIterationsDropDown.SetValueWithoutNotify((int)value);
        }

        private void OnModelInterpolationChange(INTERPOLATION value) {
            Debug.Log($"UI: {value}");
            // do NOT set using value otherwise infinite event callbacks will occur!
            m_InterpolationDropDown.SetValueWithoutNotify((int)value);
        }
    }
}
