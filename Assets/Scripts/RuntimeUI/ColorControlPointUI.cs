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
        public event Action<int> ControlPointSelected;

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////// CACHED COMPONENTS ////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////

        Selectable m_ControlPointSelectable;
        RectTransform m_ControlPointTransform;

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////// IN-CURRENT or IN-CHILDREN REFERENCES //////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////

        [SerializeField]
        TMP_Text m_PositionLabel;

        [SerializeField]
        Image m_Image;

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

        void Awake()
        {
            m_ControlPointSelectable = this.GetComponent<Selectable>();
            m_ControlPointTransform = this.GetComponent<RectTransform>();
            m_PositionLabel.gameObject.SetActive(true);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////// INITIALIZERS ////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////

        public void Init(int id, ControlPoint<float, Color> cpData)
        {
            m_ID = id;
            m_ControlPoint = cpData;
            SetPosition(cpData.Position);
            SetColor(cpData.Value);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////// GETTERS //////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////

        public ControlPoint<float, Color> GetControlPoint()
        {
            return m_ControlPoint;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////// SETTERS //////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////

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
