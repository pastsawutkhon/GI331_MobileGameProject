using System.Collections;
using UnityEngine;

public abstract class GhostEvent : MonoBehaviour
{
    [SerializeField] protected int triggerCount = 3;

    protected int useCount;
    protected bool isPlaying;

    public void RegisterUse()
    {
        if (isPlaying)
            return;

        useCount++;

        if (useCount >= triggerCount)
        {
            useCount = 0;
            isPlaying = true;

            OnTriggered();
        }
    }

    protected void FinishEvent()
    {
        isPlaying = false;
    }

    protected abstract void OnTriggered();
}