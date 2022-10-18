using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DepthTargetSwitch : MonoBehaviour
{
    private DepthTarget target;
    private bool on = true;

    private void Start()
    {
        target = GetComponent<DepthTarget>();
        target.enabled = false;
    }

    private void Update()
    {
        if (on)
        {
            if (DepthSource.Initialized)
            {
                target.enabled = true;
                on = false;
            }
        }
    }
}
