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

    private AROcclusionManager GetActiveMgr()
    {
        if (curStatus)
            return mgrInCam;
        else
            return mgr;
    }

    public void Toggle()
    {
        SetOcclusion(!curStatus);
        foreach (var depthMeshCollider in FindObjectsOfType<DepthMeshCollider>())
        {
            depthMeshCollider.SetOcclusionManager(GetActiveMgr());
        }
    }
}
