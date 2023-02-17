using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleTCP;
using UnityEditor.PackageManager;
using UnityEngine.UI;
using System.Security.Policy;
using System.ComponentModel.Design;
using Unity.Mathematics;
using System.Security.Cryptography;
using System.IO;
using UnityEditor;

public enum ExperimentState
{
    INIT,
    BASELINE,
    PRE_TEST,
    VR_TUTORIAL,
    VR_PRE_TEST,
    VR_POST_TEST,
    MISTAKE_TRAINING,
    SPEED_TRAINING,
    TRAINING_SELF_EFFICACY,
    POST_TEST
};

public enum ExperimentalCondition
{
    CONTROL,
    ADAPTIVE
}


public class ExperimentManagerScript : MonoBehaviour
{
    static ExperimentManagerScript thisObj;

    public static ExperimentalCondition expCondition;

    List<GameObject> anchorsLst;
    public List<GameObject> colliderList;

    int listPos;
    public GameObject speedPrimer, mistakePrimer;
    public float speed; //cm per second
    public GameObject levelAnchorsRoot;
    public GameObject levelCollidersRoot;
    public GameObject trainingWireObj, vrTestWireObj, vrTutorialWireObj;

    Vector3 detachPt;
    Vector3 offsetGhostDistance;
    Vector3 oldGhostPos;
    GameObject detachPivot;
    public bool isFeedbackOnNow = false;
    public bool feedbackEnabled = false;

    public bool isDetached = false;
    enum Direction { xDir, yDir, zDir };
    CapsuleCollider currCollider;
    string currDragDir;
    public Vector3 offsetPivotAng;

    public Vector3 projectedHookPos;
    public GameObject mistakeLineObj;

    public GameObject env;
    public GameObject hapticEnv, avatar, oculusRightControllerObj;

    public GameObject testArduinoSerialControllerObj;
    public SerialController testArduinoSerialController;

    //UI
    public TMPro.TMP_Text modeTxt;
    public TMPro.TMP_Text conditionTxt;
    public TMPro.TMP_Text iMotionsConnText;
    public Image leftSwitchIndicator, rightSwitchIndicator, mistakeIndicator;
    public GameObject baselineOverIndicator;
    //public GameObject restOverIndicator;
    public GameObject configMenu;
    public Slider avatar_x_slider, avatar_y_slider, avatar_z_slider, hapticEnv_x_slider, hapticEnv_y_slider, hapticEnv_z_slider;
    public TMPro.TMP_Text currSpeedTxt, prevSpeedTxt, currTrainingIterationTxt;

    //VR UI
    public GameObject levelTimeResultsObj;
    public int levelTimeResult;
    public GameObject surveyPanel;
    public GameObject arrowObj;
    public GameObject mistakeTimeIndicatorObj;

    //VR in-game UI
    public GameObject speedSERoot, mistakeSERoot, seOkButtonObj;
    public GameObject speedSEsliderObj, mistakeSEsliderObj;
    public GameObject speedSEsliderStartLocObj, speedSEsliderEndLocObj;
    public GameObject mistakeSEsliderStartLocObj, mistakeSEsliderEndLocObj;
    public ArrowClickScript arrow;
    GameObject touchedObj;
    float speedSESliderXMin, speedSESliderXMax;
    float mistakeSESliderXMin, mistakeSESliderXMax;
    public GameObject instructionPanelObj, mistakePrimerInstructionTxtObj, speedPrimerInstructionTxtObj, okButtonObj;

    public GameObject laserPointerObj;
    public Quaternion hookRootDefaultRot;
    public Vector3 hookRootDefaultPos;
    public Quaternion solidRightHandControllerDefaultRot;
    public Vector3 solidRightHandControllerDefaultPos;

    public bool speedTrainingStarted = false;

    public ExperimentState expState;
    int currLevel;
    int mistakeLevelCount, speedLevelCount;

    public GameObject ghostRightHandController;
    public GameObject solidRightHandController;
    //public GameObject ghost_wire;
    public GameObject hookRoot;
    public Vector3 mistakePrimerStartRefPos;
    
    public float lastTrainingIterationSpeed, nextTrainingIterationSpeed;
    public float currTrainingIterationMistakeTime, lastTrainingIterationMistakeTime, nextTrainingIterationMistakeTime;
    public float mistakeStartTime, mistakeEndTime;
    public int numCollidersInContact; //To potentially solve script execution order issues

