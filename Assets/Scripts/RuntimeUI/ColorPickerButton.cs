using UnityEngine;
using UnityEngine.UI;

namespace UnityCTVisualizer
{
    [RequireComponent(typeof(Image), typeof(Button))]
    public class ColorPickerButton : MonoBehaviour
    {
        public event VoidHandler OnClick;

        [Range(0.0f, 1.0f)]
        public float m_ColorDefaultAlpha;

        Image m_ImgComponent;
        Button m_BtnComponent;
        Color m_CachedColor;

        void Awake()
        {
            m_ImgComponent = GetComponent<Image>();
            m_BtnComponent = GetComponent<Button>();
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
