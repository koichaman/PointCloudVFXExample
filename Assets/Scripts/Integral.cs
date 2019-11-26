using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;

public class Integral : MonoBehaviour
{
    private string PATH_TO_PCACHE_DIR ="/PointCache/";
    [Tooltip("***.pcache in Assets/PointCache")]
    [SerializeField]private string PCACHE_FILE_NAME;
    private List<Vector3> points = new List<Vector3>();
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(Application.dataPath + PATH_TO_PCACHE_DIR + PCACHE_FILE_NAME);
        StreamReader sr = new StreamReader(Application.dataPath + PATH_TO_PCACHE_DIR + PCACHE_FILE_NAME);
        bool isHeader = true;
        while(!sr.EndOfStream){
            string line = sr.ReadLine();
            if(!isHeader && line!=null){
                string[] strVals = line.Split(' ');
                Vector3 point = new Vector3(float.Parse(strVals[0]), float.Parse(strVals[2]), float.Parse(strVals[1]));
                points.Add(point);
            }
            if(line=="end_header") isHeader = false;
        }
        Debug.Log(points[0]);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
