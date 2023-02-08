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
    int levelStartTime, levelEndTime;
    GameObject _mistakePrimerObj;

    //public GameObject hapticPointer;
    Vector3 normForceVector, prevNormForceVector;

    int partNumAtTimeOfDetaching = 0, partNumAtTimeOfReattaching = 0;
   

    // Start is called before the first frame update
    void Start()
    {
        levelStartTime = levelEndTime = 0;
        numCollidersInContact = 0;
        experimentControllerScript = experimentController.GetComponent<ExperimentManagerScript>();
        _mistakePrimerObj = experimentControllerScript.mistakePrimer;
    }

    // Update is called once per frame
    void Update()
    {
        new Ray(experimentControllerScript.solidRightHandController.transform.position, experimentControllerScript.solidRightHandController.transform.forward);
        
        if (experimentControllerScript.isFeedbackOnNow)
        {
            //Set the start and end points of the line
            experimentControllerScript.mistakeLineObj.GetComponent<LineRenderer>().SetPosition(0, transform.position);
            experimentControllerScript.mistakeLineObj.GetComponent<LineRenderer>().SetPosition(1, experimentControllerScript.projectedHookPos);
            mistakeVector = experimentControllerScript.projectedHookPos - transform.position;
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
        }
        else if (other.tag == "StopZone")
        {
            experimentControllerScript.doControllerReattachOperations("null");
            experimentControllerScript.mistakeLineObj.SetActive(false);
            experimentControllerScript.stopMistakeFeedback();

            experimentControllerScript.feedbackEnabled = false;
            experimentControllerScript.solidRightHandController.SetActive(false);
            experimentControllerScript.ghostRightHandController.SetActive(true);
        }
        else if (other.tag == "StartZone")
        {
            experimentControllerScript.doControllerReattachOperations("null");
            experimentControllerScript.mistakeLineObj.SetActive(false);
            experimentControllerScript.feedbackEnabled = false;          
            experimentControllerScript.solidRightHandController.SetActive(true);
            experimentControllerScript.ghostRightHandController.SetActive(false);
        }
    }


    private void OnTriggerStay(Collider other)
    {
        if (other.tag != "StartZone" && other.tag != "StopZone")
        {            
            //Debug.Log("Collision with " + other.gameObject);
            loc = other.gameObject.GetComponent<Collider>().ClosestPointOnBounds(transform.position);
            //_mistakePrimerObj.transform.position = loc;
            string dragDir = other.gameObject.tag;

            //bool firstColliderDetected = false, lastColliderDetected = false;
            int indexOfThisCollider = experimentControllerScript.colliderList.IndexOf(other.gameObject);

            float primerLocX = _mistakePrimerObj.transform.position.x;
            float primerLocY = _mistakePrimerObj.transform.position.y;
            float primerLocZ = _mistakePrimerObj.transform.position.z;
            GameObject startAnchorObj = other.gameObject.GetComponent<ColliderExtraInfoScript>().start;
            GameObject endAnchorObj = other.gameObject.GetComponent<ColliderExtraInfoScript>().end;
            float startAnchorLocX = startAnchorObj.transform.position.x;
            float startAnchorLocY = startAnchorObj.transform.position.y;
            float startAnchorLocZ = startAnchorObj.transform.position.z;
            float endAnchorLocX = endAnchorObj.transform.position.x;
            float endAnchorLocY = endAnchorObj.transform.position.y;
            float endAnchorLocZ = endAnchorObj.transform.position.z;
            float tiltAngleDelta = 0;

            if (experimentControllerScript.expState == ExperimentState.MISTAKE_TRAINING)
            {
                //Set rotation and position of mistake primer
                if (dragDir == "x-axis")
                {
                    //Set rotation of the mistake primer
                    if (Mathf.Abs(primerLocX - startAnchorLocX) < 0.005f) //Check if ring is close to the beginning of the wire segment
                    {
                        //Scale tiltAngleDelta proportional to the difference between primerLocX and startLocX
                        if (startAnchorObj.name != "Anchor1") //Check if we are at the beginning of the wire so as not to have the primer rotate
                        {
                            if (experimentControllerScript.colliderList[indexOfThisCollider - 1].tag == "y-axis")
                            {
                                tiltAngleDelta = math.remap(0, 0.005f, 45, 0, Mathf.Abs(primerLocX - startAnchorLocX));
                                _mistakePrimerObj.transform.rotation = Quaternion.Euler(0, 0, -tiltAngleDelta + 0);
                            }
                            else if (experimentControllerScript.colliderList[indexOfThisCollider - 1].tag == "z-axis")
                            {
                                tiltAngleDelta = math.remap(0, 0.005f, 45, 0, Mathf.Abs(primerLocX - startAnchorLocX));
                                _mistakePrimerObj.transform.rotation = Quaternion.Euler(0, tiltAngleDelta + 0, 0);
                            }
                        }
                    }
                    else if (Mathf.Abs(primerLocX - endAnchorLocX) < 0.005f) //Check if ring is close to the end of the wire segment
                    {
                        if (endAnchorObj.name != "Anchor38") //Check if we are at the end of the wire so as not to have the primer rotate
                        {
                            if (experimentControllerScript.colliderList[indexOfThisCollider + 1].tag == "y-axis")
                            {
                                tiltAngleDelta = math.remap(0, 0.005f, 45, 0, Mathf.Abs(primerLocX - endAnchorLocX));
                                _mistakePrimerObj.transform.rotation = Quaternion.Euler(0, 0, tiltAngleDelta + 0);
                            }
                            else if (experimentControllerScript.colliderList[indexOfThisCollider + 1].tag == "z-axis")
                            {
                                tiltAngleDelta = math.remap(0, 0.005f, 45, 0, Mathf.Abs(primerLocX - endAnchorLocX));
                                _mistakePrimerObj.transform.rotation = Quaternion.Euler(0, tiltAngleDelta + 0, 0);
                            }
                        }
                    }
                    else
                    {
                        _mistakePrimerObj.transform.rotation = Quaternion.Euler(0, 0, 0);
                    }

                    //Set position of mistakePrimer
                    _mistakePrimerObj.transform.position = new Vector3(loc.x, other.gameObject.transform.position.y, other.gameObject.transform.position.z);
                }
                else if (dragDir == "y-axis")
                {
                    if (Mathf.Abs(primerLocY - startAnchorLocY) < 0.005f)
                    {
                        //Scale tiltAngleDelta proportional to the difference between primerLocX and startLocX
                        if (experimentControllerScript.colliderList[indexOfThisCollider - 1].tag == "x-axis")
                        {
                            tiltAngleDelta = math.remap(0, 0.005f, 45, 0, Mathf.Abs(primerLocY - startAnchorLocY));
                            _mistakePrimerObj.transform.rotation = Quaternion.Euler(0, 0, -tiltAngleDelta + 90);
                        }
                        else if (experimentControllerScript.colliderList[indexOfThisCollider - 1].tag == "z-axis")
                        {
                            tiltAngleDelta = math.remap(0, 0.005f, 45, 0, Mathf.Abs(primerLocY - startAnchorLocY));
                            _mistakePrimerObj.transform.rotation = Quaternion.Euler(-tiltAngleDelta + 0, 0, 90);
                        }
                    }
                    else if (Mathf.Abs(primerLocY - endAnchorLocY) < 0.005f)
                    {
                        if (experimentControllerScript.colliderList[indexOfThisCollider + 1].tag == "z-axis")
                        {
                            tiltAngleDelta = math.remap(0, 0.005f, 45, 0, Mathf.Abs(primerLocY - endAnchorLocY));
                            _mistakePrimerObj.transform.rotation = Quaternion.Euler(tiltAngleDelta + 0, 0, 90);
                        }
                        if (experimentControllerScript.colliderList[indexOfThisCollider + 1].tag == "x-axis")
                        {
                            tiltAngleDelta = math.remap(0, 0.005f, 45, 0, Mathf.Abs(primerLocY - endAnchorLocY));
                            _mistakePrimerObj.transform.rotation = Quaternion.Euler(0, 0, tiltAngleDelta + 90);
                        }
                    }
                    else
                    {
                        _mistakePrimerObj.transform.rotation = Quaternion.Euler(0, 0, 90);
                    }

                    _mistakePrimerObj.transform.position = new Vector3(other.gameObject.transform.position.x + 0.015f, loc.y, other.gameObject.transform.position.z);
                }
                else if (dragDir == "z-axis")
                {
                    if (Mathf.Abs(primerLocZ - startAnchorLocZ) < 0.005f)
                    {
                        if (experimentControllerScript.colliderList[indexOfThisCollider - 1].tag == "y-axis")
                        {
                            tiltAngleDelta = math.remap(0, 0.005f, 45, 0, Mathf.Abs(primerLocZ - startAnchorLocZ));
                            _mistakePrimerObj.transform.rotation = Quaternion.Euler(0, 90, -tiltAngleDelta + 0);
                        }
                        else if (experimentControllerScript.colliderList[indexOfThisCollider - 1].tag == "x-axis")
                        {
                            tiltAngleDelta = math.remap(0, 0.005f, 45, 0, Mathf.Abs(primerLocZ - startAnchorLocZ));
                            _mistakePrimerObj.transform.rotation = Quaternion.Euler(0, -tiltAngleDelta + 90, 0);
                        }
                    }
                    else if (Mathf.Abs(primerLocZ - endAnchorLocZ) < 0.005f)
                    {
                        if (experimentControllerScript.colliderList[indexOfThisCollider + 1].tag == "y-axis")
                        {
                            tiltAngleDelta = math.remap(0, 0.005f, 45, 0, Mathf.Abs(primerLocZ - endAnchorLocZ));
                            _mistakePrimerObj.transform.rotation = Quaternion.Euler(0, 90, tiltAngleDelta + 0);
                        }
                        else if (experimentControllerScript.colliderList[indexOfThisCollider + 1].tag == "x-axis")
                        {
                            tiltAngleDelta = math.remap(0, 0.005f, 45, 0, Mathf.Abs(primerLocZ - endAnchorLocZ));
                            _mistakePrimerObj.transform.rotation = Quaternion.Euler(0, -tiltAngleDelta + 90, 0);
                        }
                    }
                    else
                    {
                        _mistakePrimerObj.transform.rotation = Quaternion.Euler(0, 90, 0);
                    }

                    _mistakePrimerObj.transform.position = new Vector3(other.gameObject.transform.position.x + 0.015f, other.gameObject.transform.position.y, loc.z);
                }
            }
        }
        else if (other.tag == "StartZone")
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
            if (experimentControllerScript.expState != ExperimentState.VR_TUTORIAL)
            {
                levelEndTime = (int)Time.time;
                if (experimentControllerScript.expState == ExperimentState.MISTAKE_TRAINING || experimentControllerScript.expState == ExperimentState.SPEED_TRAINING)
                    experimentControllerScript.lastTrainingIterationSpeed = (float)57 / (float)(levelEndTime - levelStartTime);
                else if (experimentControllerScript.expState == ExperimentState.VR_PRE_TEST)
                    experimentControllerScript.lastTrainingIterationSpeed = (float)52 / (float)(levelEndTime - levelStartTime); 
                //experimentControllerScript.lastTrainingIterationSpeed = (float) 57 / (float)(levelEndTime - levelStartTime);
                //experimentControllerScript.lastTrainingIterationMistakeTime = experimentControllerScript.mistakeEndTime - experimentControllerScript.mistakeStartTime;

                //Debug.Log("Last TCT was - " + (float)(levelEndTime - levelStartTime));
                Debug.Log("Last speed was - " + experimentControllerScript.lastTrainingIterationSpeed);
                
                experimentControllerScript.changeSpeedTxt();
                experimentControllerScript.showLevelResult(((int)Time.time - levelStartTime));

                experimentControllerScript.changeState("TRAINING_SELF_EFFICACY");
                //experimentControllerScript.surveyPanel.SetActive(true);
                //experimentControllerScript.expState = ExperimentState.TRAINING_SELF_EFFICACY;

            }
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
