using HSVPicker;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace UnityCTVisualizer
{
    [RequireComponent(typeof(RectTransform), typeof(Canvas))]
    public class ColorPickerWrapper : MonoBehaviour
    {
        /// <summary>
        /// Invoked when the user is done with picking a color (e.g., through a button click action).
        /// </summary>
        public event Action<Color> ColorPickerDone;

        /// <summary>
        /// Invoked when the color picker value changes. This event is throttled for performance reasons.
        /// </summary>
        public event Action<Color> ColorPickerChange;

        public ColorPicker m_ColorPicker;
        public Button m_ColorPickerDone;

        void Awake()
        {
            m_ColorPickerDone.onClick.AddListener(
                () => ColorPickerDone?.Invoke(m_ColorPicker.CurrentColor)
            );
            m_ColorPicker.onValueChanged.AddListener((Color clr) => ColorPickerChange?.Invoke(clr));
        }

        /// <summary>
        /// Initializes the color picker. Call this before enabling a ColorPickerWrapper component.
        /// </summary>
        /// <param name="initialColor">Initial color to set for the color picker</param>
        public void Init(Color initialColor)
        {
            m_ColorPicker.CurrentColor = initialColor;
        }
    }
}
