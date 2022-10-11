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

    private void Start()
    {
        raycastManager = GetComponent<ARRaycastManager>();

        phase = InputPhase.CloudAnchor;
    }

    private void Update()
    {
        GetTouchInput();
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
                case InputPhase.NPCAnchor:
                    nPCAnchorHitSubject.OnNext(hits[0]);
                    break;
                default:
                    break;
            }
        }
    }
}
