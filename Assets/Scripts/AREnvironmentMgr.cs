using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using g3;
using System.Linq;

public struct OBB
{
    public Vector3[] vertices;
    public Vector3 center;
    public Vector3 extent;
    public Vector3 axisX;
    public Vector3 axisY;
    public Vector3 axisZ;
}

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
    [HideInInspector]
    public OBB oBB;
    public List<DynamicPropSO> propList = new List<DynamicPropSO>();

    private void Start()
    {
        boundsVisualizer = GetComponent<MeshBoundsVisualizer>();

        inputProcess.panoramaTargetDetectSubject.Subscribe(OnPanoramaTargetDetected);
    }

    private float GetHorizontalFoV()
    {
        return Camera.VerticalToHorizontalFieldOfView(Camera.main.fieldOfView, Camera.main.aspect);
    }

    public void StartPanorama()
    {
        var fov = GetHorizontalFoV();

        int count = Mathf.CeilToInt(360f / fov);
        float angle = 360f / count;

        var origin = Camera.main.transform.position;
        var euler = Camera.main.transform.rotation.eulerAngles;
        euler.x = 0f;
        euler.z = 0f;
        var rot = Quaternion.Euler(euler);
        CloudAnchorMgr.Singleton.DebugLog($"Create panoram target. Angle: {angle}, Count: {count}");

        for (int i = 0; i < count; i++)
        {
            var targetPos = origin + rot * Vector3.forward;
            Instantiate(cameraTargetPrefab, targetPos, Quaternion.identity);
            var mesh = Instantiate(depthMeshObj, Vector3.zero, Quaternion.identity);
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
            GenerateOBB();
            AddPropToSpawningPool();
        }
    }

    private void OnPanoramaTargetDetected(Vector3 pos)
    {
        depthMeshCollider.ShootProjectile();
    }

    private void GenerateOBB()
    {
        var vertices = new List<Vector3>();
        foreach (var meshFilter in depthMeshes)
        {
            // have to multiply the vertices' positions
            // with the lossyScale and add it to the transform.position 
            vertices.AddRange(meshFilter.sharedMesh.vertices.Select(vertex => meshFilter.transform.position + Vector3.Scale(vertex, meshFilter.transform.lossyScale)));
        }

        var points3d = new Vector3d[vertices.Count];

        for (var i = 0; i < vertices.Count; i++)
        {
            points3d[i] = vertices[i];
        }

        // BOOM MAGIC!!!
        var orientedBoundingBox = new ContOrientedBox3(points3d);

        // Now just convert the information back to Unity Vector3 positions and axis
        // Since g3.Vector3d uses doubles but Unity Vector3 uses floats
        // we have to explicitly cast to Vector3
        var center = (Vector3)orientedBoundingBox.Box.Center;

        var axisX = (Vector3)orientedBoundingBox.Box.AxisX;
        var axisY = (Vector3)orientedBoundingBox.Box.AxisY;
        var axisZ = (Vector3)orientedBoundingBox.Box.AxisZ;
        var extends = (Vector3)orientedBoundingBox.Box.Extent;

        // Now we can simply calculate our 8 vertices of the bounding box
        var A = center - extends.z * axisZ - extends.x * axisX - axisY * extends.y;
        var B = center - extends.z * axisZ + extends.x * axisX - axisY * extends.y;
        var C = center - extends.z * axisZ + extends.x * axisX + axisY * extends.y;
        var D = center - extends.z * axisZ - extends.x * axisX + axisY * extends.y;

        var E = center + extends.z * axisZ - extends.x * axisX - axisY * extends.y;
        var F = center + extends.z * axisZ + extends.x * axisX - axisY * extends.y;
        var G = center + extends.z * axisZ + extends.x * axisX + axisY * extends.y;
        var H = center + extends.z * axisZ - extends.x * axisX + axisY * extends.y;

        // And finally visualize it
        Gizmos.DrawLine(A, B);
        Gizmos.DrawLine(B, C);
        Gizmos.DrawLine(C, D);
        Gizmos.DrawLine(D, A);

        Gizmos.DrawLine(E, F);
        Gizmos.DrawLine(F, G);
        Gizmos.DrawLine(G, H);
        Gizmos.DrawLine(H, E);

        Gizmos.DrawLine(A, E);
        Gizmos.DrawLine(B, F);
        Gizmos.DrawLine(D, H);
        Gizmos.DrawLine(C, G);

        oBB.vertices = new Vector3[] { A, B, C, D, E, F, G, H };
        oBB.center = center;
        oBB.extent = extends;
        oBB.axisX = axisX;
        oBB.axisY = axisY;
        oBB.axisZ = axisZ;

        boundsVisualizer.Visualize(oBB);

        CloudAnchorMgr.Singleton.DebugLog("All panorama targets are detected. Create bounding box");
        CloudAnchorMgr.Singleton.DebugLog($"Bounds Volume: {GetBoundsVolume()}");
    }

    public void AddPropToSpawningPool()
    {
        foreach(var prop in propList)
        {
            var count = prop.GetSpawnCount(GetBoundsVolume());
            var posList = prop.GetSpawnPos(oBB,count);
            CloudAnchorMgr.Singleton.DebugLog($"[SpawningPool] {count} {prop.name}");
            foreach(var pos in posList)
            {
                //inputProcess.propSpawningPool.Add((pos,prop.prefab));
                Instantiate(prop.prefab,pos,Quaternion.identity);
            }
        }
    }

    public void ToggleDepthMeshRenderer()
    {
        var projectileMeshRenderer = depthMeshCollider.GetComponent<Renderer>();
        var current = projectileMeshRenderer.enabled;

        projectileMeshRenderer.enabled = !current;
        foreach (var meshFilter in depthMeshes)
        {
            var renderer = meshFilter.GetComponent<Renderer>();
            renderer.enabled = !current;
        }
    }

    public float GetBoundsVolume()
    {
        var volume = oBB.extent.x*2 * oBB.extent.y*2 * oBB.extent.z*2;
        return volume;
    }
}
