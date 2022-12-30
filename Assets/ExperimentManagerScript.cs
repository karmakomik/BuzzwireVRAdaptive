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
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator MoveRing()
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
        
    }

}
