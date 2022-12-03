using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCCtrl : MonoBehaviour
{
    public Transform origin;
    public GameObject blue;
    public GameObject red;

    private void Start()
    {
        origin = transform.Find("Origin");
    }

    public void OnDoorOpen()
    {
        red.SetActive(true);
        blue.SetActive(false);
    }

    public void OnDoorClose()
    {
        red.SetActive(false);
        blue.SetActive(true);
    }
}
