using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class VRPointerCounter : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public MeshRenderer MeshRenderer => GetComponent<MeshRenderer>();

    public  bool counting = false;
    public int timesClicked = 0;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (counting && timesClicked < 20) timesClicked++;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        MeshRenderer.material.color = Color.green;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        MeshRenderer.material.color = Color.red;
    }
}
