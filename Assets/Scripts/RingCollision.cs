using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class RingCollision : MonoBehaviour
{
    public GameObject experimentController;
    ExperimentManagerScript experimentControllerScript;
    Vector3 loc;

    //int numCollidersInContact;

    private Vector3 mistakeVector;
    Vector3 mistakeDirection;
    float levelStartTime, levelEndTime;
    GameObject _mistakePrimerObj, _speedPrimerObj;

    float aToBMistakeTime, aToBTaskTime, totalTaskTime, totalMistakeTime, totalSpeed;

    int partNumAtTimeOfDetaching = 0, partNumAtTimeOfReattaching = 0;
   

    // Start is called before the first frame update
    void Start()
    {
        levelStartTime = levelEndTime = 0;        
        experimentControllerScript = experimentController.GetComponent<ExperimentManagerScript>();
        experimentControllerScript.numCollidersInContact = 0;
        _mistakePrimerObj = experimentControllerScript.mistakePrimer;
        _speedPrimerObj = experimentControllerScript.speedPrimer;
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
        if (experimentControllerScript.numCollidersInContact < 2) ++experimentControllerScript.numCollidersInContact;

        //print("OnTriggerEnter on collision with " + other.gameObject.name);

        //print("OnTriggerEnter: numCollidersInContact:" + experimentControllerScript.numCollidersInContact + "isFeedbackOnNow:" + experimentControllerScript.isFeedbackOnNow);
        //Debug.Log("Object in contact - " + other.gameObject);
        if (other.tag != "StartZone" && other.tag != "StopZone" && experimentControllerScript.feedbackEnabled)
        {
            if (other.gameObject.name.StartsWith("Part") && experimentControllerScript.isDetached)
            {
                partNumAtTimeOfReattaching = int.Parse(other.gameObject.name.Substring(4));
                //print("Part num at time of reattaching - " + partNumAtTimeOfReattaching + " part num at time of detaching - " + partNumAtTimeOfDetaching);

                if (partNumAtTimeOfReattaching == partNumAtTimeOfDetaching || partNumAtTimeOfReattaching == partNumAtTimeOfDetaching + 1 || partNumAtTimeOfReattaching == partNumAtTimeOfDetaching - 1)
                {
                    //print("Reattaching to correct part");
                    experimentControllerScript.doControllerReattachOperations(other.gameObject.tag);
                    experimentControllerScript.isFeedbackOnNow = false;
                    experimentControllerScript.mistakeLineObj.SetActive(false);
                }
                else
                {
                    //print("Reattaching to wrong part");
                }
            }
        }
        else if (other.tag == "StopZone" && experimentControllerScript.feedbackEnabled && !experimentControllerScript.isDetached) //This is true only when the loop is first passed through the start zone and the player is currently not making a mistake
        {
            experimentControllerScript.mistakeLineObj.SetActive(false);
                
            experimentControllerScript.numCollidersInContact = 0;
            partNumAtTimeOfDetaching = 1;
            experimentControllerScript.feedbackEnabled = false;
            experimentControllerScript.isFeedbackOnNow = false;
            experimentControllerScript.solidRightHandController.SetActive(false);
            experimentControllerScript.ghostRightHandController.SetActive(true);

            if (experimentControllerScript.client != null && experimentControllerScript.expState != ExperimentState.VR_TUTORIAL)
            {
                experimentControllerScript.accelLogger.logEventHeader("Stopped");
                experimentControllerScript.client.Write("M;1;;;RightSwitchPressedVR;\r\n");
            }

            //Debug.Log("Level finished!");
            if (experimentControllerScript.expState != ExperimentState.VR_TUTORIAL)
            {
                experimentControllerScript.speedTrainingStarted = false;
                levelEndTime = Time.time;
                float currTaskTime = levelEndTime - levelStartTime;

                if (experimentControllerScript.expState == ExperimentState.SPEED_TRAINING || experimentControllerScript.expState == ExperimentState.MISTAKE_TRAINING)
                    experimentControllerScript.lastTrainingIterationSpeed = (float)57 / currTaskTime;
                else if (experimentControllerScript.expState == ExperimentState.VR_PRE_TEST || experimentControllerScript.expState == ExperimentState.VR_POST_TEST)
                    experimentControllerScript.lastTrainingIterationSpeed = (float)(52) / currTaskTime;

                if (experimentControllerScript.expState == ExperimentState.SPEED_TRAINING || experimentControllerScript.expState == ExperimentState.MISTAKE_TRAINING || experimentControllerScript.expState == ExperimentState.VR_PRE_TEST || experimentControllerScript.expState == ExperimentState.VR_POST_TEST)
                {
                    //Debug.Log("Data** Last total mistake time was - " + experimentControllerScript.currTrainingIterationMistakeTime);
                    //Debug.Log("Data** Last TCT was - " + (float)(levelEndTime - levelStartTime));
                    experimentControllerScript.dataFileWriter.WriteLine("Mistake Time: " + experimentControllerScript.currTrainingIterationMistakeTime + " Task time: " + currTaskTime + " Speed: " + experimentControllerScript.lastTrainingIterationSpeed);
                    //Debug.Log("Last speed was - " + experimentControllerScript.lastTrainingIterationSpeed);
                }                    

                //Calculate next speed primer's speed based on either previous speed or pretest speed
                if (experimentControllerScript.expState == ExperimentState.SPEED_TRAINING)
                {
                    if (ExperimentManagerScript.expCondition == ExperimentalCondition.ADAPTIVE)
                    { 
                        experimentControllerScript.nextTrainingIterationSpeed = experimentControllerScript.lastTrainingIterationSpeed; 
                    }
                    else if (ExperimentManagerScript.expCondition == ExperimentalCondition.CONTROL)
                    {
                        experimentControllerScript.nextTrainingIterationSpeed = (1 + ((1 + experimentControllerScript.speedLevelCount) * 0.0225f)) * experimentControllerScript.pretestAtoBSpeed; //1.03f * experimentControllerScript.lastTrainingIterationSpeed; // Increase speed by 3%
                        print("Speed primer multiplicative factor - " + (1 + ((1 + experimentControllerScript.speedLevelCount) * 0.0225f)));
                    }
                }

                //Calculate mistake time target for next training iteration based on either previous mistake time or pretest mistake time
                if (experimentControllerScript.expState == ExperimentState.MISTAKE_TRAINING)
                {
                    if (ExperimentManagerScript.expCondition == ExperimentalCondition.ADAPTIVE) experimentControllerScript.nextTrainingIterationMistakeTime = experimentControllerScript.currTrainingIterationMistakeTime;
                    else if (ExperimentManagerScript.expCondition == ExperimentalCondition.CONTROL)
                    {
                        experimentControllerScript.nextTrainingIterationMistakeTime = (1 + ((1 + experimentControllerScript.mistakeLevelCount) * -0.03f)) * experimentControllerScript.pretestAtoBMistakeTime; //0.97f * experimentControllerScript.currTrainingIterationMistakeTime; // Decrease mistake time by 3%                        
                        print("Mistake primer multiplicative factor - " + (1 + ((1 + experimentControllerScript.mistakeLevelCount) * -0.03f)));
                    }
                }                    

                if (experimentControllerScript.expState == ExperimentState.VR_PRE_TEST || experimentControllerScript.expState == ExperimentState.VR_POST_TEST)
                {
                    experimentControllerScript.didLoopTouchStopZone = true;
                    experimentControllerScript.vrTestAtoBInstructionObj.SetActive(false);

                    if (experimentControllerScript.didLoopTouchStopZone && experimentControllerScript.didLoopExitStartZone)
                    {
                        //experimentControllerScript.vrTestPostWaitInstructionObj.SetActive(true);
                        if (experimentControllerScript.didVRTestWaitPeriodEnd == true)
                        {
                            //print("B to A has ended");
                            totalMistakeTime = experimentControllerScript.currTrainingIterationMistakeTime + aToBMistakeTime;
                            totalTaskTime = currTaskTime + aToBTaskTime;
                            totalSpeed = (float)(52 * 2) / totalTaskTime;
                            //experimentControllerScript.dataFileWriter.WriteLine("Combined Mistake Time: " + totalMistakeTime + " Combined Task time: " + totalTaskTime + " Combined Speed: " + totalSpeed);
                            //print("totalMistakeTime - " + totalMistakeTime);
                            //print("totalTaskTime - " + totalTaskTime);
                            //experimentControllerScript.nextTrainingIterationMistakeTime = (totalMistakeTime/104f)*57f; //Adjust mistake time for change in length from test to training levels.                             
                            //experimentControllerScript.nextTrainingIterationSpeed = totalSpeed;
                            experimentControllerScript.changeState("TRAINING_SELF_EFFICACY"); //Invoked only when the loop goes from A to B and from B to A
                        }
                        else
                        {
                            //print("A to B has ended");
                            //print("aToBMistakeTime - " + aToBMistakeTime);
                            //print("aToBTaskTime - " + currTaskTime);
                            experimentControllerScript.vrTestWaitAtBInstructionObj.SetActive(true);
                            aToBMistakeTime = experimentControllerScript.currTrainingIterationMistakeTime;                                

                            if (ExperimentManagerScript.expCondition == ExperimentalCondition.CONTROL) 
                            {
                                experimentControllerScript.pretestAtoBMistakeTime = (aToBMistakeTime / 52f) * 57f;
                                experimentControllerScript.nextTrainingIterationMistakeTime = 0.97f * experimentControllerScript.pretestAtoBMistakeTime; //Adjust mistake time for change in length from test to training levels. And then decrease target mistake time by 3%                            
                                experimentControllerScript.pretestAtoBSpeed = experimentControllerScript.lastTrainingIterationSpeed;
                                experimentControllerScript.nextTrainingIterationSpeed = experimentControllerScript.lastTrainingIterationSpeed * 1.0225f; // Increase speed by 3%
                            }
                            else if (ExperimentManagerScript.expCondition == ExperimentalCondition.ADAPTIVE)
                            {
                                experimentControllerScript.nextTrainingIterationMistakeTime = (aToBMistakeTime / 52f) * 57f; //Adjust mistake time for change in length from test to training levels.
                                experimentControllerScript.nextTrainingIterationSpeed = experimentControllerScript.lastTrainingIterationSpeed;
                            }

                            aToBTaskTime = currTaskTime;
                            StartCoroutine(waitPeriodForVRTest());
                        }
                    }
                }
                else //Go to self efficacy when the loop touch the stop zone for every state except pre and post test
                {
                    experimentControllerScript.changeState("TRAINING_SELF_EFFICACY");
                }

            }
            else
            {
                experimentControllerScript.vrTestAtoBInstructionObj.SetActive(false);
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

            if (experimentControllerScript.expState == ExperimentState.MISTAKE_TRAINING && !experimentControllerScript.isFeedbackOnNow) //Set rotation and position of mistake primer
            {
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

                /* Documentation for the following code (generated by chatgpt):
                 * The script first finds the index of the current collider in a list of colliders.
                 * It then retrieves the position and rotation information for various game objects and colliders.
                 * If the direction of the drag is along the x-axis, the script checks if the dragged object is close to either end of the wire. If so, it calculates a tilt angle delta based on the distance between the dragged object and the end anchor object.
                 * If the tilt angle delta is greater than zero, it sets the rotation of the "mistake primer" object (presumably an object that is associated with the dragged object) based on the angle delta and whether the neighboring collider is aligned with the y or z-axis.
                 * Finally, it sets the position of the "mistake primer" object to the x-coordinate of the dragged object and the y and z-coordinates of the anchor object.
                 */

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
        if(experimentControllerScript.numCollidersInContact > 0) --experimentControllerScript.numCollidersInContact;

        //print("OnTriggerExit after collision with " + other.gameObject.name);
            
        //print("OnTriggerExit : numCollidersInContact:" + experimentControllerScript.numCollidersInContact + "isFeedbackOnNow:" + experimentControllerScript.isFeedbackOnNow);
        
        if (other.tag != "StartZone" && other.tag != "StopZone" && experimentControllerScript.feedbackEnabled)
        {
            //Debug.Log("Haptic pointer z angle " + hapticPointer.transform.localEulerAngles.z);
            //print("numCollidersInContact:" + numCollidersInContact + "isFeedbackOnNow:" + experimentControllerScript.isFeedbackOnNow);
            if (experimentControllerScript.numCollidersInContact < 1 && !experimentControllerScript.isFeedbackOnNow)
            {
                //Debug.Log("Number of colliders in contact is less than 1. Triggering feedback");
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
        else if (other.tag == "StartZone") //Task starts here
        {
            //experimentControllerScript.numCollidersInContact = 1;
            //print("numCollidersInContact: " + numCollidersInContact + ",isFeedbackOnNow: " + experimentControllerScript.isFeedbackOnNow);

            if (experimentControllerScript.expState == ExperimentState.MISTAKE_TRAINING) _mistakePrimerObj.SetActive(true);

            if (experimentControllerScript.expState == ExperimentState.SPEED_TRAINING)
            {
                _speedPrimerObj.SetActive(true);
                if (!experimentControllerScript.speedTrainingStarted)
                {
                    experimentControllerScript.speedTrainingStarted = true;
                    experimentControllerScript.startSpeedPrimer();
                }
            }
            //Debug.Log("Level started!");
            experimentControllerScript.currTrainingIterationMistakeTime = 0;
            
            levelStartTime = Time.time;            

            experimentControllerScript.feedbackEnabled = true;
            
            experimentControllerScript.solidRightHandController.SetActive(true);
            experimentControllerScript.ghostRightHandController.SetActive(false);

            experimentControllerScript.mistakeStartTime = Time.time;
            experimentControllerScript.doControllerReattachOperations("null");
            experimentControllerScript.mistakeLineObj.SetActive(false);

            if (experimentControllerScript.expState == ExperimentState.VR_PRE_TEST || experimentControllerScript.expState == ExperimentState.VR_POST_TEST)
            {
                experimentControllerScript.didLoopExitStartZone = true;
                if(experimentControllerScript.didVRTestWaitPeriodEnd == true)
                {
                    //print("B to A has started");                    
                    experimentControllerScript.vrTestBtoAInstructionObj.SetActive(false);
                }
                else
                {
                    //print("A to B has started");                    
                    experimentControllerScript.vrTestAtoBInstructionObj.SetActive(false);
                }
            }

            if (experimentControllerScript.client != null && experimentControllerScript.expState != ExperimentState.VR_TUTORIAL)
            {
                experimentControllerScript.accelLogger.logEventHeader("Started");
                experimentControllerScript.client.Write("M;1;;;LeftSwitchPressedVR;\r\n");
            }
                
        }
    }
    
    IEnumerator waitPeriodForVRTest()
    {
        experimentControllerScript.didLoopTouchStopZone = false;
        experimentControllerScript.didLoopExitStartZone = false;
        yield return new WaitForSeconds(3);
        experimentControllerScript.swapTestStartStopPositions();
        experimentControllerScript.didVRTestWaitPeriodEnd = true;
        experimentControllerScript.vrTestWaitAtBInstructionObj.SetActive(false);
        experimentControllerScript.vrTestBtoAInstructionObj.SetActive(true);
        //print("waitPeriodForVRTest over");        
    }

    /*private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(loc, 0.005f);
    }*/
}
