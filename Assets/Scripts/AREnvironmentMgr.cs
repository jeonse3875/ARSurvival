using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

public class AREnvironmentMgr : MonoBehaviour
{
    public GameObject cameraTargetPrefab;
    public GameObject depthMeshObj;
    private int leftMeshCount = -1;
    private Queue<MeshFilter> depthMeshes = new Queue<MeshFilter>();
    public MeshCollider originalMesh;
    public InputProcess inputProcess;
    public DepthMeshCollider depthMeshCollider;

    private void Start() 
    {
        inputProcess.panoramaTargetDetectSubject.Subscribe(OnPanoramaTargetDetected);
    }

    private void Update() 
    {
        //CloudAnchorMgr.Singleton.DebugLogInUpdate(GetHorizontalFoV().ToString());
    }

    private float GetHorizontalFoV()
    {
        return Camera.VerticalToHorizontalFieldOfView(Camera.main.fieldOfView,Camera.main.aspect);
    }

    public void StartPanorama()
    {
        var fov = GetHorizontalFoV();
        
        int count = Mathf.CeilToInt(360f/fov);
        float angle = 360f/count;

        var origin = Camera.main.transform.position;
        var euler = Camera.main.transform.rotation.eulerAngles;
        euler.x = 0f;
        euler.z = 0f;
        var rot = Quaternion.Euler(euler);
        CloudAnchorMgr.Singleton.DebugLog($"Create panoram target. Angle: {angle}, Count: {count}");

        for(int i = 0; i<count; i++)
        {
            var targetPos = origin + rot * Vector3.forward;
            Instantiate(cameraTargetPrefab,targetPos,Quaternion.identity);
            var mesh = Instantiate(depthMeshObj,Vector3.zero,Quaternion.identity);
            depthMeshes.Enqueue(mesh.GetComponent<MeshFilter>());
            rot.eulerAngles = new Vector3(rot.eulerAngles.x, rot.eulerAngles.y + angle, rot.eulerAngles.z);
        }

        leftMeshCount = count;
    }

    public void OnMeshCreated()
    {
        if (leftMeshCount <= 0) return;

        var mesh = depthMeshes.Dequeue();
        mesh.sharedMesh = (Mesh)Instantiate(originalMesh.sharedMesh);
        depthMeshes.Enqueue(mesh);
        leftMeshCount--;
    }

    private void OnPanoramaTargetDetected(Vector3 pos)
    {
        depthMeshCollider.ShootProjectile();
    }
}
