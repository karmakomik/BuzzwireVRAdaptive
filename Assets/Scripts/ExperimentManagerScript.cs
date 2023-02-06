using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleTCP;
using UnityEditor.PackageManager;
using UnityEngine.UI;
using System.Security.Policy;
using System.ComponentModel.Design;
using Unity.Mathematics;

public enum ExperimentState
{
    INIT,
    BASELINE,
    PRE_TEST,
    VR_TUTORIAL,
    VR_TEST,
    TRAINING,
    TRAINING_SELF_EFFICACY,
    POST_TEST
};

public class ExperimentManagerScript : MonoBehaviour
{
    List<GameObject> anchorsLst;
    public List<GameObject> colliderList;
    float fraction;
    int listPos;
    public GameObject speedPrimer, mistakePrimer;
    public float speed; //cm per second
    public GameObject levelAnchorsRoot;
    public GameObject levelCollidersRoot;
    public GameObject levelObj;

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
    public GameObject hapticEnv, avatar;

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
    public TMPro.TMP_Text currSpeedTxt, prevSpeedTxt;

    //VR UI
    public GameObject levelTimeResultsObj;
    public int levelTimeResult;
    public GameObject surveyPanel;
    public GameObject arrowObj;
    public GameObject mistakeTimeIndicatorObj;

    //VR in-game UI
    public GameObject speedSERoot, mistakeSERoot;
    public GameObject speedSEsliderObj, mistakeSEsliderObj;
    public GameObject speedSEsliderStartLocObj, speedSEsliderEndLocObj;
    public GameObject mistakeSEsliderStartLocObj, mistakeSEsliderEndLocObj;
    public ArrowClickScript arrow;
    GameObject touchedObj;
    float speedSESliderXMin, speedSESliderXMax;
    float mistakeSESliderXMin, mistakeSESliderXMax;

    public GameObject laserPointerObj;
    public Quaternion hookRootDefaultRot;
    public Vector3 hookRootDefaultPos;
    public Quaternion solidRightHandControllerDefaultRot;
    public Vector3 solidRightHandControllerDefaultPos;

    public ExperimentState expState;

    public GameObject ghostRightHandController;
    public GameObject solidRightHandController;
    //public GameObject ghost_wire;
    public GameObject hookRoot;
    public Vector3 mistakePrimerStartRefPos;

    public float lastTrainingIterationSpeed, nextTrainingIterationSpeed;
    public float currTrainingIterationMistakeTime, lastTrainingIterationMistakeTime, nextTrainingIterationMistakeTime;
    public int mistakeStartTime, mistakeEndTime;

    

    public SimpleTcpClient client;

    //SE related
    int speedSEVal, mistakeSEVal;

