using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DepthMeshDebugger : MonoBehaviour
{
    public void OnMeshCreated()
    {
        CloudAnchorMgr.Singleton.DebugLog("Depth Mesh Created");
    }
}
