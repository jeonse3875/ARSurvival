using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.Netcode;
using UniRx;

[RequireComponent(typeof(InputProcess),typeof(ARRaycastManager),typeof(ARAnchorManager))]
public class DoorInteractionMgr : MonoBehaviour
{
    private InputProcess inputProcess;
    private ARRaycastManager raycastManager;
    private ARAnchorManager anchorManager;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private GameObject nPCObj;
    private List<NPCCtrl> nPCList = new List<NPCCtrl>();
    private Camera cam;
    public GameObject nPCPrefab;

    private void Start()
    {
        inputProcess = GetComponent<InputProcess>();
        raycastManager = GetComponent<ARRaycastManager>();
        anchorManager = GetComponent<ARAnchorManager>();

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

        // 클라우드 앵커 동기화 방식
        var dir = CloudAnchorMgr.Singleton.cloudAnchorObj.transform.position - hit.pose.position;
        var rot = Quaternion.LookRotation(dir,CloudAnchorMgr.Singleton.cloudAnchorObj.transform.up);
        var worldPose = new Pose(hit.pose.position, rot);
        var relPose = CloudAnchorMgr.Singleton.GetRelativePose(worldPose);
        relPose = new Pose(new Vector3(relPose.position.x,0f,relPose.position.z),relPose.rotation);
        var euler = relPose.rotation.eulerAngles;
        euler = new Vector3(0f,euler.y,0f);
        relPose.rotation = Quaternion.Euler(euler);
        //CloudAnchorMgr.Singleton.SpawnARSyncObject((int)SyncObjNum.NPC, relPose.position, relPose.rotation);

        // 앵커 생성 방식
        var anchor = anchorManager.AddAnchor(hit.pose);
        worldPose = CloudAnchorMgr.Singleton.GetWorldPose(relPose);
        Instantiate(nPCPrefab,worldPose.position,worldPose.rotation,anchor.transform);
        //rpc 호출
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
