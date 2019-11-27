using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;

public class Integral : MonoBehaviour
{
    public GameObject cube;
    private GameObject[,] cubes;
    private string PATH_TO_PCACHE_DIR ="/PointCache/";
    [Tooltip("***.pcache in Assets/PointCache")]
    [SerializeField]private string PCACHE_FILE_NAME;
    [Tooltip("Mesh size [m]")]
    [SerializeField]private float MESH_SIZE;
    [Tooltip("The base elevation used for integral [m]")]
    [SerializeField]private float BASE_ELE;
    // [SerializeField]private float DENSE_AREA_SIZE;
    private List<Vector3> points = new List<Vector3>();
    private float xMax, xMin, zMax, zMin;
    private float vol = 0f;
    private int xMeshCnt, zMeshCnt, xBias, zBias;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(Application.dataPath + PATH_TO_PCACHE_DIR + PCACHE_FILE_NAME);
        StreamReader sr = new StreamReader(Application.dataPath + PATH_TO_PCACHE_DIR + PCACHE_FILE_NAME);
        bool isHeader = true;
        float[,] mesh; //the center point of mesh[x,z] is [(x+xBias)*MESH_SIZE, (z+zBias)*MESH_SIZE]
        int[,] numPoints; //the number of points which are included in the mesh

        // import xyz data from ***.pcache to "points"
        // and calcurate max and min of x and z
        while(!sr.EndOfStream){
            string line = sr.ReadLine();
            if(!isHeader && line!=null){
                string[] strVals = line.Split(' ');
                Vector3 point = new Vector3(float.Parse(strVals[0]), float.Parse(strVals[2]), float.Parse(strVals[1]));
                points.Add(point);
                if(points.Count==1){
                    xMax = point.x;
                    xMin = point.x;
                    zMax = point.z;
                    zMin = point.z;
                }
                else{
                    if(xMax<point.x) xMax = point.x;
                    else if(xMin>point.x) xMin = point.x;
                    if(zMax<point.z) zMax = point.z;
                    else if(zMin>point.z) zMin = point.z;
                }
            }
            if(line=="end_header") isHeader = false;
        }

        // float dense = 0;
        // float xRange = xMax-xMin;
        // float zRange = zMax-zMin;
        // float coeff = (1f-DENSE_AREA_SIZE)/2f;
        // foreach(Vector3 pnt in points){
        //     bool xFlag = pnt.x<=xMax-xRange*coeff && pnt.x>=xMin+xRange*coeff;
        //     bool zFlag = pnt.z<=zMax-zRange*coeff && pnt.z>=zMin+zRange*coeff;
        //     if(xFlag && zFlag) dense++;
        // }
        // dense /= xRange*DENSE_AREA_SIZE*zRange*DENSE_AREA_SIZE;
        // Debug.Log("Dense: "+dense.ToString()+"[/m^2]");

        // start point of mesh
        xBias = Mathf.RoundToInt(xMin/MESH_SIZE);
        zBias = Mathf.RoundToInt(zMin/MESH_SIZE);
        // number of meshes
        xMeshCnt = Mathf.RoundToInt(xMax/MESH_SIZE) - xBias + 1;
        zMeshCnt = Mathf.RoundToInt(zMax/MESH_SIZE) - zBias + 1;
        // initialize the array of mesh and number of points
        mesh = new float[xMeshCnt, zMeshCnt];
        numPoints = new int[xMeshCnt, zMeshCnt];
        for(int z=0; z<zMeshCnt; z++){
            for(int x=0; x<xMeshCnt; x++){
                mesh[x,z] = 0f;
                numPoints[x,z] = 0;
            }
        }
        // added y value to the corresponding mesh
        // and count the number of points which is included in a mesh
        foreach(Vector3 pnt in points){
            int x = Mathf.RoundToInt(pnt.x/MESH_SIZE)-xBias;
            int z = Mathf.RoundToInt(pnt.z/MESH_SIZE)-zBias;
            mesh[x,z] += (pnt.y - BASE_ELE);
            numPoints[x,z]++;
        }
        // calcurate the average y value of each mesh
        // and calcurate the volume
        for(int z=0; z<zMeshCnt; z++){
            for(int x=0; x<xMeshCnt; x++){
                if(numPoints[x,z]!=0) mesh[x,z]/=numPoints[x,z];
                if(mesh[x,z]>0) vol += mesh[x,z]*MESH_SIZE*MESH_SIZE;
            }
        }

        // visualization
        cubes = new GameObject[xMeshCnt,zMeshCnt];
        for(int z=0; z<zMeshCnt; z++){
            for(int x=0; x<xMeshCnt; x++){
                float height = mesh[x,z]+BASE_ELE;
                Vector3 pos = new Vector3((x+xBias)*MESH_SIZE, height/2f, (z+zBias)*MESH_SIZE);
                cubes[x,z] = Instantiate(cube, pos, Quaternion.identity);
                Vector3 scale = new Vector3(MESH_SIZE, height, MESH_SIZE);
                cubes[x,z].transform.localScale = scale;
                if(height==0f)cubes[x,z].GetComponent<Renderer>().material.color = Color.red;
            }
        }
        Debug.Log(vol);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
