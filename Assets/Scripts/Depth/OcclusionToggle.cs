using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class OcclusionToggle : MonoBehaviour
{
    public AROcclusionManager mgr;
    public AROcclusionManager mgrInCam;
    public DepthMeshCollider depthMeshCollider;

    private void Update()
    {
        var currentActiveMgr = mgr;
        if (mgrInCam.currentEnvironmentDepthMode != EnvironmentDepthMode.Disabled) {currentActiveMgr = mgrInCam;}
        depthMeshCollider.SetOcclusionManager(currentActiveMgr);
    }

    public void SetOcclusion(bool status)
    {
        if (status)
        {
            mgrInCam.requestedEnvironmentDepthMode = EnvironmentDepthMode.Fastest;
            mgr.requestedEnvironmentDepthMode = EnvironmentDepthMode.Disabled;
        }
        else
        {
            mgrInCam.requestedEnvironmentDepthMode = EnvironmentDepthMode.Disabled;
            mgr.requestedEnvironmentDepthMode = EnvironmentDepthMode.Fastest;
        }
    }

    public void Toggle()
    {
        var curStatus = mgrInCam.currentEnvironmentDepthMode != EnvironmentDepthMode.Disabled;
        SetOcclusion(!curStatus);
    }
}
