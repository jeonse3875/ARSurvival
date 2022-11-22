using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UniRx;

public enum InputPhase
{
    None, CloudAnchor, NPCAnchor
}

[RequireComponent(typeof(ARRaycastManager))]
public class InputProcess : MonoBehaviour
{
    private ARRaycastManager raycastManager;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    public InputPhase phase;
    public Subject<ARRaycastHit> cloudAnchorHitSubject = new Subject<ARRaycastHit>();
    public Subject<ARRaycastHit> nPCAnchorHitSubject = new Subject<ARRaycastHit>();
    public Subject<Vector3> panoramaTargetDetectSubject = new Subject<Vector3>();

    public List<(Vector3,GameObject)> propSpawningPool = new List<(Vector3, GameObject)>();

    private void Start()
    {
        raycastManager = GetComponent<ARRaycastManager>();

        phase = InputPhase.CloudAnchor;
    }

    private void Update()
    {
        GetTouchInput();
        CheckPanorama();
        
        foreach((var pos, var prefab) in propSpawningPool)
        {
            if (!IsPointInAngle(pos)) {return;}
            SpawnProp(pos,prefab);
        }
    }

    private void CheckPanorama()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.transform.CompareTag("Panorama"))
            {
                CloudAnchorMgr.Singleton.DebugLog("Find panorama target. Create Mesh from depth.");
                panoramaTargetDetectSubject.OnNext(hit.transform.position);
                Destroy(hit.transform.gameObject);
            }
        }
    }

    private void GetTouchInput()
    {
        if (Input.touchCount < 1) return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase != TouchPhase.Began) return;

        if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) return;

        if (raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
        {
            switch (phase)
            {
                case InputPhase.CloudAnchor:
                    cloudAnchorHitSubject.OnNext(hits[0]);
                    break;
                default:
                    break;
            }
        }

        if (raycastManager.Raycast(touch.position, hits, TrackableType.Depth))
        {
            switch (phase)
            {
                case InputPhase.NPCAnchor:
                    nPCAnchorHitSubject.OnNext(hits[0]);
                    break;
                default:
                    break;
            }
        }
    }

    public bool IsPointInAngle(Vector3 point)
    {
        var viewPos = Camera.main.WorldToViewportPoint(point);

        return viewPos.x >= 0 && viewPos.x <= 1 && viewPos.y >= 0 && viewPos.y <= 1 && viewPos.z > 0;
    }

    public void SpawnProp(Vector3 pos, GameObject prefab)
    {
        Vector2 screenPoint = Camera.main.WorldToScreenPoint(pos);
        if (raycastManager.Raycast(pos, hits, TrackableType.Depth))
        {
            var hit = hits[hits.Count-1];
            Instantiate(prefab,hit.pose.position,Quaternion.identity);

            CloudAnchorMgr.Singleton.DebugLog($"Prop point detected. Spawn {prefab.name} at {pos}");
            propSpawningPool.Remove((pos,prefab));
        }
    }
}
