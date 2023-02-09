using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class StartSceneScript : MonoBehaviour
{
    public TMP_InputField testCOMPort;

    // Start is called before the first frame update
    void Start()
    {
        testCOMPort.text = PlayerPrefs.GetString("testCOMPort", "not_set");
        //Debug.Log("testCOMPort" + testCOMPort.text);

        /*if (PlayerPrefs.HasKey("testCOMPort"))
        {
            Debug.Log("The key " + "testCOMPort" + " exists");
        }
        else
            Debug.Log("The key " + "testCOMPort" + " does not exist");*/
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void setTestCOMPort()
    {
        //Debug.Log("Current com port - " + testCOMPort.text);
        PlayerPrefs.SetString("testCOMPort", testCOMPort.text);
        PlayerPrefs.Save();
    }

    public void selectExperimentCondition(string expCondStr)
    {
        switch (expCondStr)
        {
            case "Control":
                ExperimentManagerScript.expCondition = ExperimentalCondition.CONTROL;
                startExperiment();
                break;
            case "Adaptive":
                ExperimentManagerScript.expCondition = ExperimentalCondition.ADAPTIVE;
                startExperiment();
                break;
            default:
                print("Invalid experimental condition");
                break;
        }
    }

    public void startExperiment()
    {        
        SceneManager.LoadScene("ExperimentScene");
    }

}
