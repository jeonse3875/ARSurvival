using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Unity.Netcode;
using UniRx;

[RequireComponent(typeof(InputProcess))]
public class DoorInteractionMgr : MonoBehaviour
{
    private InputProcess inputProcess;

    private void Start()
    {
        inputProcess = GetComponent<InputProcess>();

        inputProcess.nPCAnchorHitSubject.Subscribe(PlaceNPCAnchor);
    }

    private void PlaceNPCAnchor(ARRaycastHit hit)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            CloudAnchorMgr.Singleton.DebugLog($"You cannot create npc anchor.");
            return;
        }

        var worldPose = hit.pose;
        var relPose = CloudAnchorMgr.Singleton.GetRelativePose(worldPose);
        CloudAnchorMgr.Singleton.SpawnARSyncObject(0,relPose.position, relPose.rotation);
    }
}
