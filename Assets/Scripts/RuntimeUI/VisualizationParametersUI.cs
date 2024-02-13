#define DEBUG_UI
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UnityCTVisualizer
{
    public enum INTERPOLATION
    {
        NEAREST_NEIGHBOR = 0,
        TRILINEAR_PRE_CLASSIFICATION,
        TRILINEAR_POST_CLASSIFICATION,
    }

    public class VisualizationParametersUI : MonoBehaviour
    {
        public event Action<TF> OnTransferFunctionChange;
        public event Action<INTERPOLATION> OnInterpolationChange;

        [SerializeField]
        TMP_Dropdown m_TFDropDown;

        [SerializeField]
        ClampUI m_ClampMin;

        [SerializeField]
        ClampUI m_ClampMax;

        [SerializeField]
        Slider m_AlphaCutoff;

        [SerializeField]
        TMP_Dropdown m_InterpolationDropDown;

        VolumetricDataset m_VolumetricDataset = null;
        VolumetricObject m_VoumetricObject = null;

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////// CACHED COMPONENTS ////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////

        // previously selected transfer function
        int m_PrevTFIndex = -1;
        int m_PrevInterIndex = -1;

        void Awake()
        {
            m_TFDropDown.options.Clear();
            foreach (string enumName in Enum.GetNames(typeof(TF)))
            {
                m_TFDropDown.options.Add(new TMP_Dropdown.OptionData(enumName));
            }
            m_TFDropDown.onValueChanged.AddListener(OnTFDropDownChange);
            m_InterpolationDropDown.options.Clear();
            foreach (string enumName in Enum.GetNames(typeof(INTERPOLATION)))
            {
                m_InterpolationDropDown.options.Add(
                    new TMP_Dropdown.OptionData(enumName.Replace("_", " ").ToLower())
                );
            }
            m_InterpolationDropDown.onValueChanged.AddListener(OnInterDropDownChange);
            m_AlphaCutoff.onValueChanged.AddListener(OnAlphaCutoffChange);
        }

        /// <summary>
        /// Initializes the VolumetricDataset ScriptableObject this UI affects.
        /// Call this exactly after initializing this component.
        /// </summary>
        /// <param name="volumetricDataset">Instance of VolumetricDataset ScriptableObject that this UI will affect its </param>
        /// <param name="volumetricObject">The volumetric object that this UI will affect its rendering parameters</param>
        public void Init(VolumetricDataset volumetricDataset, VolumetricObject volumetricObject)
        {
            m_VolumetricDataset = volumetricDataset;
            m_VoumetricObject = volumetricObject;
            m_AlphaCutoff.value = m_VoumetricObject.AlphaCutoff;
        }

        public void SetTFDropDown(TF tf)
        {
            m_PrevTFIndex = m_TFDropDown.value;
            m_TFDropDown.value = (int)tf;
        }

        void OnTFDropDownChange(int tfIndex)
        {
            if (tfIndex != m_PrevTFIndex)
            {
                OnTransferFunctionChange?.Invoke((TF)tfIndex);
                m_PrevTFIndex = tfIndex;
            }
        }

        void OnInterDropDownChange(int interIndex)
        {
            if (interIndex != m_PrevInterIndex)
            {
                m_PrevInterIndex = interIndex;
                OnInterpolationChange?.Invoke((INTERPOLATION)interIndex);
            }
        }

        void OnAlphaCutoffChange(float newVal)
        {
#if DEBUG_UI
            Debug.Log($"Alpha cutoff change: {newVal}");
#endif
            m_VoumetricObject.AlphaCutoff = newVal;
        }
    }
}
