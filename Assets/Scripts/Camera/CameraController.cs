using UnityEngine;
using UnityEngine.InputSystem;

namespace UnityCTVisualizer
{
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        [Range(0.0f, 0.5f)]
        public float m_ZoomSpeed;

        [Range(0.01f, 1.0f)]
        public float m_ZoomSlowDown;

        [Range(0.0f, 5.0f)]
        public float m_MinDistance;

        [Range(5.0f, 10.0f)]
        public float m_MaxDistance;

        [Range(0.0f, 2.0f)]
        public float m_RotationSpeed;

        Transform m_Transform;
        UnityCTVisualizerInput m_InputLayer;

        float m_ZoomSpeedModifier = 1;

        void Awake()
        {
            m_Transform = GetComponent<Transform>();
            m_InputLayer = new UnityCTVisualizerInput();

            m_InputLayer.CameraControls.ZoomInOut.performed += OnZoomInOrOut;

            m_InputLayer.CameraControls.SlowZoomActivator.performed += (
                InputAction.CallbackContext _
            ) => m_ZoomSpeedModifier = m_ZoomSlowDown;
            m_InputLayer.CameraControls.SlowZoomActivator.canceled += (
                InputAction.CallbackContext _
            ) => m_ZoomSpeedModifier = 1.0f;

            m_InputLayer.CameraControls.VerticalRotation.performed += OnRotateAroundVertically;
            m_InputLayer.CameraControls.HorizontalRotation.performed += OnRotateAroundHorizontally;
        }

        void OnZoomInOrOut(InputAction.CallbackContext context)
        {
            // assume camera is looking at the target
            float scroll = context.ReadValue<float>();
            Debug.Log($"Scroll: {scroll}");
            if (scroll > 0)
            {
                m_Transform.position +=
                    m_Transform.forward
                    * Mathf.Clamp(
                        m_ZoomSpeed * m_ZoomSpeedModifier,
                        0.0f,
                        m_Transform.position.sqrMagnitude - m_MinDistance
                    );
            }
            // yes. This has to be done because on linux we get 120, 0, -120
            else if (scroll < 0)
            {
                m_Transform.position -=
                    m_Transform.forward
                    * Mathf.Clamp(
                        m_ZoomSpeed * m_ZoomSpeedModifier,
                        0.0f,
                        m_MaxDistance - m_Transform.position.sqrMagnitude
                    );
            }
        }

        void OnRotateAroundVertically(InputAction.CallbackContext context)
        {
            m_Transform.RotateAround(
                Vector3.zero,
                Vector3.left,
                context.ReadValue<Vector2>().y * m_RotationSpeed
            );
        }

        void OnRotateAroundHorizontally(InputAction.CallbackContext context)
        {
            m_Transform.RotateAround(
                Vector3.zero,
                Vector3.up,
                context.ReadValue<Vector2>().x * m_RotationSpeed
            );
        }

        void OnEnable()
        {
            m_Transform.LookAt(Vector3.zero);
            // in case initial camera position is out of min-max range
            if (m_Transform.position.sqrMagnitude < m_MinDistance)
                m_Transform.position -=
                    m_Transform.forward * (m_MinDistance - m_Transform.position.sqrMagnitude);
            else if (m_Transform.position.sqrMagnitude > m_MaxDistance)
                m_Transform.position +=
                    m_Transform.forward * (m_Transform.position.sqrMagnitude - m_MaxDistance);
            m_InputLayer.CameraControls.Enable();
        }

        void OnDisable()
        {
            m_InputLayer.CameraControls.Disable();
        }
    }
}
