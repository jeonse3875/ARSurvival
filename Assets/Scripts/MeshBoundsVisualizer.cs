using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshBoundsVisualizer : MonoBehaviour
{
    public GameObject boundPointIndicator;
    public GameObject boundCenterIndicator;
    private LineRenderer lineRenderer;

    public struct OBBArg
    {
        public Vector3[] vertices;
        public Vector3 center;
        public Vector3 extent;
        public Vector3 axisX;
        public Vector3 axisY;
        public Vector3 axisZ;
    }

    private void Start() 
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    public void Visualize(OBBArg args)
    {
        foreach(var vertex in args.vertices)
        {
            Instantiate(boundPointIndicator, vertex, Quaternion.identity);
        }
        Instantiate(boundCenterIndicator, args.center, Quaternion.identity);
        var positions = new Vector3[]
        {args.vertices[0],args.vertices[1],args.vertices[2],args.vertices[3],args.vertices[0],
        args.vertices[4],args.vertices[5],args.vertices[6],args.vertices[7],args.vertices[4],
        args.vertices[5], args.vertices[1],args.vertices[2],args.vertices[6],args.vertices[7],args.vertices[3]};
        lineRenderer.positionCount = positions.Length;
        lineRenderer.SetPositions(positions);
        
        CloudAnchorMgr.Singleton.DebugLog($"OBB/ Center: {args.center}, Extent: {args.extent}");
    }
}
