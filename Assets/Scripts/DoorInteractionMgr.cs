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
    private GameObject nPCObj;
    private List<NPCCtrl> nPCList = new List<NPCCtrl>();
    private Camera cam;

    private void Start()
    {
        inputProcess = GetComponent<InputProcess>();

        inputProcess.nPCAnchorHitSubject.Subscribe(PlaceNPCAnchor);

        CloudAnchorMgr.Singleton.objSpawnSubject
        .Where(obj => obj.GetComponent<NPCCtrl>() != null)
        .Subscribe(OnNPCSpawn);

        cam = Camera.main;
    }

    private void Update()
    {
        CheckNPCsInAngle();
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
        CloudAnchorMgr.Singleton.SpawnARSyncObject((int)SyncObjNum.NPC, relPose.position, relPose.rotation);
    }

    private void OnNPCSpawn(GameObject obj)
    {
        nPCList.Add(obj.GetComponent<NPCCtrl>());
    }

    private void OnNPCDesapwn(NPCCtrl nPC)
    {
        nPCList.Remove(nPC);
    }

    private void CheckNPCsInAngle()
    {
        foreach (var nPC in nPCList)
        {
            var viewPos = cam.WorldToViewportPoint(nPC.transform.position);

            if (viewPos.x >= 0 && viewPos.x <= 1 && viewPos.y >=0 && viewPos.y <= 1 && viewPos.z > 0)
            {
                CloudAnchorMgr.Singleton.DebugLogInUpdate($"NPC({nPC.name}) is In Angle");
            }
            else
            {
                CloudAnchorMgr.Singleton.DebugLogInUpdate($"NPC({nPC.name}) is not In Angle");
            }
        }
    }
}
