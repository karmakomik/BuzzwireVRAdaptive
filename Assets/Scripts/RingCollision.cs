using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class RingCollision : MonoBehaviour
{
    public GameObject experimentController;
    ExperimentManagerScript experimentControllerScript;
    Collider currCollider;
    Collider oldCollider;
    Vector3 loc;
    // GameObject startStopLight;
    int numCollidersInContact;
    //GameObject mistakeLineObj;
    private Vector3 mistakeVector;
    Vector3 mistakeDirection;
    int levelStartTime;
    //public GameObject hapticPointer;
    Vector3 normForceVector, prevNormForceVector;

    int partNumAtTimeOfDetaching = 0, partNumAtTimeOfReattaching = 0;
   

    // Start is called before the first frame update
    void Start()
    {
        levelStartTime = 0;
        numCollidersInContact = 0;
        experimentControllerScript = experimentController.GetComponent<ExperimentManagerScript>();
    }

    float hapticDistLimit = 0.03f;


    // Update is called once per frame
    void Update()
    {
        if (experimentControllerScript.isFeedbackOnNow)
        {
            //mistakeLineObj.GetComponent<LineRenderer>().;
            //Set the start and end points of the line
            experimentControllerScript.mistakeLineObj.GetComponent<LineRenderer>().SetPosition(0, transform.position);
            experimentControllerScript.mistakeLineObj.GetComponent<LineRenderer>().SetPosition(1, experimentControllerScript.projectedHookPos);
            mistakeVector = experimentControllerScript.projectedHookPos - transform.position;
            //print("mistakeVector.magnitude" + mistakeVector.magnitude);
            float distance = mistakeVector.magnitude;
            float intensity = math.remap(0, 0.1f, 0, 1, distance);
            float clampedIntensity = math.clamp(intensity, 0, 1);
            if (clampedIntensity < 0.4f)
            {
                experimentControllerScript.mistakeLineObj.GetComponent<LineRenderer>().startColor = Color.black;
                experimentControllerScript.mistakeLineObj.GetComponent<LineRenderer>().endColor = Color.black;
            }
            else
            {
                experimentControllerScript.mistakeLineObj.GetComponent<LineRenderer>().startColor = Color.red;
                experimentControllerScript.mistakeLineObj.GetComponent<LineRenderer>().endColor = Color.red;
            }
            //experimentControllerScript.changeIntensityOfGhost(clampedIntensity);

            //print("distance - " + distance);



            //print("mistakeDirection - " + mistakeDirection);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        ++numCollidersInContact;
        //Debug.Log("Object in contact - " + other.gameObject);
        if (other.tag != "StartZone" && other.tag != "StopZone" && experimentControllerScript.feedbackEnabled)
        {
            if (experimentControllerScript.feedbackEnabled)
            {
                if (other.gameObject.name.StartsWith("Part"))
                {
                    partNumAtTimeOfReattaching = int.Parse(other.gameObject.name.Substring(4));

                    if (partNumAtTimeOfReattaching == partNumAtTimeOfDetaching || partNumAtTimeOfReattaching == partNumAtTimeOfDetaching + 1)
                    {
                        //print("Reattaching to correct part");
                        experimentControllerScript.doControllerReattachOperations(other.gameObject.tag);
                        experimentControllerScript.stopMistakeFeedback();
                        experimentControllerScript.mistakeLineObj.SetActive(false);
                    }
                    else
                    {
                        print("Reattaching to wrong part");
                    }
                }
            }
            else
            {
                /*experimentControllerScript.doControllerReattachOperations(other.gameObject.tag);
                experimentControllerScript.stopMistakeFeedback();
                mistakeLineObj.SetActive(false);*/
            }
        }
        else if (other.tag == "StopZone")
        {
            experimentControllerScript.doControllerReattachOperations("null");
            experimentControllerScript.mistakeLineObj.SetActive(false);
            experimentControllerScript.stopMistakeFeedback();
            //experimentControllerScript.changeIntensityOfGhost(1);
            experimentControllerScript.feedbackEnabled = false;
            //experimentControllerScript.startStopRefController.SetActive(true);
            //experimentControllerScript.startStopRefController.transform.position = experimentControllerScript.stopPositions[experimentControllerScript.currLevel - 1];
            experimentControllerScript.solidRightHandController.SetActive(false);
            experimentControllerScript.ghostRightHandController.SetActive(true);
        }
        else if (other.tag == "StartZone")
        {
            experimentControllerScript.doControllerReattachOperations("null");
            experimentControllerScript.mistakeLineObj.SetActive(false);
            experimentControllerScript.feedbackEnabled = false;
            //experimentControllerScript.startStopRefController.SetActive(false);            
            experimentControllerScript.solidRightHandController.SetActive(true);
            experimentControllerScript.ghostRightHandController.SetActive(false);
        }
            
        
    }


    private void OnTriggerStay(Collider other)
    {

        if (other.tag != "StartZone" && other.tag != "StopZone")
        {
            //startStopLight.SetActive(false);
            //Debug.Log("Collision with " + other.gameObject);
            loc = other.gameObject.GetComponent<Collider>().ClosestPointOnBounds(transform.position);
            //Debug.Log(loc);       
        }
        else if(other.tag == "StartZone")
        {
            //startStopLight.SetActive(true);
            experimentControllerScript.stopMistakeFeedback();
            experimentControllerScript.mistakeLineObj.SetActive(false);
            if (experimentControllerScript.client != null && experimentControllerScript.expState != ExperimentState.VR_TUTORIAL)
                experimentControllerScript.client.Write("M;1;;;LeftSwitchPressedVR;\r\n");
        }
        else if (other.tag == "StopZone")
        {
            //startStopLight.SetActive(true);
            experimentControllerScript.stopMistakeFeedback();
            experimentControllerScript.mistakeLineObj.SetActive(false);
            if (experimentControllerScript.client != null && experimentControllerScript.expState != ExperimentState.VR_TUTORIAL)
                experimentControllerScript.client.Write("M;1;;;RightSwitchPressedVR;\r\n");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        --numCollidersInContact;
        if (other.tag != "StartZone" && other.tag != "StopZone" && experimentControllerScript.feedbackEnabled)
        {
            //Debug.Log("Haptic pointer z angle " + hapticPointer.transform.localEulerAngles.z);
            if (numCollidersInContact < 1 && !experimentControllerScript.isFeedbackOnNow)
            {
                //Debug.Log("Number of colliders in contact is less than 1. Triggering feedback");
                //currCollider = null;
                if (other.gameObject.name.StartsWith("Part"))
                {
                    partNumAtTimeOfDetaching = int.Parse(other.gameObject.name.Substring(4));

                    experimentControllerScript.doControllerDetachOperations((CapsuleCollider)other, other.gameObject.tag, loc);
                    //experimentControllerScript.triggerMistakeFeedback();
                    //Find the vector between the two points
                    mistakeVector = experimentControllerScript.projectedHookPos - transform.position;
                    //experimentControllerScript.changeIntensityOfGhost(math.remap(0, 127, 0, 1, mistakeVector.magnitude));

                    mistakeDirection = mistakeVector.normalized;
                    //float mistakeDepth = mistakeVector.magnitude;
                    experimentControllerScript.triggerMistakeFeedback(other.tag.ToString(), mistakeDirection, mistakeVector);
                    
                }
            }
        }
        else if (other.tag == "StartZone")
        {
            Debug.Log("Level started!");
            levelStartTime = (int)Time.time;
            //if(!experimentControllerScript.tutorialPhase) other.enabled = false;
            experimentControllerScript.feedbackEnabled = true;
            //experimentControllerScript.startStopRefController.SetActive(false);
            experimentControllerScript.solidRightHandController.SetActive(true);
            experimentControllerScript.ghostRightHandController.SetActive(false);
        }
        else if (other.tag == "StopZone")
        {
            Debug.Log("Level finished!");
            if(experimentControllerScript.expState != ExperimentState.VR_TUTORIAL)
                experimentControllerScript.showLevelResult(((int)Time.time - levelStartTime));
            //startStopLight.SetActive(true);
            experimentControllerScript.feedbackEnabled = false;
            //experimentControllerScript.startStopRefController.SetActive(true);
            experimentControllerScript.solidRightHandController.SetActive(false);
            experimentControllerScript.ghostRightHandController.SetActive(true);
        }
        
    }

    /*private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(loc, 0.005f);
    }*/
}