    private void Awake()
    {
        testArduinoSerialController.portName = PlayerPrefs.GetString("testCOMPort", "not_set");

        Debug.Log("Test COM port set - " + testArduinoSerialController.portName);

        testArduinoSerialControllerObj.SetActive(true);


        if (PlayerPrefs.GetString("avatar_x", "not_set") == "not_set" || PlayerPrefs.GetString("avatar_y", "not_set") == "not_set" || PlayerPrefs.GetString("avatar_z", "not_set") == "not_set")
        {
            Debug.Log("Avatar pos not set");
        }
        else
        {
            //print("Player prefs avatar_z" + PlayerPrefs.GetString("avatar_z", "not_set"));
            avatar.transform.position = new Vector3(float.Parse(PlayerPrefs.GetString("avatar_x", "not_set")), float.Parse(PlayerPrefs.GetString("avatar_y", "not_set")), float.Parse(PlayerPrefs.GetString("avatar_z", "not_set")));
            avatar_x_slider.value = float.Parse(PlayerPrefs.GetString("avatar_x", "not_set"));
            avatar_y_slider.value = float.Parse(PlayerPrefs.GetString("avatar_y", "not_set"));
            avatar_z_slider.value = float.Parse(PlayerPrefs.GetString("avatar_z", "not_set"));
        }

        if (PlayerPrefs.GetString("hapticEnv_x", "not_set") == "not_set" || PlayerPrefs.GetString("hapticEnv_y", "not_set") == "not_set" || PlayerPrefs.GetString("hapticEnv_z", "not_set") == "not_set")
        {
            Debug.Log("Haptic Env pos not set");
        }
        else
        {
            hapticEnv.transform.position = new Vector3(float.Parse(PlayerPrefs.GetString("hapticEnv_x", "not_set")), float.Parse(PlayerPrefs.GetString("hapticEnv_y", "not_set")), float.Parse(PlayerPrefs.GetString("hapticEnv_z", "not_set")));
            hapticEnv_x_slider.value = float.Parse(PlayerPrefs.GetString("hapticEnv_x", "not_set"));
            hapticEnv_y_slider.value = float.Parse(PlayerPrefs.GetString("hapticEnv_y", "not_set"));
            hapticEnv_z_slider.value = float.Parse(PlayerPrefs.GetString("hapticEnv_z", "not_set"));
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        listPos = 0;
        fraction = 0;
        lastTrainingIterationSpeed = 1f;
        speed = 0;

        anchorsLst = new List<GameObject>();
        colliderList = new List<GameObject>();
        //StartCoroutine(MoveRing());
        //StartCoroutine(MoveFromTo(ring.transform,anchorsLst[1].transform, anchorsLst[2].transform, 0.01f));
        //Assign all child gameobjects of primerAnchorRoot to anchorsLst
        for (int i = 0; i < levelAnchorsRoot.transform.childCount; i++)
        {
            anchorsLst.Add(levelAnchorsRoot.transform.GetChild(i).gameObject);
        }

        for (int i = 0; i < levelCollidersRoot.transform.childCount; i++)
        {
            colliderList.Add(levelCollidersRoot.transform.GetChild(i).gameObject);
        }

        //expState = ExperimentState.TRAINING;
        changeState("TRAINING");

        //beepsound.mute = true;
        //currLevel = 1;

        solidRightHandControllerDefaultRot = solidRightHandController.transform.localRotation;
        solidRightHandControllerDefaultPos = solidRightHandController.transform.localPosition;
        detachPivot = new GameObject("DetachPivot");
        detachPivot.transform.position = Vector3.zero;
        oldGhostPos = ghostRightHandController.transform.position;
        mistakePrimerStartRefPos = mistakePrimer.transform.position;

        //startStopRefController.transform.position = startPositions[currLevel - 1];
        solidRightHandController.SetActive(false);
        ghostRightHandController.SetActive(true);

        //checkSnapCondition = false;
        //doControllerDetachOperations(null, "",  startPositions[currLevel - 1], true);
        //Debug.Log("startPositions[currLevel - 1].transform.position" + transform.TransformPoint(startPositions[currLevel - 1]));
        client = new SimpleTcpClient().Connect("127.0.0.1", 8089);

        


    }


    
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

        if (expState == ExperimentState.TRAINING || expState == ExperimentState.VR_TUTORIAL)
        {
            message = null; //Since we are not using physical test here
            if (isDetached && feedbackEnabled)
            {
                mistakeLineObj.SetActive(true);
                //StartCoroutine(Haptics(1, 1, 0.2f, true, false));
                OVRInput.SetControllerVibration(1, 1, OVRInput.Controller.RTouch);
                if (client != null && expState != ExperimentState.VR_TUTORIAL)
                    client.Write("M;1;;;BuzzWireHitVR;\r\n");
                //Debug.Log("isDetached = true");
                if (currDragDir == "x-axis")
                {
                    //Vector3 projectedPos = new Vector3(ghostRightHandController.transform.position.x, solidRightHandController.transform.position.y, solidRightHandController.transform.position.z);
                    //if (projectedPos.x < currCollider.bounds.max.x && projectedPos.x > currCollider.bounds.min.x)
                    //    solidRightHandController.transform.position = projectedPos;
                    projectedHookPos = detachPivot.transform.position + new Vector3(offsetGhostDistance.x, 0, 0);
                    if (projectedHookPos.x < currCollider.bounds.max.x && projectedHookPos.x > currCollider.bounds.min.x)
                        detachPivot.transform.position = projectedHookPos;

                    //detachPivot.transform.eulerAngles = ghostRightHandController.transform.eulerAngles;// + offsetPivotAng;

                }
                else if (currDragDir == "y-axis")
                {
                    //solidRightHandController.transform.position = new Vector3(solidRightHandController.transform.position.x, ghostRightHandController.transform.position.y, solidRightHandController.transform.position.z);
                    projectedHookPos = detachPivot.transform.position + new Vector3(0, offsetGhostDistance.y, 0);
                    if (projectedHookPos.y < currCollider.bounds.max.y && projectedHookPos.y > currCollider.bounds.min.y)
                        detachPivot.transform.position = projectedHookPos;
                    //detachPivot.transform.eulerAngles = ghostRightHandController.transform.eulerAngles;
                }
                else if (currDragDir == "z-axis")
                {
                    //solidRightHandController.transform.position = new Vector3(solidRightHandController.transform.position.x, solidRightHandController.transform.position.y, ghostRightHandController.transform.position.z);
                    projectedHookPos = detachPivot.transform.position + new Vector3(0, 0, offsetGhostDistance.z);
                    if (projectedHookPos.z < currCollider.bounds.max.z && projectedHookPos.z > currCollider.bounds.min.z)
                        detachPivot.transform.position = projectedHookPos;
                }
                //mistakeLineObj.SetActive(true);
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
        mistakeStartTime = (int)Time.time;

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

        mistakeEndTime = (int)Time.time;

        //mistake

        if(tag!="null") currTrainingIterationMistakeTime += (mistakeEndTime - mistakeStartTime);
        print("Current mistake time - " + currTrainingIterationMistakeTime);
        print("Previous mistake time - " + lastTrainingIterationMistakeTime);

        mistakeTimeIndicatorObj.GetComponent<Image>().fillAmount = ((float)currTrainingIterationMistakeTime / (float)lastTrainingIterationMistakeTime);
        Debug.Log("Mistake percentage: " + mistakeTimeIndicatorObj.GetComponent<Image>().fillAmount);

        ghostRightHandController.SetActive(false);

        solidRightHandController.transform.SetParent(hookRoot.transform);
        solidRightHandController.transform.localRotation = solidRightHandControllerDefaultRot;
        solidRightHandController.transform.localPosition = solidRightHandControllerDefaultPos;

    }

    public void triggerMistakeFeedback(string dir, Vector3 _mistakeDir, Vector3 _mistakeVector)
    {
        isFeedbackOnNow = true;
        //print("mistakeVector" + _mistakeVector);
        /*if (expCondition == ExperimentalCondition.VIBRATION)
        {
            vibrateInDirection(_mistakeDir);

        }*/



        //hapticArduinoSerialController.SendSerialMessage("1");
        //GetComponent<VibrationDemoScript>().TurnEffectOn();
        //StartCoroutine(Haptics(1, 1, 0.1f, true, false));
        //beepsound.mute = false;
        //mistakeLight.GetComponent<MeshRenderer>().material = lightOnMat;
        //mistakeLight.SetActive(true);

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

    public void stopMistakeFeedback()
    {
        isFeedbackOnNow = false;
        //vibrateInDirection(new Vector3(0, 0, 0));
        //HapticsDeviceManager.SetForce(new Vector3(0, 0, 0));
        //double[] zero = { 0.0, 0.0, 0.0 };
        //HapticPlugin.setForce("Default Device", zero, zero);
        //hapticArduinoSerialController.SendSerialMessage("0");

        //beepsound.mute = true;
        //GetComponent<VibrationDemoScript>().TurnEffectOff();


        //mistakeLight.SetActive(false);
    }

    void setAllLevelsInactive()
    {
        //tutorial.SetActive(false);
        //level1.SetActive(false);
        //level2.SetActive(false);
        //level3.SetActive(false);
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

    /*public void changeIntensityOfGhost(float level)
    {
        // Change alpha of ghostRightHandController material
        ghostRightHandController.GetComponent<Renderer>().material.color = new Color(ghostRightHandController.GetComponent<Renderer>().material.color.r, ghostRightHandController.GetComponent<Renderer>().material.color.g, ghostRightHandController.GetComponent<Renderer>().material.color.b, level);
        //ghost_handle.GetComponent<Renderer>().material.color = new Color(ghostRightHandController.GetComponent<Renderer>().material.color.r, ghostRightHandController.GetComponent<Renderer>().material.color.g, ghostRightHandController.GetComponent<Renderer>().material.color.b, level);
        ghost_wire.GetComponent<Renderer>().material.color = new Color(ghostRightHandController.GetComponent<Renderer>().material.color.r, ghostRightHandController.GetComponent<Renderer>().material.color.g, ghostRightHandController.GetComponent<Renderer>().material.color.b, level);
    }*/

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
            setAllLevelsInactive();
            levelTimeResultsObj.SetActive(false);
            levelObj.SetActive(true);
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

    float translateFactor = 0.001f;

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
                modeTxt.text = "Pre Test Active";
                if (client != null)
                    client.Write("M;1;;;pre_test_started;\r\n");
                break;
            case "VR_TUTORIAL":
                env.SetActive(true);
                surveyPanel.SetActive(false);
                modeTxt.text = "Tutorial Mode On";
                hookRoot.SetActive(true);
                //cylinderPointer.SetActive(false);

                expState = ExperimentState.VR_TUTORIAL;
                modeTxt.text = "Tutorial";
                setAllLevelsInactive();
                //tutorial.SetActive(true);
                break;
            case "TRAINING":
                //env.SetActive(true);
                hookRoot.SetActive(true);
                //cylinderPointer.SetActive(false);
                expState = ExperimentState.TRAINING;
                modeTxt.text = "Training Medium Feedback";
  
                if (client != null)
                    client.Write("M;1;;;training_started;\r\n");
                break;

            case "TRAINING_SELF_EFFICACY":
                //env.SetActive(false);
                //hookRoot.SetActive(false);
                //levelTimeResultsObj.SetActive(false);
                surveyPanel.SetActive(true);
                hookRoot.SetActive(false);
                arrowObj.SetActive(true);
                //formHandler.startSurveyMediumPost();
                modeTxt.text = "SE";
                expState = ExperimentState.TRAINING_SELF_EFFICACY;
                //if (client != null)
                //    client.Write("M;1;;;medium_post_survey_started;\r\n");
                break;


            case "POST_TEST":
                surveyPanel.SetActive(false);
                expState = ExperimentState.POST_TEST;
                modeTxt.text = "Post Test Active";
                if (client != null)
                    client.Write("M;1;;;post_test_started;\r\n");
                break;
            default:
                break;
        }
    }

    public void showLevelResult(int _levelTimeResult)
    {
        levelTimeResultsObj.SetActive(true);
        levelTimeResultsObj.transform.GetChild(0).GetChild(0).GetComponent<TMPro.TMP_Text>().text = "You took " + _levelTimeResult + " seconds. \n Come on, you can do it faster!";

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

    public void decideNextTrial()
    {
        surveyPanel.SetActive(false);
        expState = ExperimentState.TRAINING;

        //speedSEVal = speedSESlider.value;
        //mistakeSEVal = mistakeSESlider.value;

        //Logic for selecting next primer
        if (speedSEVal > mistakeSEVal)
        {
            print("Speed SE is higher");
            selectMistakePrimer();
        }
        else if (speedSEVal < mistakeSEVal)
        {
            print("Mistake SE is higher");
            selectSpeedPrimer();
        }
        else //Equal so randomly choose
        {
            //Randomly choose between invoking selectSpeedPrimer or selectMistakePrimer
            print("Mistake SE and Speed SE are equal. Randomly choosing between speed and mistake primer - ");
            float rand = UnityEngine.Random.Range(0, 9);
            if ((int)rand % 2 == 0)
            {
                print("Speed primer chosen");
                selectSpeedPrimer();
            }
            else
            {
                print("Mistake primer chosen");
                selectMistakePrimer();
            }
        }
        //Reset sliders to middle
        //speedSESlider.value = (speedSESlider.maxValue + speedSESlider.minValue) / 2;
        //mistakeSESlider.value = (mistakeSESlider.maxValue + mistakeSESlider.minValue) / 2;
        arrowObj.SetActive(false);
        hookRoot.SetActive(true);        
        arrow.resetSlider();
    }
    
    public void selectSpeedPrimer()
    {
        Debug.Log("Speed Primer Selected");
        mistakePrimer.SetActive(false);
        speedPrimer.SetActive(true);
        //Check condition and calculate next speed primer's speed based on either previous speed or pretest speed
        nextTrainingIterationSpeed = lastTrainingIterationSpeed * 1.1f; // Increase speed by 10%
        Debug.Log("Current speed is - " + lastTrainingIterationSpeed);
        changeSpeed(nextTrainingIterationSpeed);
        startSpeedPrimer();
    }

    public void selectMistakePrimer()
    {
        Debug.Log("Mistake Primer Selected");        
        lastTrainingIterationMistakeTime = currTrainingIterationMistakeTime * 0.9f; // Decrease mistake time by 10%
        Debug.Log("Last mistake time was - " + lastTrainingIterationMistakeTime);
        currTrainingIterationMistakeTime = 0;
        mistakeTimeIndicatorObj.GetComponent<Image>().fillAmount = 0;
        mistakePrimer.transform.position = mistakePrimerStartRefPos;
        mistakePrimer.SetActive(true);
        speedPrimer.SetActive(false);
    }

    public void changeSpeed(float newSpeed)
    {
        speed = newSpeed;
    }

    public void changeSpeedTxt()
    {
        prevSpeedTxt.text = "Previous training speed " + lastTrainingIterationSpeed;
        currSpeedTxt.text = "Current primer speed " + lastTrainingIterationSpeed;
    }

    public void startSpeedPrimer()
    {
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



}
