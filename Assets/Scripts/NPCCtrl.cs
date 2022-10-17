using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCCtrl : MonoBehaviour
{
    public Transform origin;

    private void Start()
    {
        origin = transform.Find("Origin");
    }
}
