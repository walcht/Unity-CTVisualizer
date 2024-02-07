using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Dropdown))]
public class TFDropDown : MonoBehaviour
{
    [SerializeField]
    TMP_Dropdown m_DropDown;
    Dictionary<string, int> m_TFEnumDict;
    List<TMP_Dropdown.OptionData> m_TFOptions;

    void Awake()
    {
        List<TMP_Dropdown.OptionData> m_TFOptions = new();
        m_TFEnumDict = new();
        foreach (var enumName in Enum.GetNames(typeof(TransferFunction)))
        {
            m_TFEnumDict.Add(enumName, (int)Enum.Parse(typeof(TransferFunction), enumName));
            m_TFOptions.Add(new(enumName));
        }
        m_DropDown.options = m_TFOptions;
    }

    void OnEnable()
    {
        m_DropDown.onValueChanged.AddListener(OnDropDownValueChange);
    }

    void OnDisable()
    {
        m_DropDown.onValueChanged.RemoveListener(OnDropDownValueChange);
    }

    void OnDropDownValueChange(int newValIndex)
    {
        // TODO: implement this when more than 1 TF is available
    }
}
