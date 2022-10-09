using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Google.XR.ARCoreExtensions;
using Unity.Netcode;
using UnityEngine.EventSystems;
using UniRx;

public enum AnchorHostingPhase
{
    nothingToHost, readyToHost, hostInProgress, success, fail
}

public enum AnchorResolvingPhase
{
    nothingToResolve, readyToResolve, resolveInProgress, success, fail
}

[RequireComponent(typeof(ARRaycastManager),typeof(ARAnchorManager))]
public class CloudAnchorMgr : NetworkBehaviour
{
    public static CloudAnchorMgr Singleton;
    [HideInInspector]
    public ARCloudAnchor cloudAnchor;
    private ARAnchor anchorToHost;
    private ARRaycastManager raycastManager;
    private ARAnchorManager anchorManager;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    [HideInInspector]
    public AnchorHostingPhase hostPhase;
    [HideInInspector]
    public AnchorResolvingPhase resolvePhase;
    private string idToResolve;
    private bool isStartEstimate = false;
    [HideInInspector]
    public GameObject cloudAnchorObj;

    public Camera arCam;
    public GameObject anchorPrefab;
    public List<GameObject> syncPrefab = new List<GameObject>();

    public Subject<string> logSubject = new Subject<string>();

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

    // Start is called before the first frame update
    void Start()
    {
        raycastManager = GetComponent<ARRaycastManager>();
        anchorManager = GetComponent<ARAnchorManager>();
    }

    // Update is called once per frame
    void Update()
    {
        InputProcess();
        HostResolveProcess();
    }

    private void InputProcess()
    {
        if (Input.touchCount < 1) return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase != TouchPhase.Began) return;

        if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) return;


        if (!NetworkManager.IsServer)
        {
            DebugLog($"You cannot create cloud anchor.");
            return;
        }

        if (raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
        {
            DebugLog($"Hit PlaneWithinPolygon");
            anchorToHost = anchorManager.AddAnchor(hits[0].pose);
            cloudAnchorObj = Instantiate(anchorPrefab, anchorToHost.transform);
            if (anchorToHost != null)
            {
                hostPhase = AnchorHostingPhase.readyToHost;
            }
            DebugLog($"Anchor created at {anchorToHost.transform.position}");
            isStartEstimate = true;
        }
    }

    private void HostResolveProcess()
    {
        FeatureMapQuality quality = FeatureMapQuality.Insufficient;

        if (isStartEstimate) { quality = anchorManager.EstimateFeatureMapQualityForHosting(GetCamPose()); }

        Vector3 anchorPos = Vector3.zero;
        if (cloudAnchor != null) { anchorPos = cloudAnchor.transform.position; }

        //DebugLog($"Anchor: {anchorPos}, Map Quality: {quality.ToString()}, Host: {hostPhase.ToString()}, Resolve: {resolvePhase.ToString()}, Cloud Anchor State: {cloudAnchor?.cloudAnchorState.ToString()}");

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
    }

    private Pose GetCamPose()
    {
        return new Pose(arCam.transform.position, arCam.transform.rotation);
    }

    public void HostAnchor()
    {
        DebugLog("Host Anchor ...");
        var quality = anchorManager.EstimateFeatureMapQualityForHosting(GetCamPose());
        DebugLog($"Feature map quality: {quality.ToString()}");
        cloudAnchor = anchorManager.HostCloudAnchor(anchorToHost, 1);
        hostPhase = AnchorHostingPhase.hostInProgress;

        if (cloudAnchor == null)
        {
            //fail
            DebugLog("Host failed");
            hostPhase = AnchorHostingPhase.fail;
        }
        else
        {
            //success
            DebugLog("Cloud anchor has been created");
        }
    }

    void CheckHostProgress()
    {
        var state = cloudAnchor.cloudAnchorState;
        DebugLog($"Host State: {state.ToString()}");
        
        if (state == CloudAnchorState.Success)
        {
            hostPhase = AnchorHostingPhase.success;
            idToResolve = cloudAnchor.cloudAnchorId;
            DebugLog($"Successfully Hosted. Anchor ID: {idToResolve}");
            resolvePhase = AnchorResolvingPhase.readyToResolve;
            SendAnchorIDClientRPC(idToResolve);
        }
        else if (state != CloudAnchorState.TaskInProgress)
        {
            hostPhase = AnchorHostingPhase.fail;
        }
        else
        {
            hostPhase = AnchorHostingPhase.hostInProgress;
        }

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
        DebugLog($"Resolve State: {state.ToString()}");

        if (state == CloudAnchorState.Success)
        {
            resolvePhase = AnchorResolvingPhase.success;
            var pos = cloudAnchor.pose.position;
            if (cloudAnchorObj != null) { Destroy(cloudAnchorObj); }
            cloudAnchorObj = Instantiate(anchorPrefab, cloudAnchor.transform);
            DebugLog($"Successfully Resolved. Cloud anchor position: {pos}");
        }
        else if (state != CloudAnchorState.TaskInProgress)
        {
            resolvePhase = AnchorResolvingPhase.fail;
        }
        else
        {
            resolvePhase = AnchorResolvingPhase.resolveInProgress;
        }
    }

    public Pose GetRelativePose(Pose worldVec)
    {
        if (cloudAnchor == null)
        {
            DebugLog("clouad anchor is null");
            return Pose.identity;
        }
        return cloudAnchor.transform.InverseTransformPose(worldVec);
    }

    public Pose GetWorldPose(Pose relVec)
    {
        if (cloudAnchor == null)
        {
            DebugLog("clouad anchor is null");
            return Pose.identity;
        }
        return cloudAnchor.transform.TransformPose(relVec);
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
