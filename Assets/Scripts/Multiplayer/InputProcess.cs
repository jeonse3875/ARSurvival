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

    private void Start()
    {
        raycastManager = GetComponent<ARRaycastManager>();

        phase = InputPhase.CloudAnchor;
    }

    private void Update()
    {
        GetTouchInput();
        CheckPanorama();
    }

    private void CheckPanorama()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3 (0.5f, 0.5f, 0));
        RaycastHit hit;
        if (Physics.Raycast(ray,out hit))
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
}
