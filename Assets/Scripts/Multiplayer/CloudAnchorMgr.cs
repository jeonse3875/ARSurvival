using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Google.XR.ARCoreExtensions;
using Unity.Netcode;
using UniRx;

public enum AnchorHostingPhase
{
    nothingToHost, readyToHost, hostInProgress, success, fail
}

public enum AnchorResolvingPhase
{
    nothingToResolve, readyToResolve, resolveInProgress, success, fail
}

[RequireComponent(typeof(ARAnchorManager),typeof(InputProcess))]
public class CloudAnchorMgr : NetworkBehaviour
{
    public static CloudAnchorMgr Singleton;
    [HideInInspector]
    public ARCloudAnchor cloudAnchor;
    private ARAnchor anchorToHost;
    private InputProcess inputProcess;
    private ARAnchorManager anchorManager;
    [HideInInspector]
    public AnchorHostingPhase hostPhase;
    [HideInInspector]
    public AnchorResolvingPhase resolvePhase;
    private string idToResolve;
    private bool isStartEstimate = false;
    public bool isAnchorHosted = false;
    [HideInInspector]
    public GameObject cloudAnchorObj;

    public Camera arCam;
    public GameObject anchorPrefab;
    public List<GameObject> syncPrefab = new List<GameObject>();

    public Subject<string> logSubject = new Subject<string>();
    public Subject<string> logInUpdateSubject = new Subject<string>();
    public Subject<GameObject> objSpawnSubject = new Subject<GameObject>();

    private void Awake()
    {
        if (Singleton == null)
        {
            Singleton = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            if (Singleton != this)
                Destroy(this.gameObject);
        }
    }

    private void Start()
    {
        anchorManager = GetComponent<ARAnchorManager>();
        inputProcess = GetComponent<InputProcess>();

        inputProcess.cloudAnchorHitSubject.Subscribe(PlaceCloudAnchor);
    }

    private void Update()
    {
        HostResolveProcess();
    }

    private void PlaceCloudAnchor(ARRaycastHit hit)
    {
        if (!NetworkManager.IsServer)
        {
            DebugLog($"You cannot create cloud anchor.");
            return;
        }

        anchorToHost = anchorManager.AddAnchor(hit.pose);

        if (anchorToHost != null)
        {
            hostPhase = AnchorHostingPhase.readyToHost;
            cloudAnchorObj = Instantiate(anchorPrefab, anchorToHost.transform);
            DebugLog($"Anchor created at {anchorToHost.transform.position}");
            isStartEstimate = true;
        }
        else
        {
            DebugLog($"Anchor is null");
        }
    }

    private void HostResolveProcess()
    {
        FeatureMapQuality quality = FeatureMapQuality.Insufficient;

        if (isStartEstimate) 
        { 
            quality = anchorManager.EstimateFeatureMapQualityForHosting(GetCamPose()); 
            DebugLogInUpdate($"Hosting Quality: {quality.ToString()}");
        }

        if (anchorToHost == null)
        {
            hostPhase = AnchorHostingPhase.nothingToHost;
        }
        else if (cloudAnchor != null && hostPhase == AnchorHostingPhase.hostInProgress)
        {
            CheckHostProgress();
        }

        if (cloudAnchor == null)
        {
            resolvePhase = AnchorResolvingPhase.nothingToResolve;
        }
        else if (cloudAnchor != null && resolvePhase == AnchorResolvingPhase.resolveInProgress)
        {
            CheckResolveProgress();
        }
    }

