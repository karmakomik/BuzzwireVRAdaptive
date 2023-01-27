using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UISliderScript : MonoBehaviour, IPointerClickHandler
{   
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("Clicked" + gameObject.name);
    }
}
