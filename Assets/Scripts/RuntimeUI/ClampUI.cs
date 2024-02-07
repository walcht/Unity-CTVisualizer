using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UnityCTVisualizer
{
    public class ClampUI : MonoBehaviour
    {
        /// <summary>
        /// Invoked when toggle is set/unset
        /// </summary>
        public event Action<bool> OnToggleChange;

        /// <summary>
        /// Invoked either when user submits input (e.g., by clicking enter) in the input field or when
        /// toggle is set. In the latter case, the value inside the input field is passed.
        /// </summary>
        public event Action<float> OnInputFieldSubmit;

        /// <summary>
        /// Default value to be set in the input field
        /// </summary>
        public float DEFAULT_VALUE;

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////// IN-CURRENT or IN-CHILDREN REFERENCES //////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////

        [SerializeField]
        private Toggle m_Toggle;

        [SerializeField]
        private TMP_InputField m_InputField;

        private void Awake()
        {
            // default state
            m_InputField.text = DEFAULT_VALUE.ToString();
            m_InputField.gameObject.SetActive(false);
            m_Toggle.isOn = false;
            m_Toggle.onValueChanged.AddListener(OnToggleChangeHandler);
            m_InputField.onSubmit.AddListener(OnInputFieldSubmitHandler);
        }

        private void OnToggleChangeHandler(bool value)
        {
            OnToggleChange?.Invoke(value);
            if (value)
            {
                OnInputFieldSubmit?.Invoke(Single.Parse(m_InputField.text));
                m_InputField.gameObject.SetActive(true);
                return;
            }
            m_InputField.gameObject.SetActive(false);
        }

        private void OnInputFieldSubmitHandler(string newVal)
        {
            OnInputFieldSubmit?.Invoke(Single.Parse(newVal));
        }
    }
}
