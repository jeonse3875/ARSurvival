using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

public class AREnvironmentMgr : MonoBehaviour
{
    public GameObject cameraTargetPrefab;
    public GameObject depthMeshObj;
    [HideInInspector]
    public int leftMeshCount = -1;
    private Queue<MeshFilter> depthMeshes = new Queue<MeshFilter>();
    public MeshCollider originalMesh;
    public InputProcess inputProcess;
    public DepthMeshCollider depthMeshCollider;
    private MeshBoundsVisualizer boundsVisualizer;

    private void Start() 
    {
        boundsVisualizer = GetComponent<MeshBoundsVisualizer>();

        inputProcess.panoramaTargetDetectSubject.Subscribe(OnPanoramaTargetDetected);
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
        if (leftMeshCount <= 0) { return; }

        var mesh = depthMeshes.Dequeue();
        mesh.sharedMesh = (Mesh)Instantiate(originalMesh.sharedMesh);
        depthMeshes.Enqueue(mesh);
        leftMeshCount--;

        if (leftMeshCount == 0)
        {
            GenerateMeshBounds();
        }
    }

    private void OnPanoramaTargetDetected(Vector3 pos)
    {
        depthMeshCollider.ShootProjectile();
    }

    private void GenerateMeshBounds()
    {
        var bounds = depthMeshes.Peek().GetComponent<Renderer>().bounds;
        foreach(var meshFilter in depthMeshes)
        {
            var renderer = meshFilter.GetComponent<Renderer>();
            var bound = renderer.bounds;
            bounds.Encapsulate(bound);
        }
        boundsVisualizer.Visualize(bounds);
        CloudAnchorMgr.Singleton.DebugLog("All panorama targets are detected. Create bounding box");
        CloudAnchorMgr.Singleton.DebugLog($"Box extents: {bounds.extents}");
    }

    public void ToggleDepthMeshRenderer()
    {
        var projectileMeshRenderer = depthMeshCollider.GetComponent<Renderer>();
        var current = projectileMeshRenderer.enabled;

        projectileMeshRenderer.enabled = !current;
        foreach(var meshFilter in depthMeshes)
        {
            var renderer = meshFilter.GetComponent<Renderer>();
            renderer.enabled = !current;
        }
    }
}