    public bool didLoopExitStartZone, didLoopTouchStopZone, didVRTestWaitPeriodEnd;
    public GameObject vrTestStartZoneObj, vrTestStopZoneObj, vrTestAtoBInstructionObj, vrTestBtoAInstructionObj, vrTestWaitAtBInstructionObj;
    Vector3 vrTestStartZonePos, vrTestStopZonePos;

    public StreamWriter dataFileWriter;
    string _participantId;

    public SimpleTcpClient client;

    //SE related
    int speedSEVal, mistakeSEVal;

    private void Awake()
    {
        thisObj = this;
        testArduinoSerialController.portName = PlayerPrefs.GetString("testCOMPort", "not_set");
        _participantId = PlayerPrefs.GetString("participantId", "not_set");

        Debug.Log("Test COM port set - " + testArduinoSerialController.portName);

        testArduinoSerialControllerObj.SetActive(true);        

        if (PlayerPrefs.GetString("env_x", "not_set") == "not_set" || PlayerPrefs.GetString("env_y", "not_set") == "not_set" || PlayerPrefs.GetString("env_z", "not_set") == "not_set")
        {
            Debug.Log("Env pos not set");
        }
        else
        {
            env.transform.position = new Vector3(float.Parse(PlayerPrefs.GetString("env_x", "not_set")), float.Parse(PlayerPrefs.GetString("env_y", "not_set")), float.Parse(PlayerPrefs.GetString("env_z", "not_set")));
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        listPos = 0;
        lastTrainingIterationSpeed = 1f;
        speed = 0;
        currLevel = 0;
        currTrainingIterationMistakeTime = 0;
        mistakeLevelCount = speedLevelCount = 0;

        expCondition = ExperimentalCondition.ADAPTIVE;

        if (expCondition == ExperimentalCondition.CONTROL)
            conditionTxt.text = "Control";
        else if (expCondition == ExperimentalCondition.ADAPTIVE)
            conditionTxt.text = "Adaptive";

        anchorsLst = new List<GameObject>();
        colliderList = new List<GameObject>();

        //Assign all child gameobjects of primerAnchorRoot to anchorsLst
        for (int i = 0; i < levelAnchorsRoot.transform.childCount; i++)
        {
            anchorsLst.Add(levelAnchorsRoot.transform.GetChild(i).gameObject);
        }

        for (int i = 0; i < levelCollidersRoot.transform.childCount; i++)
        {
            colliderList.Add(levelCollidersRoot.transform.GetChild(i).gameObject);
        }

        changeState("PRE_TEST");

        solidRightHandControllerDefaultRot = solidRightHandController.transform.localRotation;
        solidRightHandControllerDefaultPos = solidRightHandController.transform.localPosition;
        detachPivot = new GameObject("DetachPivot");
        detachPivot.transform.position = Vector3.zero;
        oldGhostPos = ghostRightHandController.transform.position;
        mistakePrimerStartRefPos = mistakePrimer.transform.position;
        vrTestStartZonePos = vrTestStartZoneObj.transform.position;
        vrTestStopZonePos = vrTestStopZoneObj.transform.position;

        //startStopRefController.transform.position = startPositions[currLevel - 1];
        solidRightHandController.SetActive(false);
        ghostRightHandController.SetActive(true);

        //File write
        if (!Directory.Exists(Application.persistentDataPath + "/AdaptiveExperimentData"))
        {
            Directory.CreateDirectory(Application.persistentDataPath + "/AdaptiveExperimentData");
        }
        dataFileWriter = new StreamWriter(Application.persistentDataPath + "/AdaptiveExperimentData/" + _participantId + System.DateTime.Now.ToString("Data_dd_MMMM_yyyy_HH_mm_ss") + ".txt");
        dataFileWriter.WriteLine(expCondition.ToString());
        EditorApplication.playModeStateChanged += LogPlayModeState;

        client = new SimpleTcpClient().Connect("127.0.0.1", 8089);
    }

    
#if UNITY_EDITOR
    private static void LogPlayModeState(PlayModeStateChange state)
    {
        thisObj.dataFileWriter.Flush();
        Debug.Log(state);
    }
#endif


    private void FixedUpdate()
    {
        //Debug.Log("expState" + expState);
        if (client == null)
        {
            iMotionsConnText.color = Color.red;
            iMotionsConnText.text = "iMotions Disconnected";
        }
        else
        {
            iMotionsConnText.color = Color.green;
            iMotionsConnText.text = "iMotions Connected";
        }


        offsetGhostDistance = ghostRightHandController.transform.position - oldGhostPos;
        oldGhostPos = ghostRightHandController.transform.position;

        string message;

        if (expState == ExperimentState.SPEED_TRAINING || expState == ExperimentState.MISTAKE_TRAINING || expState == ExperimentState.VR_PRE_TEST || expState == ExperimentState.VR_POST_TEST || expState == ExperimentState.VR_TUTORIAL)
        {
            message = null; //Since we are not using physical test here
            //print("isDetached:" + isDetached + "feedbackEnabled:" + feedbackEnabled);
            if (isFeedbackOnNow)//(isDetached && feedbackEnabled)
            {
                if (expState == ExperimentState.MISTAKE_TRAINING)
                {
                    mistakeTimeIndicatorObj.GetComponent<Image>().fillAmount = 1 - ((float)(Time.time - mistakeStartTime + currTrainingIterationMistakeTime) / (float)lastTrainingIterationMistakeTime);
                }
                
                mistakeLineObj.SetActive(true);
                
                OVRInput.SetControllerVibration(1, 1, OVRInput.Controller.RTouch);
                if (client != null && expState != ExperimentState.VR_TUTORIAL)
                    client.Write("M;1;;;BuzzWireHitVR;\r\n");
                //Debug.Log("isDetached = true");
                if (currDragDir == "x-axis")
                {
                    projectedHookPos = detachPivot.transform.position + new Vector3(offsetGhostDistance.x, 0, 0);
                    if (projectedHookPos.x < currCollider.bounds.max.x && projectedHookPos.x > currCollider.bounds.min.x)
                        detachPivot.transform.position = projectedHookPos;
                }
                else if (currDragDir == "y-axis")
                {
                    projectedHookPos = detachPivot.transform.position + new Vector3(0, offsetGhostDistance.y, 0);
                    if (projectedHookPos.y < currCollider.bounds.max.y && projectedHookPos.y > currCollider.bounds.min.y)
                        detachPivot.transform.position = projectedHookPos;
                }
                else if (currDragDir == "z-axis")
                {
                    projectedHookPos = detachPivot.transform.position + new Vector3(0, 0, offsetGhostDistance.z);
                    if (projectedHookPos.z < currCollider.bounds.max.z && projectedHookPos.z > currCollider.bounds.min.z)
                        detachPivot.transform.position = projectedHookPos;
                }
                
            }
            else
            {
                mistakeLineObj.SetActive(false);
                //Debug.Log("isDetached = false");
            }
        }
        else if(expState == ExperimentState.TRAINING_SELF_EFFICACY)
        {
            //Debug.Log("Training Self Efficacy");
            message = null; //Since we are not using physical test here
            if (arrow.touchingObj != null)
            {
                touchedObj = arrow.touchingObj;
                if (touchedObj == speedSEsliderObj)
                {
                    //print("Touching speed SE slider line");
                    //int newX = (int)math.remap(arrow.xMin, arrow.xMax, formUIScript.xMin, formUIScript.xMax, hapticDeviceArrow.clickLoc.x);
                    //int clampedX = (int)math.clamp(newX, formUIScript.xMin, formUIScript.xMax);
                    speedSESliderXMin = speedSEsliderStartLocObj.transform.position.x;
                    speedSESliderXMax = speedSEsliderEndLocObj.transform.position.x;
                    int selectedVal = (int)math.remap(speedSESliderXMin, speedSESliderXMax, 1, 100, arrow.clickLoc.x);
                    speedSEVal = (int)math.clamp(selectedVal, 1, 100);
                    //print("Selected Speed SE:" + speedSEVal);
                    
                }
                else if (touchedObj == mistakeSEsliderObj)
                {
                    //print("Touching mistake SE slider line");
                    mistakeSESliderXMin = mistakeSEsliderStartLocObj.transform.position.x;
                    mistakeSESliderXMax = mistakeSEsliderEndLocObj.transform.position.x;
                    int selectedVal = (int)math.remap(mistakeSESliderXMin, mistakeSESliderXMax, 1, 100, arrow.clickLoc.x);
                    mistakeSEVal = (int)math.clamp(selectedVal, 1, 100);
                    //print("Selected Mistake SE:" + mistakeSEVal);
                }
            }
        }
        else //Physical test
        {
            message = testArduinoSerialController.ReadSerialMessage();
        }

        if (message == null)
        {
            leftSwitchIndicator.color = Color.gray;
            rightSwitchIndicator.color = Color.gray;
            mistakeIndicator.color = Color.gray;
            return;
        }

        // Check if the message is plain data or a connect/disconnect event.
        if (ReferenceEquals(message, SerialController.SERIAL_DEVICE_CONNECTED))
        {
            Debug.Log("Connection established");
        }
        else if (ReferenceEquals(message, SerialController.SERIAL_DEVICE_DISCONNECTED))
        {
            //Debug.Log("Connection attempt failed or disconnection detected");
        }
        else
        {
            //Debug.Log("Message arrived: " + message);
            if (message == "1")
            {
                mistakeIndicator.color = Color.red;
                if (client != null)
                    client.Write("M;1;;;BuzzWireHitTest;Buzz wire was hit\r\n");
            }
            else
            {
                //beepsound.mute = true;
            }

            if (message == "+")
            {
                leftSwitchIndicator.color = Color.green;
                if (client != null)
                    client.Write("M;1;;;LeftSwitchPressedTest;Left Switch Pressed\r\n");
            }
            else if (message == "*")
            {
                rightSwitchIndicator.color = Color.green;
                if (client != null)
                    client.Write("M;1;;;RightSwitchPressedTest;Right Switch Pressed\r\n");
            }
            else
            {
                leftSwitchIndicator.color = Color.gray;
                rightSwitchIndicator.color = Color.gray;
            }
            message = "";
        }

    }

    public void doControllerDetachOperations(CapsuleCollider _collider, string tag, Vector3 _detachPt)
    {
        currCollider = _collider;
        //Debug.Log("isDetached = true, collision with " + tag);
        isDetached = true;
        currDragDir = tag; //x-dir, y-dir or z-dir        

        detachPt = _detachPt;
        detachPivot.transform.position = detachPt;

        solidRightHandController.SetActive(true);
        solidRightHandController.transform.SetParent(detachPivot.transform);
        ghostRightHandController.SetActive(true);
    }

    public void doControllerReattachOperations(string tag)
    {
        //Debug.Log("isDetached = false, collision with " + tag);
        isDetached = false;
        OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch);
        mistakeEndTime = Time.time;

        if (expState == ExperimentState.MISTAKE_TRAINING || expState == ExperimentState.SPEED_TRAINING || expState == ExperimentState.VR_PRE_TEST || expState == ExperimentState.VR_POST_TEST)
        {
            //print("Last mistake duration:" + (mistakeEndTime - mistakeStartTime));
            if (tag != "null") currTrainingIterationMistakeTime += (mistakeEndTime - mistakeStartTime);
            //print("Current total mistake time - " + currTrainingIterationMistakeTime);
            //print("Last total mistake time - " + lastTrainingIterationMistakeTime);
        }
 
        //print("Previous mistake time - " + lastTrainingIterationMistakeTime);

        //mistakeTimeIndicatorObj.GetComponent<Image>().fillAmount = ((float)currTrainingIterationMistakeTime / (float)lastTrainingIterationMistakeTime);
        //Debug.Log("Mistake percentage: " + mistakeTimeIndicatorObj.GetComponent<Image>().fillAmount);

        ghostRightHandController.SetActive(false);

        solidRightHandController.transform.SetParent(hookRoot.transform);
        solidRightHandController.transform.localRotation = solidRightHandControllerDefaultRot;
        solidRightHandController.transform.localPosition = solidRightHandControllerDefaultPos;

    }

