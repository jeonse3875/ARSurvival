using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class OcclusionToggle : MonoBehaviour
{
    public AROcclusionManager mgr;
    public AROcclusionManager mgrInCam;
    [HideInInspector]
    public bool curStatus;

    private void Start()
    {
        mgr.enabled = false;
        mgrInCam.enabled = true;
    }

    public void SetOcclusion(bool status)
    {
        mgrInCam.enabled = status;
        mgr.enabled = !status;
        curStatus = status;
    }

    public void Toggle()
    {
        SetOcclusion(!curStatus);
    }
}
