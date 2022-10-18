using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.Netcode;
using UniRx;

[RequireComponent(typeof(InputProcess),typeof(ARRaycastManager))]
public class DoorInteractionMgr : MonoBehaviour
{
    private InputProcess inputProcess;
    private ARRaycastManager raycastManager;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private GameObject nPCObj;
    private List<NPCCtrl> nPCList = new List<NPCCtrl>();
    private Camera cam;

    private void Start()
    {
        inputProcess = GetComponent<InputProcess>();
        raycastManager = GetComponent<ARRaycastManager>();

        inputProcess.nPCAnchorHitSubject.Subscribe(PlaceNPCAnchor);

        CloudAnchorMgr.Singleton.objSpawnSubject
        .Where(obj => obj.GetComponent<NPCCtrl>() != null)
        .Subscribe(OnNPCSpawn);

        cam = Camera.main;

        Type type = typeof(MotionStereoDepthDataSource);
        Debug.Log(type.AssemblyQualifiedName);
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
            var nPCPos = nPC.origin.position;
            var viewPos = cam.WorldToViewportPoint(nPCPos);

            if (viewPos.x >= 0 && viewPos.x <= 1 && viewPos.y >=0 && viewPos.y <= 1 && viewPos.z > 0)
            {
                //CloudAnchorMgr.Singleton.DebugLogInUpdate($"NPC({nPC.name}) is In Angle");
                var camPos = cam.transform.position;
                Ray ray = new Ray(camPos, nPCPos - camPos);
                var nPCDistance = Vector3.Distance(camPos,nPCPos);

                if (raycastManager.Raycast(ray, hits, TrackableType.Depth))
                {
                    if (hits[0].distance > nPCDistance)
                    {
                        CloudAnchorMgr.Singleton.DebugLogInUpdate($"NPC({nPC.name}) is in depth");
                    }
                    else
                    {
                        CloudAnchorMgr.Singleton.DebugLogInUpdate($"Dist: {nPCDistance}, Depth: {hits[0].distance}");
                    }
                }
            }
            else
            {
                CloudAnchorMgr.Singleton.DebugLogInUpdate($"NPC({nPC.name}) is not In Angle");
            }
        }
    }
}