    [ClientRpc]
    public void SendAnchorIDClientRPC(string id)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            DebugLog($"Ignore received Anchor ID: {idToResolve}");
            return;
        }

        idToResolve = id;
        DebugLog($"Receive Anchor ID: {idToResolve}");
        resolvePhase = AnchorResolvingPhase.readyToResolve;
    }

    private Pose GetCamPose()
    {
        return new Pose(arCam.transform.position, arCam.transform.rotation);
    }

    public void HostAnchor()
    {
        DebugLog("Host Anchor ...");
        cloudAnchor = anchorManager.HostCloudAnchor(anchorToHost, 1);
        hostPhase = AnchorHostingPhase.hostInProgress;

        if (cloudAnchor == null)
        {
            //fail
            DebugLog($"Hosting failed");
            hostPhase = AnchorHostingPhase.fail;
        }
        else
        {
            //success
            DebugLog("Cloud anchor has been created");
        }
    }

    private void CheckHostProgress()
    {
        var state = cloudAnchor.cloudAnchorState;
        DebugLogInUpdate($"Host State: {state.ToString()}");
        
        if (state == CloudAnchorState.Success)
        {
            hostPhase = AnchorHostingPhase.success;
            idToResolve = cloudAnchor.cloudAnchorId;
            DebugLogInUpdate($"Successfully Hosted. Anchor ID: {idToResolve}");
            resolvePhase = AnchorResolvingPhase.readyToResolve;
            SendAnchorIDClientRPC(idToResolve);
            isStartEstimate = false;
            isAnchorHosted = true;
        }
        else if (state != CloudAnchorState.TaskInProgress)
        {
            hostPhase = AnchorHostingPhase.fail;
            SetCloudAnchorDefault();
        }
        else
        {
            hostPhase = AnchorHostingPhase.hostInProgress;
        }

    }

    private void SetCloudAnchorDefault()
    {
        isStartEstimate = false;
        isAnchorHosted = false;

        Destroy(cloudAnchor.gameObject);
        cloudAnchor = null;
        Destroy(cloudAnchorObj);
        cloudAnchorObj = null;
        Destroy(anchorToHost.gameObject);
        anchorToHost = null;
    }

    public void CreateTestAnchor()
    {
        DebugLog("Test anchor created");
        anchorToHost = anchorManager.AddAnchor(new Pose(Vector3.zero, Quaternion.identity));
        if (anchorToHost != null)
        {
            hostPhase = AnchorHostingPhase.readyToHost;
        }
    }

    public void ResolveAnchor()
    {
        DebugLog("Resolve Anchor ...");
        cloudAnchor = null;
        cloudAnchor = anchorManager.ResolveCloudAnchorId(idToResolve);
        resolvePhase = AnchorResolvingPhase.resolveInProgress;

        if (cloudAnchor == null)
        {
            DebugLog("Resolve failed");
            resolvePhase = AnchorResolvingPhase.fail;
        }
        else
        {
            DebugLog("Cloud anchor has been created");
        }
    }

    void CheckResolveProgress()
    {
        var state = cloudAnchor.cloudAnchorState;
        DebugLogInUpdate($"Resolve State: {state.ToString()}");

        if (state == CloudAnchorState.Success)
        {
            resolvePhase = AnchorResolvingPhase.success;
            var pos = cloudAnchor.pose.position;
            if (cloudAnchorObj != null) { Destroy(cloudAnchorObj); }
            cloudAnchorObj = Instantiate(anchorPrefab, cloudAnchor.transform);
            DebugLogInUpdate($"Successfully Resolved. Cloud anchor position: {pos}");

            //임시
            inputProcess.phase = InputPhase.NPCAnchor;
        }
        else if (state != CloudAnchorState.TaskInProgress)
        {
            resolvePhase = AnchorResolvingPhase.fail;
            SetCloudAnchorDefault();
        }
        else
        {
            resolvePhase = AnchorResolvingPhase.resolveInProgress;
        }
    }

    public Pose GetRelativePose(Pose worldPose)
    {
        if (cloudAnchor == null)
        {
            DebugLog("clouad anchor is null");
            return Pose.identity;
        }
        return cloudAnchor.transform.InverseTransformPose(worldPose);
    }

    public Pose GetWorldPose(Pose relPose)
    {
        if (cloudAnchor == null)
        {
            DebugLog("clouad anchor is null");
            return Pose.identity;
        }
        return cloudAnchor.transform.TransformPose(relPose);
    }

    private void SpawnObj(int objNum, Vector3 relPos, Quaternion relRot, ulong ownerId)
    {
        if (!NetworkManager.IsServer) return;

        Pose relPose = new Pose(relPos, relRot);
        Pose worldPose = GetWorldPose(relPose);
        var instance = Instantiate(syncPrefab[objNum], worldPose.position, worldPose.rotation, cloudAnchor.transform);
        DebugLog($"obj created. Owner: {ownerId}, Relative: {relPose.ToString()}, World: {worldPose.ToString()}");
        NetworkObject netObj = instance.GetComponent<NetworkObject>();

        if (netObj == null)
        {
            DebugLog($"NetObj is null");
            return;
        }

        netObj.SpawnWithOwnership(ownerId);
        objSpawnSubject.OnNext(instance);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnObjServerRpc(int objNum, Vector3 relPos, Quaternion relRot, ulong ownerId)
    {
        DebugLog("Receive SpawnObjServerRPC");
        Pose relPose = new Pose(relPos, relRot);
        Pose worldPose = GetWorldPose(relPose);
        var instance = Instantiate(syncPrefab[objNum], worldPose.position, worldPose.rotation, cloudAnchor.transform);
        DebugLog($"obj created. Owner: {ownerId}, Relative: {relPose.ToString()}, World: {worldPose.ToString()}");
        NetworkObject netObj = instance.GetComponent<NetworkObject>();

        if (netObj == null)
        {
            DebugLog($"NetObj is null");
            return;
        }

        netObj.SpawnWithOwnership(ownerId);
    }

    public void DebugLog(string msg)
    {
        logSubject.OnNext(msg);
    }

    public void DebugLogInUpdate(string msg)
    {
        logInUpdateSubject.OnNext(msg);
    }

    public void SpawnARSyncObject(int objNum, Vector3 relPos, Quaternion relRot)
    {
        if (NetworkManager.IsServer)
        {
            DebugLog("Spawn AR Sync Object");
            SpawnObj(objNum, relPos, relRot, NetworkManager.Singleton.LocalClientId);
        }
        else
        {
            DebugLog("Spawn AR Sync Object: call server rpc");
            SpawnObjServerRpc(objNum, relPos, relRot, NetworkManager.Singleton.LocalClientId);
        }
    }
}
