using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crosshair : MonoBehaviour
{
    private static Crosshair instance;
    public static Crosshair Instance
    {
        get
        {
            if (!instance) instance = FindObjectOfType<Crosshair>();
            return instance;
        }
    }

    public bool touchingController = false;

    public void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("GameController"))
        {
            touchingController = true;
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("GameController"))
        {
            touchingController = false;
        }
    }
}
