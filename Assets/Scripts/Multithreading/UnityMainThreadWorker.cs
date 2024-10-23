using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityCTVisualizer
{
    /// <summary>
    /// Dispatches jobs on Unity's main worker thread. This is especially useful for updating Unity Engine-
    /// related components (e.g., UIs) from a different thread. Attempting such updates directly in their
    /// origin threads is not possible.
    /// </summary>
    public class UnityMainThreadWorker : MonoBehaviour
    {
        public static UnityMainThreadWorker Instance;
        Queue<Action> jobs = new();

        void Awake()
        {
            Instance = this;
            QualitySettings.vSyncCount = 0; // Set vSyncCount to 0 so that using .targetFrameRate is enabled.
            Application.targetFrameRate = 120;
        }

        void Update()
        {
            while (jobs.Count > 0)
            {
                jobs.Dequeue().Invoke();
            }
        }

        public void AddJob(Action job)
        {
            jobs.Enqueue(job);
        }
    }
}
