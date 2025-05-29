using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dispatch calls from background threads to Unity's main thread.
/// Required for making Unity API calls from Tasks or threads.
/// </summary>
public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> queue = new Queue<Action>();
    private static UnityMainThreadDispatcher instance;

    /// <summary>
    /// Queue an action to be executed on the main thread.
    /// </summary>
    /// <param name="action">Action to execute on main thread</param>
    public static void Enqueue(Action action)
    {
        if (action == null) return;

        if (instance == null)
        {
            Debug.LogError("UnityMainThreadDispatcher not found in scene");
            return;
        }

        lock (queue) queue.Enqueue(action);
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        lock (queue)
        {
            while (queue.Count > 0)
            {
                try
                {
                    queue.Dequeue()?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in dispatched action: {ex.Message}");
                }
            }
        }
    }
}
