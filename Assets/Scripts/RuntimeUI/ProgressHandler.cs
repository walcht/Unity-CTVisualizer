using TMPro;
using UnityEngine;

namespace UnityCTVisualizer
{
    public class ProgressHandler : MonoBehaviour, IProgressHandler
    {
        [SerializeField]
        TMP_Text m_TextMessage;

        [SerializeField]
        TMP_Text m_PercentageText;

        [SerializeField]
        RectTransform m_ProgressBar;

        void OnEnable()
        {
            m_ProgressBar.anchorMax = new Vector2(0.0f, 1.0f);
            m_PercentageText.text = "0 %";
        }

        public float Progress
        {
            set
            {
                float clampedVal = Mathf.Clamp01(value);
                UnityMainThreadWorker.Instance.AddJob(() =>
                {
                    m_ProgressBar.anchorMax = new Vector2(clampedVal, 1.0f);
                    m_PercentageText.text = $"{Mathf.FloorToInt(clampedVal * 100.0f)} %";
                });
            }
        }

        string m_Message;
        public string Message
        {
            set
            {
                UnityMainThreadWorker.Instance.AddJob(() =>
                {
                    m_TextMessage.text = value;
                });
            }
        }
    }
}
