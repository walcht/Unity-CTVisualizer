using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityCTVisualizer
{
    [RequireComponent(typeof(RawImage))]
    public class HistogramUI : MonoBehaviour, IPointerClickHandler
    {
        public event Action<Vector2> OnAddAlphaControlPoint;

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
                rectLocalPos.x =
                    (rectLocalPos.x + m_RectTransform.pivot.x * m_RectTransform.rect.width)
                    / m_RectTransform.rect.width;
                rectLocalPos.y =
                    (rectLocalPos.y + m_RectTransform.pivot.y * m_RectTransform.rect.height)
                    / m_RectTransform.rect.height;
                OnAddAlphaControlPoint?.Invoke(rectLocalPos);
            }
        }
    }
}
