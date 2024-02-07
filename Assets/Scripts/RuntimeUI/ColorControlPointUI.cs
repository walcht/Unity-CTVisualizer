using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityCTVisualizer
{
    [RequireComponent(typeof(RectTransform), typeof(Button))]
    public class ColorControlPointUI
        : MonoBehaviour,
            IDragHandler,
            IBeginDragHandler,
            IPointerClickHandler,
            ISelectHandler
    {
        /// <summary>
        /// Invoked when this color control point is selected. The ID assigned to this control point is
        /// passed.
        /// </summary>
        public event Action<int> ControlPointSelected;

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////// IN-CURRENT or IN-CHILDREN REFERENCES //////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////

        [SerializeField]
        private Selectable m_ControlPointSelectable;

        [SerializeField]
        private RectTransform m_ControlPointTransform;

        [SerializeField]
        private TMP_Text m_PositionLabel;

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
        ControlPoint<float, Color> m_ControlPoint;
        public ControlPoint<float, Color> ControlPointData
        {
            get => m_ControlPoint;
        }

        private void Awake()
        {
            m_PositionLabel.gameObject.SetActive(true);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////// INITIALIZERS ////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Initializes control point's attributes. Call this before enabling this behaviour.
        /// </summary>
        /// <param name="id">the unique ID assigned to this control point. Useful for identifying it</param>
        /// <param name="cpData">initial control point data</param>
        public void Init(int id, ControlPoint<float, Color> cpData)
        {
            m_ID = id;
            m_ControlPoint = cpData;
            SetPosition(cpData.Position);
            SetColor(cpData.Value);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////// SETTERS //////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sets the position for this control point; both in UI and in underlying control point data
        /// </summary>
        /// <param name="pos">normalized position. Values not in the range [0.0, 1.0] are clamped.</param>
        public void SetPosition(float pos)
        {
            m_ControlPoint.Position = Mathf.Clamp01(pos);
            m_AnchorMin.x = m_ControlPoint.Position;
            m_AnchorMax.x = m_ControlPoint.Position;
            m_ControlPointTransform.anchorMin = m_AnchorMin;
            m_ControlPointTransform.anchorMax = m_AnchorMax;
            // don't forget to reset rect position after updating anchors
            m_ControlPointTransform.anchoredPosition = m_PositionVect;
            m_PositionLabel.text = m_ControlPoint.Position.ToString("0.00");
        }

        /// <summary>
        /// Sets the color for this control point; both in UI and in underlying control point data
        /// </summary>
        /// <param name="col">new color value. Alpha component is ignored</param>
        public void SetColor(Color col)
        {
            col.a = DEFAULT_ARROW_ALPHA;
            m_ControlPoint.Value = col;
            m_Image.color = m_ControlPoint.Value;
#if DEBUG_UI
            Debug.Log($"Color changed to: {m_Image.color}");
#endif
            return;
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
            SetPosition(m_ControlPoint.Position + (eventData.delta.x / (float)Screen.width));
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            m_ControlPointSelectable.Select();
        }

        public void OnSelect(BaseEventData eventData)
        {
#if DEBUG_UI
            Debug.Log($"Color control point selected: {m_ID}");
#endif
            ControlPointSelected?.Invoke(m_ID);
        }
    }
}
