using UnityEngine;
using UnityEngine.UI;

namespace UnityCTVisualizer
{
    public class CameraParametersUI : MonoBehaviour
    {
        [SerializeField]
        Toggle m_PerspectiveTgl;

        void Awake()
        {
            m_PerspectiveTgl.onValueChanged.AddListener(OnPerspectiveToggle);
        }

        void OnPerspectiveToggle(bool isPerspective)
        {
            Camera.main.orthographic = !isPerspective;
        }
    }
}
