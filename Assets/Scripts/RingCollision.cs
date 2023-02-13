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

    int numCollidersInContact;

    private Vector3 mistakeVector;
    Vector3 mistakeDirection;
    float levelStartTime, levelEndTime;
    GameObject _mistakePrimerObj;

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
            if (other.gameObject.name.StartsWith("Part") && experimentControllerScript.isDetached)
            {
                partNumAtTimeOfReattaching = int.Parse(other.gameObject.name.Substring(4));
                //print("Part num at time of reattaching - " + partNumAtTimeOfReattaching + " part num at time of detaching - " + partNumAtTimeOfDetaching);

                if (partNumAtTimeOfReattaching == partNumAtTimeOfDetaching || partNumAtTimeOfReattaching == partNumAtTimeOfDetaching + 1)
                {
                    //print("Reattaching to correct part");
                    experimentControllerScript.doControllerReattachOperations(other.gameObject.tag);                    
                    experimentControllerScript.isFeedbackOnNow = false;
                    experimentControllerScript.mistakeLineObj.SetActive(false);
                }
                else
                {
                    print("Reattaching to wrong part");
                }
            }
        }
        else if (other.tag == "StopZone")
        {
            experimentControllerScript.mistakeLineObj.SetActive(false);

            numCollidersInContact = 0;
            partNumAtTimeOfDetaching = 1;
            experimentControllerScript.feedbackEnabled = false;
            experimentControllerScript.isFeedbackOnNow = false;
            experimentControllerScript.solidRightHandController.SetActive(false);
            experimentControllerScript.ghostRightHandController.SetActive(true);

            if (experimentControllerScript.client != null && experimentControllerScript.expState != ExperimentState.VR_TUTORIAL)
                experimentControllerScript.client.Write("M;1;;;RightSwitchPressedVR;\r\n");

            //on trigger exit

            Debug.Log("Level finished!");
            if (experimentControllerScript.expState != ExperimentState.VR_TUTORIAL)
            {
                experimentControllerScript.speedTrainingStarted = false;
                levelEndTime = Time.time;
                if (experimentControllerScript.expState == ExperimentState.SPEED_TRAINING || experimentControllerScript.expState == ExperimentState.MISTAKE_TRAINING)
                    experimentControllerScript.lastTrainingIterationSpeed = (float)57 / (float)(levelEndTime - levelStartTime);
                else if (experimentControllerScript.expState == ExperimentState.VR_PRE_TEST || experimentControllerScript.expState == ExperimentState.VR_POST_TEST)
                    experimentControllerScript.lastTrainingIterationSpeed = (float)52 / (float)(levelEndTime - levelStartTime);

                if (experimentControllerScript.expState == ExperimentState.SPEED_TRAINING || experimentControllerScript.expState == ExperimentState.MISTAKE_TRAINING || experimentControllerScript.expState == ExperimentState.VR_PRE_TEST || experimentControllerScript.expState == ExperimentState.VR_POST_TEST)
                {
                    Debug.Log("Data** Last total mistake time was - " + experimentControllerScript.currTrainingIterationMistakeTime);
                    Debug.Log("Data** Last TCT was - " + (float)(levelEndTime - levelStartTime));
                    experimentControllerScript.dataFileWriter.WriteLine("Total mistake time: " + experimentControllerScript.currTrainingIterationMistakeTime + " Total task time: " + (float)(levelEndTime - levelStartTime));
                    //Debug.Log("Last speed was - " + experimentControllerScript.lastTrainingIterationSpeed);
                }

                if (experimentControllerScript.expState == ExperimentState.SPEED_TRAINING || experimentControllerScript.expState == ExperimentState.VR_PRE_TEST)
                {
                    //Check condition and calculate next speed primer's speed based on either previous speed or pretest speed
                    if (ExperimentManagerScript.expCondition == ExperimentalCondition.CONTROL)
                        experimentControllerScript.nextTrainingIterationSpeed = experimentControllerScript.lastTrainingIterationSpeed * 1.03f; // Increase speed by 3%
                    else if (ExperimentManagerScript.expCondition == ExperimentalCondition.ADAPTIVE)
                        experimentControllerScript.nextTrainingIterationSpeed = experimentControllerScript.lastTrainingIterationSpeed;
                }

                if (experimentControllerScript.expState == ExperimentState.MISTAKE_TRAINING || experimentControllerScript.expState == ExperimentState.VR_PRE_TEST)
                {
                    experimentControllerScript.nextTrainingIterationMistakeTime = experimentControllerScript.currTrainingIterationMistakeTime;
                }

                experimentControllerScript.changeState("TRAINING_SELF_EFFICACY");

            }            
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
    }

    private void OnTriggerExit(Collider other)
    {
        --numCollidersInContact;
        if (other.tag != "StartZone" && other.tag != "StopZone" && experimentControllerScript.feedbackEnabled)
        {
            //Debug.Log("Haptic pointer z angle " + hapticPointer.transform.localEulerAngles.z);
            //print("numCollidersInContact:" + numCollidersInContact + "isFeedbackOnNow:" + experimentControllerScript.isFeedbackOnNow);
            if (numCollidersInContact < 1 && !experimentControllerScript.isFeedbackOnNow)
            {
                Debug.Log("Number of colliders in contact is less than 1. Triggering feedback");
                //currCollider = null;
                if (other.gameObject.name.StartsWith("Part"))
                {
                    partNumAtTimeOfDetaching = int.Parse(other.gameObject.name.Substring(4));

                    experimentControllerScript.doControllerDetachOperations((CapsuleCollider)other, other.gameObject.tag, loc);
                    experimentControllerScript.mistakeStartTime = Time.time;
                    //Find the vector between the two points
                    mistakeVector = experimentControllerScript.projectedHookPos - transform.position;

                    mistakeDirection = mistakeVector.normalized;
                    experimentControllerScript.isFeedbackOnNow = true;
                }
            }
        }
        else if (other.tag == "StartZone")
        {
            if (experimentControllerScript.expState == ExperimentState.SPEED_TRAINING && !experimentControllerScript.speedTrainingStarted)
            {
                experimentControllerScript.speedTrainingStarted = true;
                experimentControllerScript.startSpeedPrimer();                
            }
            Debug.Log("Level started!");
            experimentControllerScript.currTrainingIterationMistakeTime = 0;
            levelStartTime = Time.time;
            
            experimentControllerScript.feedbackEnabled = true;
            
            experimentControllerScript.solidRightHandController.SetActive(true);
            experimentControllerScript.ghostRightHandController.SetActive(false);

            //trigger enter
            experimentControllerScript.mistakeStartTime = Time.time;
            experimentControllerScript.doControllerReattachOperations("null");
            experimentControllerScript.mistakeLineObj.SetActive(false);

            //trigger stay
            if (experimentControllerScript.client != null && experimentControllerScript.expState != ExperimentState.VR_TUTORIAL)
                experimentControllerScript.client.Write("M;1;;;LeftSwitchPressedVR;\r\n");
        }
    }

    /*private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(loc, 0.005f);
    }*/
}
