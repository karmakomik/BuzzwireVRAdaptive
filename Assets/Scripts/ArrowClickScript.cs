using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowClickScript : MonoBehaviour
{
    Vector3 speedSEsliderHandleInitPos, mistakesSEsliderHandleInitPos;

    public GameObject touchingObj;
    public Vector3 clickLoc;

    public GameObject speedSEsliderHandle, mistakesSEsliderHandle;

    public float xMin, xMax;

    // Start is called before the first frame update
    void Start()
    {
        speedSEsliderHandleInitPos = speedSEsliderHandle.transform.position;
        mistakesSEsliderHandleInitPos = mistakesSEsliderHandle.transform.position;
        touchingObj = null;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter(Collider other)
    {
        //Debug.Log("Arrow Clicked " + other.gameObject.name);

        touchingObj = other.gameObject;
    }

    public void resetSlider()
    {
        speedSEsliderHandle.transform.position = speedSEsliderHandleInitPos;
        mistakesSEsliderHandle.transform.position = mistakesSEsliderHandleInitPos;

    }

    void OnTriggerStay(Collider other)
    {
        if (other.gameObject.name == "SpeedSESliderLine")
        {
            clickLoc = other.gameObject.GetComponent<Collider>().ClosestPointOnBounds(transform.position);           
            speedSEsliderHandle.transform.position = new Vector3(clickLoc.x, speedSEsliderHandle.transform.position.y, speedSEsliderHandle.transform.position.z);
        }
        else if (other.gameObject.name == "MistakeSESliderLine")
        {
            clickLoc = other.gameObject.GetComponent<Collider>().ClosestPointOnBounds(transform.position);
            mistakesSEsliderHandle.transform.position = new Vector3(clickLoc.x, mistakesSEsliderHandle.transform.position.y, mistakesSEsliderHandle.transform.position.z);
        }
        //formHandler.moveCrossToX((int)sliderHandle.transform.position.x);
    }

    void OnTriggerExit(Collider other)
    {
        touchingObj = null;
    }
}
