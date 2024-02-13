using UnityEngine;
using UnityEngine.InputSystem;

namespace UnityCTVisualizer
{
    public class VolumetricObjectController : MonoBehaviour
    {
        [Range(0.0f, 0.5f)]
        public float m_ScaleSpeed;

        [Range(0.01f, 1.0f)]
        public float m_ScaleSlowDown;

        [Range(5.0f, 10.0f)]
        public float m_MaxScale;

        [Range(0.0f, 2.0f)]
        public float m_RotationSpeed;

        Transform m_Transform;
        UnityCTVisualizerInput m_InputLayer;

        float m_ScaleSpeedModifier = 1;
        Vector3 m_OriginalScale;
        Vector3 m_MaxScaleVect;

        void Awake()
        {
            m_Transform = GetComponent<Transform>();
            m_InputLayer = new UnityCTVisualizerInput();

            m_InputLayer.VolumetricObjectControls.Scale.performed += OnScale;

            m_InputLayer.VolumetricObjectControls.SlowScaleActivator.performed += (
                InputAction.CallbackContext _
            ) => m_ScaleSpeedModifier = m_ScaleSlowDown;
            m_InputLayer.VolumetricObjectControls.SlowScaleActivator.canceled += (
                InputAction.CallbackContext _
            ) => m_ScaleSpeedModifier = 1.0f;

            m_OriginalScale = m_Transform.localScale;
            m_MaxScaleVect = m_OriginalScale * m_MaxScale;

            m_InputLayer.VolumetricObjectControls.Rotation.performed += OnRotate;
        }

        float t = 0.0f;

        void OnScale(InputAction.CallbackContext context)
        {
            float scroll = context.ReadValue<float>();
            if (scroll > 0)
            {
                t = Mathf.Clamp01(t + m_ScaleSpeed * m_ScaleSpeedModifier);
                m_Transform.localScale = Vector3.Lerp(m_OriginalScale, m_MaxScaleVect, t);
            }
            // yes. This has to be done because on linux we get 120, 0, -120
            else if (scroll < 0)
            {
                t = Mathf.Clamp01(t - m_ScaleSpeed * m_ScaleSpeedModifier);
                m_Transform.localScale = Vector3.Lerp(m_OriginalScale, m_MaxScaleVect, t);
            }
        }

        void OnRotate(InputAction.CallbackContext context)
        {
            Vector2 delta = context.ReadValue<Vector2>();
            m_Transform.Rotate(-delta.y * m_RotationSpeed, delta.x * m_RotationSpeed, 0.0f);
        }

        void OnEnable()
        {
            m_InputLayer.VolumetricObjectControls.Enable();
        }

        void OnDisable()
        {
            m_InputLayer.VolumetricObjectControls.Disable();
        }
    }
}
