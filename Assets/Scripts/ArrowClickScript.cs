using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowClickScript : MonoBehaviour
{
    public ExperimentManagerScript experimentManagerScript;
    //Vector3 speedSEsliderHandleInitPos, mistakesSEsliderHandleInitPos;

    public GameObject touchingObj;
    public Vector3 clickLoc;

    public GameObject speedSEsliderHandle, mistakesSEsliderHandle;
    public GameObject speedSEsliderLine, mistakesSEsliderLine;

    public float xMin, xMax;

    bool hasSpeedSEChanged, hasMistakesSEChanged;

    // Start is called before the first frame update
    void Start()
    {
        //experimentManagerScript = experimentManagerObj.GetComponent<ExperimentManagerScript>();
        //speedSEsliderHandleInitPos = speedSEsliderHandle.transform.position;
        //mistakesSEsliderHandleInitPos = mistakesSEsliderHandle.transform.position;
        touchingObj = null;
        hasSpeedSEChanged = false;
        hasMistakesSEChanged = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter(Collider other)
    {
        //Debug.Log("Arrow Clicked " + other.gameObject.name);

        touchingObj = other.gameObject;

        if (touchingObj == experimentManagerScript.okButtonObj)
        {
            //Debug.Log("OK Button clicked");
            experimentManagerScript.okButtonClicked();
        }
        else if (touchingObj == experimentManagerScript.seOkButtonObj)
        {
            //Debug.Log("SE OK Button clicked");
            experimentManagerScript.seOkButtonClicked();
        }
    }

    public void resetSlider()
    {
        speedSEsliderHandle.transform.position = speedSEsliderLine.transform.position;
        mistakesSEsliderHandle.transform.position = mistakesSEsliderLine.transform.position;
        hasSpeedSEChanged = false;
        hasMistakesSEChanged = false;
        experimentManagerScript.seOkButtonObj.SetActive(false);
    }

    void OnTriggerStay(Collider other)
    {
        if (other.gameObject.name == "SpeedSESliderLine")
        {
            clickLoc = other.gameObject.GetComponent<Collider>().ClosestPointOnBounds(transform.position);           
            speedSEsliderHandle.transform.position = new Vector3(clickLoc.x, speedSEsliderHandle.transform.position.y, speedSEsliderHandle.transform.position.z);
            hasSpeedSEChanged = true;
        }
        else if (other.gameObject.name == "MistakeSESliderLine")
        {
            clickLoc = other.gameObject.GetComponent<Collider>().ClosestPointOnBounds(transform.position);
            mistakesSEsliderHandle.transform.position = new Vector3(clickLoc.x, mistakesSEsliderHandle.transform.position.y, mistakesSEsliderHandle.transform.position.z);
            hasMistakesSEChanged = true;
        }

        if (hasSpeedSEChanged && hasMistakesSEChanged)
        {
            experimentManagerScript.seOkButtonObj.SetActive(true);
            //experimentManagerScript.surveyPanel.SetActive(false);
        }

        //formHandler.moveCrossToX((int)sliderHandle.transform.position.x);
    }

    void OnTriggerExit(Collider other)
    {
        touchingObj = null;
    }
}