    void OnMessageArrived(string msg)
    {
        Debug.Log("Message from Arduino - " + msg);
    }

    // Invoked when a connect/disconnect event occurs. The parameter 'success'
    // will be 'true' upon connection, and 'false' upon disconnection or
    // failure to connect.
    void OnConnectionEvent(bool success)
    {
        Debug.Log("Arduino connected");
    }
    
    public IEnumerator startBaselineCounterCoroutine()
    {
        modeTxt.text = "Baseline Active";
        Debug.Log("Baseline started");
        int seconds = 180;
        while (seconds > 0)
        {
            yield return new WaitForSecondsRealtime(1);
            baselineOverIndicator.GetComponentInChildren<Text>().text = "" + seconds + "s";
            seconds--;
        }
        if (client != null)
            client.Write("M;1;;;baseline_over;Baseline over\r\n");

        baselineOverIndicator.SetActive(true);
        baselineOverIndicator.GetComponentInChildren<Text>().text = "Baseline over";
        //Debug.Log("delayResetNewTasksFlag");
    }

    public void startBaseline()
    {
        if (client != null)
            client.Write("M;1;;;baseline_started;Baseline started\r\n");

        StartCoroutine(startBaselineCounterCoroutine());
    }

    public void moveToLevel(int level)
    {
        mistakeLineObj.GetComponent<LineRenderer>().startColor = Color.black;
        mistakeLineObj.GetComponent<LineRenderer>().endColor = Color.black;
        //modeTxt.text = "Training Mode On";
        //trainingPhase = true;
        if (level == 0)
        {
            //setAllLevelsInactive();
            levelTimeResultsObj.SetActive(false);
            trainingWireObj.SetActive(true);
            hookRoot.SetActive(false);
            //hookRoot = hook_difficulty_1_root;
            //ghostRightHandController = ghost_difficulty_1;
            //solidRightHandController = solid_difficulty_1;
            hookRoot.SetActive(true);

            if (client != null)
                client.Write("M;1;;;level_1_started;\r\n");
        }


        //startStopRefController.transform.position = startPositions[currLevel - 1];

    }

    
    public void changeHapticEnv_x(float x)
    {
        hapticEnv.transform.position = new Vector3(x, hapticEnv.transform.position.y, hapticEnv.transform.position.z);
    }

