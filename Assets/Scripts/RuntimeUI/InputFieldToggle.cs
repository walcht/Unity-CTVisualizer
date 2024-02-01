using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UnityCTVisualizer
{
    public class InputFieldToggle : MonoBehaviour
    {
        Toggle m_Toggle;
        TMP_InputField m_InputField;

        void Awake()
        {
            m_Toggle = gameObject.GetComponentInChildren<Toggle>();
            m_InputField = gameObject.GetComponentInChildren<TMP_InputField>();
            // default state
            m_InputField.gameObject.SetActive(false);
            m_Toggle.isOn = false;
        }

        void OnEnable()
        {
            m_Toggle.onValueChanged.AddListener(OnToggleChange);
        }

        void OnDisable()
        {
            m_Toggle.onValueChanged.RemoveListener(OnToggleChange);
        }

        void OnToggleChange(bool value)
        {
            if (value)
            {
                m_InputField.gameObject.SetActive(true);
                return;
            }
            m_InputField.gameObject.SetActive(false);
        }
    }
}
