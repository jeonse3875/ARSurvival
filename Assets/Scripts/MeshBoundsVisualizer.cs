using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshBoundsVisualizer : MonoBehaviour
{
    public GameObject boundPointIndicator;

    public void Visualize(Bounds bounds)
    {
        var center = bounds.center;
        var extents = bounds.extents;

        var frontTopLeft = new Vector3(center.x - extents.x, center.y + extents.y, center.z - extents.z);
        var frontTopRight = new Vector3(center.x + extents.x, center.y + extents.y, center.z - extents.z);
        var frontBottomLeft = new Vector3(center.x - extents.x, center.y - extents.y, center.z - extents.z);
        var frontBottomRight = new Vector3(center.x + extents.x, center.y - extents.y, center.z - extents.z);
        var backTopLeft = new Vector3(center.x - extents.x, center.y + extents.y, center.z + extents.z);
        var backTopRight = new Vector3(center.x + extents.x, center.y + extents.y, center.z + extents.z);
        var backBottomLeft = new Vector3(center.x - extents.x, center.y - extents.y, center.z + extents.z);
        var backBottomRight = new Vector3(center.x + extents.x, center.y - extents.y, center.z + extents.z);

        Instantiate(boundPointIndicator,frontTopLeft, Quaternion.identity);
        Instantiate(boundPointIndicator,frontTopRight, Quaternion.identity);
        Instantiate(boundPointIndicator,frontBottomLeft, Quaternion.identity);
        Instantiate(boundPointIndicator,frontBottomRight, Quaternion.identity);
        Instantiate(boundPointIndicator,backTopLeft, Quaternion.identity);
        Instantiate(boundPointIndicator,backTopRight, Quaternion.identity);
        Instantiate(boundPointIndicator,backBottomLeft, Quaternion.identity);
        Instantiate(boundPointIndicator,backBottomRight, Quaternion.identity);
    }
}
