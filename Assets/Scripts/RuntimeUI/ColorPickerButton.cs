using UnityEngine;
using UnityEngine.UI;

namespace UnityCTVisualizer
{
    [RequireComponent(typeof(Image), typeof(Button))]
    public class ColorPickerButton : MonoBehaviour
    {
        public event VoidHandler OnClick;

        /// <summary>
        /// Default alpha for the color picker button (as in the actual alpha of the button UI not the picked
        /// color!)
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float m_ColorDefaultAlpha;

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////// IN-CURRENT or IN-CHILDREN REFERENCES //////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////
        ///
        [SerializeField]
        Image m_ImgComponent;

        [SerializeField]
        Button m_BtnComponent;
        Color m_CachedColor;

        void Awake()
        {
            m_BtnComponent.interactable = false;
            m_CachedColor = m_ImgComponent.color;
            m_CachedColor.a = m_ColorDefaultAlpha;
            m_ImgComponent.color = m_CachedColor;
            m_BtnComponent.onClick.AddListener(() => OnClick?.Invoke());
        }

        public void SetColor(Color color)
        {
            m_CachedColor.r = color.r;
            m_CachedColor.g = color.g;
            m_CachedColor.b = color.b;
            m_ImgComponent.color = m_CachedColor;
        }

        public void SetInteractiveness(bool val)
        {
            m_BtnComponent.interactable = val;
        }
    }
}
