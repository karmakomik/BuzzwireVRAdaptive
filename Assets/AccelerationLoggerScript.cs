using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class AccelerationLoggerScript : MonoBehaviour
{
    public StreamWriter acc_sw;
    public Transform rightController;

    public void logEventHeader(string ev)
    {
        if (acc_sw.BaseStream != null)
        {
            acc_sw.WriteLine(ev + ", " + System.DateTime.Now.ToString("HH:mm:ss"));
            //acc_sw.WriteLine("Time - Velocity - Angular Velocity");
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (acc_sw.BaseStream != null)
        {
            acc_sw.WriteLine("" + System.DateTime.Now.ToString("HH:mm:ss:fff") + " - " + rightController.position.ToString("F8") + " - " + rightController.eulerAngles.ToString("F8"));
        }
    }
}
