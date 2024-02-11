using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityCTVisualizer
{
    [RequireComponent(typeof(RawImage))]
    public class ColorGradientRangeUI : MonoBehaviour, IPointerClickHandler
    {
        public event Action<float> OnAddColorControlPoint;
        RectTransform m_RectTransform;

        void Awake()
        {
            m_RectTransform = GetComponent<RectTransform>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                Vector2 rectLocalPos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    m_RectTransform,
                    eventData.pressPosition,
                    eventData.pressEventCamera,
                    out rectLocalPos
                );
                float xPos =
                    (rectLocalPos.x + m_RectTransform.pivot.x * m_RectTransform.rect.width)
                    / m_RectTransform.rect.width;
                OnAddColorControlPoint?.Invoke(xPos);
            }
        }
    }
}
