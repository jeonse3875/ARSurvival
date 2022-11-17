using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

public class AREnvironmentMgr : MonoBehaviour
{
    public GameObject cameraTargetPrefab;
    public GameObject depthMeshGenerator;
    private bool isPanorama = false;
    private Queue<DepthMeshCollider> depthMeshes = new Queue<DepthMeshCollider>();
    private InputProcess input;

    private void Start() 
    {
        input = GetComponent<InputProcess>();

        input.panoramaTargetDetectSubject.Subscribe(OnDetected);
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
        isPanorama = true;
        
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
            var generator = Instantiate(depthMeshGenerator,Vector3.zero,Quaternion.identity);
            depthMeshes.Enqueue(generator.GetComponent<DepthMeshCollider>());
            rot.eulerAngles = new Vector3(rot.eulerAngles.x, rot.eulerAngles.y + angle, rot.eulerAngles.z);
        }

    }

    private void OnDetected(Vector3 pos)
    {
        var depthMesh = depthMeshes.Dequeue();
        depthMesh.ShootProjectile();
        depthMeshes.Enqueue(depthMesh);
    }
}
