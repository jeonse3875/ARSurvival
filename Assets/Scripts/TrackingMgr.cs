using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public enum TrackingImage
{
    Door, Window
}

public class TrackingMgr : MonoBehaviour
{
    [SerializeField]
    ARTrackedImageManager m_TrackedImageManager;
    public GameObject doorPrefab;
    public GameObject windowPrefab;
    private Dictionary<TrackingImage, GameObject> instances = new Dictionary<TrackingImage, GameObject>();
    private ARPlaneManager planeManager;
    private bool isPlaneActive = true;

    void OnEnable() => m_TrackedImageManager.trackedImagesChanged += OnChanged;

    void OnDisable() => m_TrackedImageManager.trackedImagesChanged -= OnChanged;

    private void Start() 
    {
        planeManager = GetComponent<ARPlaneManager>();

        instances[TrackingImage.Door] = Instantiate(doorPrefab);
        instances[TrackingImage.Window] = Instantiate(windowPrefab);

        instances[TrackingImage.Door].SetActive(false);
        instances[TrackingImage.Window].SetActive(false);
    }

    private void Update()
    {
        foreach (var plane in planeManager.trackables) { plane.gameObject.SetActive(isPlaneActive); }
    }

    void OnChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (var newImage in eventArgs.added)
        {
            var key = ToEnum(newImage.referenceImage.name);
            instances[key].transform.position = newImage.transform.position;
            instances[key].transform.rotation = newImage.transform.rotation;
            instances[key].transform.SetParent(newImage.transform);
            instances[key].SetActive(true);
        }

        foreach (var updatedImage in eventArgs.updated)
        {
            var key = ToEnum(updatedImage.referenceImage.name);
            if (updatedImage.trackingState == TrackingState.None || updatedImage.trackingState == TrackingState.Limited)
            {
                instances[key].SetActive(false);
            }
            else if (updatedImage.trackingState == TrackingState.Tracking)
            {
                instances[key].SetActive(true);
            }
        }

        foreach (var removedImage in eventArgs.removed)
        {
            var key = ToEnum(removedImage.referenceImage.name);
            instances[key].SetActive(false);
        }
    }

    TrackingImage ToEnum(string str)
    {
        return (TrackingImage)Enum.Parse(typeof(TrackingImage), str);
    }

    public void ToggleARPlane()
    {
        isPlaneActive = !isPlaneActive;
    }
}
