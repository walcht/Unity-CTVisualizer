using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityCTVisualizer { }

public class UnityMainThreadWorker : MonoBehaviour
{
    public static UnityMainThreadWorker Instance;
    Queue<Action> jobs = new Queue<Action>();

    void Awake()
    {
        Instance = this;
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
