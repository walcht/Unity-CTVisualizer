#define DEBUG_UI
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityCTVisualizer
{
    [RequireComponent(typeof(RectTransform), typeof(Button))]
    public class AlphaControlPointUI
        : MonoBehaviour,
            IDragHandler,
            IBeginDragHandler,
            IPointerClickHandler,
            ISelectHandler,
            IDeselectHandler
    {
        /// <summary>
        /// Invoked when this alpha control point is selected. The ID assigned to this control point is
        /// passed.
        /// </summary>
        public event Action<int> ControlPointSelected;

        /// <summary>
        /// Invoked when this alpha control point is deselected. The ID assigned to this control point is
        /// passed.
        /// </summary>
        public event Action<int> ControlPointDeselected;

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////// IN-CURRENT or IN-CHILDREN REFERENCES //////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////

        [SerializeField]
        private Selectable m_ControlPointSelectable;

        [SerializeField]
        private RectTransform m_ControlPointTransform;

        [SerializeField]
        private Image m_Image;

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////// MISC ///////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////

        const float DEFAULT_ARROW_ALPHA = 0.75f;
        Vector3 m_PositionVect = new(0, 0, 0);
        Vector2 m_AnchorMin = new(0, 0.5f);
        Vector2 m_AnchorMax = new(0, 0.5f);
        int m_ID;

        // underlying alpha control point data
        ControlPoint<float, float> m_ControlPoint;
        public ControlPoint<float, float> ControlPointData
        {
            get => m_ControlPoint;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////// INITIALIZERS ////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Initializes control point's attributes. Call this before enabling this behaviour.
        /// </summary>
        /// <param name="id">the unique ID assigned to this control point. Useful for identifying it</param>
        /// <param name="cpData">initial control point data</param>
        public void Init(int id, ControlPoint<float, float> cpData)
        {
            m_ID = id;
            m_ControlPoint = cpData;
            SetPosition(cpData.Position, cpData.Value);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////// SETTERS //////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sets the 2D position for this alpha control point; both in UI and in underlying control point
        /// data
        /// </summary>
        public void SetPosition(float newX, float newY)
        {
            m_ControlPoint.Position = Mathf.Clamp01(newX);
            m_ControlPoint.Value = Mathf.Clamp01(newY);
            Vector2 newAnchor = new(m_ControlPoint.Position, m_ControlPoint.Value);
            m_ControlPointTransform.anchorMin = newAnchor;
            m_ControlPointTransform.anchorMax = newAnchor;
            // don't forget to reset rect position after updating anchors
            m_ControlPointTransform.anchoredPosition = m_PositionVect;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////// POINTER EVENTS ///////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////

        public void OnBeginDrag(PointerEventData eventData)
        {
            m_ControlPointSelectable.Select();
        }

        public void OnDrag(PointerEventData eventData)
        {
            SetPosition(
                m_ControlPoint.Position + (eventData.delta.x / Screen.width),
                m_ControlPoint.Value + (eventData.delta.y / Screen.height)
            );
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            m_ControlPointSelectable.Select();
        }

        public void OnSelect(BaseEventData eventData)
        {
            ControlPointSelected?.Invoke(m_ID);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            ControlPointDeselected?.Invoke(m_ID);
        }
    }
}
