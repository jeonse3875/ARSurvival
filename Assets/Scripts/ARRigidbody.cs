using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ARRigidbody : MonoBehaviour
{
    private Rigidbody rb;

    private void Start() 
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        FixObject();
    }

    private void FixObject()
    {
        if (rb.IsSleeping() && !rb.isKinematic && CloudAnchorMgr.Singleton.cloudAnchor != null)
        {
            rb.isKinematic = true;
            transform.SetParent(CloudAnchorMgr.Singleton.cloudAnchor.transform);
        }
    }
}
