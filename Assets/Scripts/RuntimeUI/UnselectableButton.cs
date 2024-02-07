using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityCTVisualizer
{
    [RequireComponent(typeof(Image))]
    public class UnselectableButton : MonoBehaviour, IPointerClickHandler
    {
        public event VoidHandler OnClick;

        [Range(0.0f, 1.0f)]
        public float m_DisabledColorAlpha = 0.2f;

        [SerializeField]
        Image m_ImageComponent;

        public void OnPointerClick(PointerEventData pointerEventData)
        {
            OnClick?.Invoke();
        }

        void OnDisable()
        {
            Color disabledColor = m_ImageComponent.color;
            disabledColor.a = m_DisabledColorAlpha;
            m_ImageComponent.color = disabledColor;
        }
    }
}