    public void changeHapticEnv_y(float y)
    {
        hapticEnv.transform.position = new Vector3(hapticEnv.transform.position.x, y, hapticEnv.transform.position.z);
    }

    public void changeHapticEnv_z(float z)
    {
        hapticEnv.transform.position = new Vector3(hapticEnv.transform.position.x, hapticEnv.transform.position.y, z);
    }



    public void changeAvatar_x(float x)
    {
        avatar.transform.position = new Vector3(x, avatar.transform.position.y, avatar.transform.position.z);
    }

    public void changeAvatar_y(float y)
    {
        avatar.transform.position = new Vector3(avatar.transform.position.x, y, avatar.transform.position.z);
    }

    public void changeAvatar_z(float z)
    {
        avatar.transform.position = new Vector3(avatar.transform.position.x, avatar.transform.position.y, z);
    }

    public void changeState(string state)
    {
        switch (state)
        {
            case "INIT":
                expState = ExperimentState.INIT;
                break;
                
            case "BASELINE":
                expState = ExperimentState.BASELINE;
                startBaseline();
                break;
                
            case "PRE_TEST":
                expState = ExperimentState.PRE_TEST;
                //goSound.Play();
                modeTxt.text = "Pre Test";
                if (client != null)
                    client.Write("M;1;;;pre_test_started;\r\n");
                break;
                
            case "VR_TUTORIAL":
                expState = ExperimentState.VR_TUTORIAL;
                modeTxt.text = "Tutorial Mode";                             
                hookRoot.SetActive(true);
                trainingWireObj.SetActive(false);
                vrTestWireObj.SetActive(false);
                vrTutorialWireObj.SetActive(true);
                vrTestAtoBInstructionObj.SetActive(true);
                
                break;
                
            case "VR_PRE_TEST":
                expState = ExperimentState.VR_PRE_TEST;
                modeTxt.text = "VR Pretest";
                trainingWireObj.SetActive(false);
                vrTestWireObj.SetActive(true);
                vrTutorialWireObj.SetActive(false);
                hookRoot.SetActive(true);
                mistakePrimer.SetActive(false);
                speedPrimer.SetActive(false);
                mistakeTimeIndicatorObj.SetActive(false);
                arrowObj.SetActive(false);
                didLoopExitStartZone = false;
                didLoopTouchStopZone = false;
                didVRTestWaitPeriodEnd = false;
                vrTestAtoBInstructionObj.SetActive(true);
                resetTestStartStopPositions();
                
                break;

            case "VR_POST_TEST":
                expState = ExperimentState.VR_POST_TEST;
                modeTxt.text = "VR Posttest";
                trainingWireObj.SetActive(false);
                vrTestWireObj.SetActive(true);
                vrTutorialWireObj.SetActive(false);
                hookRoot.SetActive(true);
                mistakePrimer.SetActive(false);
                speedPrimer.SetActive(false);
                mistakeTimeIndicatorObj.SetActive(false);
                arrowObj.SetActive(false);
                didLoopExitStartZone = false;
                didLoopTouchStopZone = false;
                didVRTestWaitPeriodEnd = false;
                vrTestAtoBInstructionObj.SetActive(true);
                resetTestStartStopPositions();
                
                break;

            case "SPEED_TRAINING":
                expState = ExperimentState.SPEED_TRAINING;
                ++currLevel;
                ++speedLevelCount;
                dataFileWriter.WriteLine("\nSpeed Primer Training Selected");
                dataFileWriter.WriteLine("--------------------------------");
                dataFileWriter.WriteLine("Training iteration: " + currLevel + ", Speed training iteration: " + speedLevelCount);

                currTrainingIterationTxt.text = "Iteration #" + currLevel;

                trainingWireObj.SetActive(false);
                vrTestWireObj.SetActive(false);
                vrTutorialWireObj.SetActive(false);

                mistakeTimeIndicatorObj.SetActive(false);
                arrowObj.SetActive(true);
                instructionPanelObj.SetActive(true);
                speedPrimerInstructionTxtObj.SetActive(true);
                mistakePrimerInstructionTxtObj.SetActive(false);
                speedPrimer.transform.position = mistakePrimerStartRefPos; //Both start at the same place

                //Debug.Log("Data** Speed Primer Selected");
                //Debug.Log("Data** Next speed target is - " + lastTrainingIterationSpeed); 
                dataFileWriter.WriteLine("Target training speed: " + nextTrainingIterationSpeed);

                StartCoroutine(showOkButtonAfterDelay());
                
                modeTxt.text = "Speed Training";                
                if (client != null)
                    client.Write("M;1;;;speed_training_started;\r\n");
                break;

            case "MISTAKE_TRAINING":
                expState = ExperimentState.MISTAKE_TRAINING;
                ++currLevel;
                ++mistakeLevelCount;
                dataFileWriter.WriteLine("\nMistake Primer Training Selected");
                dataFileWriter.WriteLine("----------------------------------");
                dataFileWriter.WriteLine("Training iteration: " + currLevel + ", Mistake training iteration: " + mistakeLevelCount);
                currTrainingIterationTxt.text = "Iteration #" + currLevel;

                trainingWireObj.SetActive(false);
                vrTestWireObj.SetActive(false);
                vrTutorialWireObj.SetActive(false);
    
                mistakeTimeIndicatorObj.SetActive(true);
                instructionPanelObj.SetActive(true);
                speedPrimerInstructionTxtObj.SetActive(false);
                mistakePrimerInstructionTxtObj.SetActive(true);
                StartCoroutine(showOkButtonAfterDelay());

                //Debug.Log("Data** Mistake Primer Selected");

                lastTrainingIterationMistakeTime = nextTrainingIterationMistakeTime;
                dataFileWriter.WriteLine("Target training mistake time: " + lastTrainingIterationMistakeTime);
                //lastTrainingIterationMistakeTime = currTrainingIterationMistakeTime; // Decrease mistake time by 10%
                Debug.Log("Data** Next mistake time target is - " + lastTrainingIterationMistakeTime);
                //currTrainingIterationMistakeTime = 0;
                mistakeTimeIndicatorObj.GetComponent<Image>().fillAmount = 1;
                mistakePrimer.transform.position = mistakePrimerStartRefPos;               
                
                modeTxt.text = "Mistake Training";
                if (client != null)
                    client.Write("M;1;;;_mistake_training_started;\r\n");
                break;

            case "TRAINING_SELF_EFFICACY":
                expState = ExperimentState.TRAINING_SELF_EFFICACY;
                
                trainingWireObj.SetActive(false);
                vrTestWireObj.SetActive(false);
                vrTutorialWireObj.SetActive(false);
                surveyPanel.SetActive(true);
                mistakePrimer.SetActive(false);
                speedPrimer.SetActive(false);
                mistakeTimeIndicatorObj.SetActive(false);

                //Switch values of speedSERoot and mistakeSERoot
                Vector3 temp = speedSERoot.GetComponent<RectTransform>().position;                
                speedSERoot.GetComponent<RectTransform>().position = mistakeSERoot.GetComponent<RectTransform>().position;
                mistakeSERoot.GetComponent<RectTransform>().position = temp;

                arrow.resetSlider();
                hookRoot.SetActive(false);
                arrowObj.SetActive(true);
                modeTxt.text = "SE";                
 
                break;


            case "POST_TEST":
                surveyPanel.SetActive(false);
                expState = ExperimentState.POST_TEST;
                modeTxt.text = "Post Test Active";
                if (client != null)
                    client.Write("M;1;;;post_test_started;\r\n");
                break;
            default:
                print("Invalid experiment state specified");
                break;
        }
    }

