using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExperimentManagerScript : MonoBehaviour
{
    public List<GameObject> anchorsLst;
    float fraction;
    int listPos;
    public GameObject ring;
    // Start is called before the first frame update
    void Start()
    {
        listPos = 0;
        fraction = 0;
        //anchorsLst = new List<GameObject>();
        StartCoroutine(MoveRing());
        //StartCoroutine(MoveFromTo(ring.transform,anchorsLst[1].transform, anchorsLst[2].transform, 0.01f));
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //Write a coroutine to iterate through a list of gameobjects, call the MoveFromTo function on each pair of gameobjects 
    //and wait for the coroutine to finish before moving to the next pair of gameobjects
    IEnumerator MoveRing()
    {
        while (true)
        {
            yield return StartCoroutine(MoveFromTo(ring.transform, anchorsLst[listPos].transform, anchorsLst[listPos + 1].transform, 0.01f));            
            if (listPos < anchorsLst.Count - 2)
            {
                listPos++;                
            }
            else
                break;
        }
    }


    /*IEnumerator MoveRing()
    { 
        while (listPos < anchorsLst.Count-1)
        {
            Debug.Log("loop");
            ring.transform.position = Vector3.Lerp(anchorsLst[listPos].transform.position, anchorsLst[listPos + 1].transform.position, fraction);
            ring.transform.rotation = Quaternion.Lerp(anchorsLst[listPos].transform.rotation, anchorsLst[listPos + 1].transform.rotation, fraction);
            yield return new WaitForSecondsRealtime(0.02f);
            if (fraction < 1.0f)
                fraction += 0.04f;
            else
            {
                fraction = 0;
                listPos += 1;
            }
        }
        
    }*/
    
    IEnumerator MoveFromTo(Transform objectToMove, Transform a, Transform b, float speed) //Adapted from https://gamedev.stackexchange.com/questions/100535/coroutine-to-move-to-position-passing-the-movement-speed
    {
        float step = (speed / (a.position - b.position).magnitude) * Time.fixedDeltaTime;
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

}
