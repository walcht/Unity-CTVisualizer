using TMPro;
using UnityEngine;
using System.Threading;
using UnityEngine.UIElements;

namespace UnityCTVisualizer {
    public interface IProgressHandler {
        /// <summary>
        ///     Progress from 0.00 to 1.00. Read and update operataions are thread-safe.
        /// </summary>
        float Progress { get; set; }
        string Message { set; }

        bool Enabled { set; }
    }

    public class ProgressHandler : MonoBehaviour, IProgressHandler {
        private readonly object m_progress_lock = new();
        private readonly object m_message_lock = new();
        private readonly object m_enabled_lock = new();

        [SerializeField] TMP_Text m_TextMessage;
        [SerializeField] TMP_Text m_PercentageText;
        [SerializeField] RectTransform m_ProgressBar;
        [SerializeField] float m_Progress;

        void OnEnable() {
            m_ProgressBar.anchorMax = new Vector2(0.0f, 1.0f);
            m_PercentageText.text = "0 %";
        }

        public float Progress {
            get {
                lock (m_progress_lock) {
                    return m_Progress;
                }
            }
            set {
                lock (m_progress_lock) {
                    m_Progress = Mathf.Clamp01(value);
                }
            }
        }

        public string Message {
            set {
                lock (m_message_lock) {
                    m_TextMessage.text = value;
                }
            }
        }

        public bool Enabled {
            set {
                lock (m_enabled_lock) {
                    gameObject.SetActive(value);
                }
            }
        }

        private void Update() {
            m_ProgressBar.anchorMax = new Vector2(m_Progress, 1.0f);
            m_PercentageText.text = $"{Mathf.FloorToInt(m_Progress * 100.0f)} %";
        }
    }
}