    IEnumerator showOkButtonAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        okButtonObj.SetActive(true);
        arrowObj.SetActive(true);
    }

    public void okButtonClicked()
    {
        hookRoot.SetActive(true);
        trainingWireObj.SetActive(true);
        arrowObj.SetActive(false);

        okButtonObj.SetActive(false);
        instructionPanelObj.SetActive(false);
        mistakePrimerInstructionTxtObj.SetActive(false);
        speedPrimerInstructionTxtObj.SetActive(false);

        if (expState == ExperimentState.SPEED_TRAINING)
        {
            speedPrimer.SetActive(true);
            //mistakePrimer.SetActive(false);

        }
        else if (expState == ExperimentState.MISTAKE_TRAINING)
        {
            //mistakePrimer.SetActive(true);
            speedPrimer.SetActive(false);
        }
    }

    public void seOkButtonClicked()
    {
        //print("seOkButtonClicked");
        surveyPanel.SetActive(false);
        dataFileWriter.WriteLine("\nSpeed SE Value:" + speedSEVal + ", Mistake SE Value:" + mistakeSEVal);
    }


    public void showLevelResult(int _levelTimeResult)
    {
        //levelTimeResultsObj.SetActive(true);
        //levelTimeResultsObj.transform.GetChild(0).GetChild(0).GetComponent<TMPro.TMP_Text>().text = "You took " + _levelTimeResult + " seconds. \n Come on, you can do it faster!";

    }

    public void calibrateEnv()
    {
        //set env position as the midway point between the avatar and the oculus right controller
        Vector3 avatarPos = avatar.transform.position;
        Vector3 oculusRightControllerPos = oculusRightControllerObj.transform.position;
        Vector3 envPos = (avatarPos + oculusRightControllerPos) / 2;
        env.transform.position = new Vector3(envPos.x, oculusRightControllerPos.y, envPos.z);
        mistakePrimerStartRefPos = mistakePrimer.transform.position;
        vrTestStartZonePos = vrTestStartZoneObj.transform.position;
        vrTestStopZonePos = vrTestStopZoneObj.transform.position;
        PlayerPrefs.SetString("env_x", env.transform.position.x.ToString());
        PlayerPrefs.SetString("env_y", env.transform.position.y.ToString());
        PlayerPrefs.SetString("env_z", env.transform.position.z.ToString());
        PlayerPrefs.Save();
    }

    public void saveConfig()
    {
        PlayerPrefs.SetString("avatar_x", avatar_x_slider.value.ToString());
        PlayerPrefs.SetString("avatar_y", avatar_y_slider.value.ToString());
        PlayerPrefs.SetString("avatar_z", avatar_z_slider.value.ToString());
        PlayerPrefs.SetString("hapticEnv_x", hapticEnv_x_slider.value.ToString());
        PlayerPrefs.SetString("hapticEnv_y", hapticEnv_y_slider.value.ToString());
        PlayerPrefs.SetString("hapticEnv_z", hapticEnv_z_slider.value.ToString());
        PlayerPrefs.Save();
    }

    public void toggleConfigMenu()
    {
        if (configMenu.activeSelf)
        {
            configMenu.SetActive(false);
        }
        else
        {
            configMenu.SetActive(true);
        }
    }

    void checkMistakeLevelCountAndChoose()
    {
        if (mistakeLevelCount < 8)
        {
            print("Mistake SE is lower");
            changeState("MISTAKE_TRAINING");
        }
        else
        {
            print("Mistake SE is lower but mistake level count is 8. So switching to speed training");
            changeState("SPEED_TRAINING");
        }
    }

    void checkSpeedLevelCountAndChoose()
    {
        if (speedLevelCount < 8)
        {
            print("Speed SE is lower");
            changeState("SPEED_TRAINING");
        }
        else
        {
            print("Speed SE is lower but speed level count is 8. So switching to mistake training");
            changeState("MISTAKE_TRAINING");
        }
    }

    public void decideNextTrial()
    {
        surveyPanel.SetActive(false);

        if (currLevel < 10)
        {
            if (expCondition == ExperimentalCondition.ADAPTIVE)
            {
                //Logic for selecting next primer
                if (speedSEVal > mistakeSEVal)
                {
                    checkMistakeLevelCountAndChoose(); 
                }
                else if (speedSEVal < mistakeSEVal)
                {
                    checkSpeedLevelCountAndChoose();
                }
                else //Equal so randomly choose
                {
                    //Randomly choose between invoking selectSpeedPrimer or selectMistakePrimer
                    print("Mistake SE and Speed SE are equal. Randomly choosing between speed and mistake primer - ");
                    float rand = UnityEngine.Random.Range(0, 9);
                    if ((int)rand % 2 == 0)
                    {
                        checkSpeedLevelCountAndChoose();
                    }
                    else
                    {
                        checkMistakeLevelCountAndChoose();
                    }
                }
            }
            else if (expCondition == ExperimentalCondition.CONTROL)
            {
        
            }
        }
        else
        {
            //changeState("VR_POST_TEST");
        }
        
         //arrowObj.SetActive(false);
        //hookRoot.SetActive(true); 
    }

    public void startSpeedPrimer()
    {
        speed = nextTrainingIterationSpeed;
        listPos = 0;
        //
        StartCoroutine(MoveRing());
    }

    IEnumerator MoveRing()
    {
        while (true)
        {
            yield return StartCoroutine(MoveFromTo(speedPrimer.transform, anchorsLst[listPos].transform, anchorsLst[listPos + 1].transform, speed));
            if (listPos < anchorsLst.Count - 2)
            {
                listPos++;
            }
            else
                break;
        }
    }
    
    IEnumerator MoveFromTo(Transform objectToMove, Transform a, Transform b, float speed) //Adapted from https://gamedev.stackexchange.com/questions/100535/coroutine-to-move-to-position-passing-the-movement-speed
    {
        float step = ((speed / 100) / (a.position - b.position).magnitude) * Time.fixedDeltaTime; //Speed is converted from cm to m
        float t = 0;
        while (t <= 1.0f)
        {
            t += step; // Goes from 0 to 1, incrementing by step each time
            objectToMove.position = Vector3.Lerp(a.position, b.position, t); // Move objectToMove closer to b
            objectToMove.rotation = Quaternion.Lerp(a.rotation, b.rotation, t);
            yield return new WaitForFixedUpdate();         // Leave the routine and return here in the next frame
        }
        objectToMove.position = b.position;
        objectToMove.rotation = b.rotation;
    }

    IEnumerator Haptics(float frequency, float amplitude, float duration, bool rightHand, bool leftHand)
    {
        if (rightHand) OVRInput.SetControllerVibration(frequency, amplitude, OVRInput.Controller.RTouch);
        if (leftHand) OVRInput.SetControllerVibration(frequency, amplitude, OVRInput.Controller.LTouch);

        yield return new WaitForSeconds(duration);

        if (rightHand) OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch);
        if (leftHand) OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.LTouch);
    }

    public void swapTestStartStopPositions()
    {
        vrTestStartZoneObj.transform.position = vrTestStopZonePos;
        vrTestStopZoneObj.transform.position = vrTestStartZonePos;
    }

    public void resetTestStartStopPositions()
    {
        vrTestStartZoneObj.transform.position = vrTestStartZonePos;
        vrTestStopZoneObj.transform.position = vrTestStopZonePos;
    }

    public void closeApp()
    {
        dataFileWriter.Close();
        Application.Quit();
    }

}
