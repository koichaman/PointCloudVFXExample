using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

// [ExecuteInEditMode]
public class Integral : MonoBehaviour
{
    public GameObject cube;
    private GameObject[,] cubes;
    private string PATH_TO_PCACHE_DIR ="/PointCache/";
    [Tooltip("***.pcache in Assets/PointCache")]
    [SerializeField]private string PCACHE_FILE_NAME;
    [SerializeField]private string PLANE_PCACHE_FILE_NAME;
    [Tooltip("Mesh size [m]")]
    [SerializeField]private float MESH_SIZE;
    [Tooltip("The base elevation used for integral [m]")]
    [SerializeField]private float BASE_ELE;
    [SerializeField]private List<Vector2> poly;
    // [SerializeField]private float DENSE_AREA_SIZE;
    private List<Vector3> points;
    private float xMax, xMin, zMax, zMin;
    private float vol = 0f;
    private int xMeshCnt, zMeshCnt, xBias, zBias;
    private float[,] mesh, basePlaneMesh; //the center point of mesh[x,z] is [(x+xBias)*MESH_SIZE, (z+zBias)*MESH_SIZE]
    private int[,] numPoints; //the number of points which are included in the mesh
    private float edgeValue = -10000;
    private float excludedValue = -200000;
    private string sintyoku;
    private int fCount = 0;


    // Start is called before the first frame update
    void Start()
    {
        initMesh();
        initBasePlane();
        // complement basePlaneMesh
        compBasePlane();
        // zoning of  the integral area
        zoneMask();
        compBasePlaneVisualization();
        // calculate Volume
        vol = calcVolume();
        Debug.Log("Calcurated volume is "+vol+"m^3");
    }
    // Update is called once per frame
    void Update()
    {
    }
    // Start is called before the first frame update
    void initMesh()
    {
        // import xyz data from ***.pcache to "points"
        loadPcache(Application.dataPath + PATH_TO_PCACHE_DIR + PCACHE_FILE_NAME);
        //calcurate max and min of x and z
        xMax = points[0].x;
        xMin = points[0].x;
        zMax = points[0].z;
        zMin = points[0].z;
        foreach(Vector3 point in points){
            if(xMax<point.x) xMax = point.x;
            else if(xMin>point.x) xMin = point.x;
            if(zMax<point.z) zMax = point.z;
            else if(zMin>point.z) zMin = point.z;
        }
        // start point of mesh
        xBias = Mathf.RoundToInt(xMin/MESH_SIZE);
        zBias = Mathf.RoundToInt(zMin/MESH_SIZE);
        // number of meshes
        xMeshCnt = Mathf.RoundToInt(xMax/MESH_SIZE) - xBias + 1;
        zMeshCnt = Mathf.RoundToInt(zMax/MESH_SIZE) - zBias + 1;
        // initialize the array of mesh and number of points
        mesh = new float[xMeshCnt, zMeshCnt];
        numPoints = new int[xMeshCnt, zMeshCnt];
        cubes = new GameObject[xMeshCnt, zMeshCnt];
        for(int z=0; z<zMeshCnt; z++){
            for(int x=0; x<xMeshCnt; x++){
                mesh[x,z] = 0f;;
                numPoints[x,z] = 0;
            }
        }
        // added y value to the corresponding mesh
        // and count the number of points which is included in a mesh
        foreach(Vector3 pnt in points){
            int x = Mathf.RoundToInt(pnt.x/MESH_SIZE)-xBias;
            int z = Mathf.RoundToInt(pnt.z/MESH_SIZE)-zBias;
            mesh[x,z] += pnt.y;
            numPoints[x,z]++;
        }
        // calcurate the average y value of each mesh
        for(int z=0; z<zMeshCnt; z++){
            for(int x=0; x<xMeshCnt; x++){
                if(numPoints[x,z]!=0) mesh[x,z]/=numPoints[x,z];
                else mesh[x,z] = excludedValue;
            }
        }
        // visualization
        for(int z=0; z<zMeshCnt; z++){
            for(int x=0; x<xMeshCnt; x++){
                float height = mesh[x,z];
                if(height!=excludedValue){
                    Vector3 pos = new Vector3((x+xBias)*MESH_SIZE, (height-BASE_ELE)/2f+BASE_ELE, (z+zBias)*MESH_SIZE);
                    Vector3 scale = new Vector3(MESH_SIZE, height-BASE_ELE, MESH_SIZE);
                    cubes[x,z] = Instantiate(cube, pos, Quaternion.identity);
                    cubes[x,z].transform.localScale = scale;
                }
            }
        }
    }

    void initBasePlane(){
        loadPcache(Application.dataPath + PATH_TO_PCACHE_DIR + PLANE_PCACHE_FILE_NAME);
        basePlaneMesh = new float[xMeshCnt, zMeshCnt];
        for(int z=0; z<zMeshCnt; z++){
            for(int x=0; x<xMeshCnt; x++){
                basePlaneMesh[x,z] = 0f;;
                numPoints[x,z] = 0;
            }
        }
        // added y value to the corresponding mesh
        // and count the number of points which is included in a mesh
        foreach(Vector3 pnt in points){
            int x = Mathf.RoundToInt(pnt.x/MESH_SIZE)-xBias;
            int z = Mathf.RoundToInt(pnt.z/MESH_SIZE)-zBias;
            if(x>=0 && x<xMeshCnt && z>=0 && z<zMeshCnt){
                basePlaneMesh[x,z] += pnt.y;
                numPoints[x,z]++;
            }
        }
        // calcurate the average y value of each mesh
        for(int z=0; z<zMeshCnt; z++){
            for(int x=0; x<xMeshCnt; x++){
                if(numPoints[x,z]!=0) basePlaneMesh[x,z]/=numPoints[x,z];
                else basePlaneMesh[x,z] = excludedValue;
            }
        }
    }

    void loadPcache(string pcachePath){
        points = new List<Vector3>();
        StreamReader sr = new StreamReader(pcachePath);
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
    }

    void zoneMask(){
        // convert poly points to mesh positions
        for(int i=0; i<poly.Count; i++){
            Vector2 p = poly[i];
            p.x = Mathf.Round(poly[i].x/MESH_SIZE)-(float)xBias;
            p.y = Mathf.Round(poly[i].y/MESH_SIZE)-(float)zBias;
            poly[i] = p;
        }
        // set the value of meshes on polyline to edgeValue
        for(int i=0; i<poly.Count; i++){
            int next = (i+1)%poly.Count;
            int diffX = (int)(poly[next].x-poly[i].x);
            if(diffX!=0){
                float slope = (poly[next].y-poly[i].y)/(poly[next].x-poly[i].x);
                float intercept = (poly[next].x*poly[i].y - poly[i].x*poly[next].y)/(poly[next].x - poly[i].x);
                for(int j=0; Mathf.Abs(j)<Mathf.Abs(diffX); j+=(int)Mathf.Sign(diffX)){
                    int now = Mathf.RoundToInt(slope*(poly[i].x+j)+intercept);
                    int nextZ = Mathf.RoundToInt(slope*(poly[i].x+j+(int)Mathf.Sign(diffX))+intercept);
                    mesh[(int)poly[i].x+j, now] = edgeValue;
                    cubes[(int)poly[i].x+j, now].GetComponent<Renderer>().material.SetColor("_BaseColor", Color.blue);
                    for(int k=(int)Mathf.Sign(nextZ-now); Mathf.Abs(k)<Mathf.Abs(nextZ-now); k+=(int)Mathf.Sign(nextZ-now)){
                        mesh[(int)poly[i].x+j, now+k] = edgeValue;
                        cubes[(int)poly[i].x+j, now+k].GetComponent<Renderer>().material.SetColor("_BaseColor", Color.blue);
                    }
                }
            }
            else{
                int diffZ = (int)(poly[next].y-poly[i].y);
                for(int j=0; Mathf.Abs(j)<=Mathf.Abs(diffZ); j+=(int)Mathf.Sign(diffZ)){
                    mesh[(int)poly[i].x, (int)poly[i].y+j] = edgeValue;
                    cubes[(int)poly[i].x, (int)poly[i].y+j].GetComponent<Renderer>().material.SetColor("_BaseColor", Color.blue);
                }
            }
        }
        // set the value of meshes outside of poly to 0
        bool keepLoop = true;
        while(keepLoop){
            keepLoop = false;
            List<int[]> excludeKeys = new List<int[]>();
            for(int x=0; x<xMeshCnt; x++){
                for(int z=0; z<zMeshCnt; z++){
                    excludeKeys = new List<int[]>();
                    if(mesh[x,z]==excludedValue){
                        if(excludeKeys.Count!=0){
                            foreach(int[] key in excludeKeys){
                                mesh[key[0],key[1]]=excludedValue;
                                cubes[key[0],key[1]].GetComponent<Renderer>().GetComponent<Renderer>().material.SetColor("_BaseColor", Color.blue);
                            }
                            keepLoop = true;
                        }
                    }
                    else if(mesh[x,z]==edgeValue)excludeKeys = new List<int[]>();
                    else excludeKeys.Add(new int[2]{x,z});
                }
            }
            for(int z=0; z<zMeshCnt; z++){
                excludeKeys = new List<int[]>();
                for(int x=0; x<xMeshCnt; x++){
                    if(mesh[x,z]==excludedValue){
                        if(excludeKeys.Count!=0){
                            foreach(int[] key in excludeKeys){
                                mesh[key[0],key[1]]=excludedValue;
                                cubes[key[0],key[1]].GetComponent<Renderer>().GetComponent<Renderer>().material.SetColor("_BaseColor", Color.blue);
                            }
                            keepLoop = true;
                        }
                    }
                    else if(mesh[x,z]==edgeValue)excludeKeys = new List<int[]>();
                    else excludeKeys.Add(new int[2]{x,z});
                }
            }
            for(int x=xMeshCnt-1; x>=0; x--){
                excludeKeys = new List<int[]>();
                for(int z=zMeshCnt-1; z>=0; z--){
                    if(mesh[x,z]==excludedValue){
                        if(excludeKeys.Count!=0){
                            foreach(int[] key in excludeKeys){
                                mesh[key[0],key[1]]=excludedValue;
                                cubes[key[0],key[1]].GetComponent<Renderer>().GetComponent<Renderer>().material.SetColor("_BaseColor", Color.blue);
                            }
                            keepLoop = true;
                        }
                    }
                    else if(mesh[x,z]==edgeValue)excludeKeys = new List<int[]>();
                    else excludeKeys.Add(new int[2]{x,z});
                }
            }
            for(int z=zMeshCnt-1; z>=0; z--){
                excludeKeys = new List<int[]>();
                for(int x=xMeshCnt-1; x>=0; x--){
                    if(mesh[x,z]==excludedValue){
                        if(excludeKeys.Count!=0){
                            foreach(int[] key in excludeKeys){
                                mesh[key[0],key[1]]=excludedValue;
                                cubes[key[0],key[1]].GetComponent<Renderer>().GetComponent<Renderer>().material.SetColor("_BaseColor", Color.blue);
                            }
                            keepLoop = true;
                        }
                    }
                    else if(mesh[x,z]==edgeValue)excludeKeys = new List<int[]>();
                    else excludeKeys.Add(new int[2]{x,z});
                }
            }
        }
    }

    void compBasePlane(){
        bool keepLoop = true;
        while(keepLoop){
            keepLoop = false;
            for(int x=0; x<xMeshCnt; x++){
                for(int z=0; z<zMeshCnt; z++){
                    if(mesh[x,z]!=excludedValue && basePlaneMesh[x,z]==excludedValue){
                        List<int[]> nearIndex = makeNearIndex(x,z);
                        int num = 0;
                        float ave = 0;
                        foreach(int[] index in nearIndex){
                            if(basePlaneMesh[index[0],index[1]]!=excludedValue){
                                ave+=basePlaneMesh[index[0],index[1]];
                                num++;
                            }
                        }
                        if(num > 0){
                            ave /= (float)num;
                            basePlaneMesh[x,z] = ave;
                            keepLoop = true;
                        }
                    }
                }
            }
        }
    }

    void compBasePlaneVisualization(){
        for(int x=0; x<xMeshCnt; x++){
            for(int z=0; z<zMeshCnt; z++){
                if(mesh[x,z]!=excludedValue && basePlaneMesh[x,z]==excludedValue){
                    cubes[x,z].GetComponent<Renderer>().material.SetColor("_BaseColor", Color.red);
                    float trueX = (x+xBias)*MESH_SIZE;
                    float trueZ = (z+zBias)*MESH_SIZE;
                }
            }
        }
    }

    List<int[]> makeNearIndex(int inputX, int inputZ){
        List<int[]> result = new List<int[]>();
        bool xIsMin = (inputX==0);
        bool xIsMax = (inputX==xMeshCnt-1);
        bool zIsMin = (inputZ==0);
        bool zIsMax = (inputZ==zMeshCnt-1);
        for(int x=inputX-1*Convert.ToInt32(!xIsMin); x<=inputX+1*Convert.ToInt32(!xIsMax); x++){
            for(int z=inputZ-1*Convert.ToInt32(!zIsMin); z<=inputZ+1*Convert.ToInt32(!zIsMax); z++){
                if(x!=inputX||z!=inputZ)result.Add(new int[2]{x,z});
            }
        }
        return result;
    }

    float calcVolume(){
        float vol = 0;
        for(int x=0; x<xMeshCnt; x++){
            for(int z=0; z<zMeshCnt; z++){
                if(mesh[x,z]!=excludedValue && mesh[x,z]!=edgeValue)vol+=(mesh[x,z]-basePlaneMesh[x,z])*MESH_SIZE*MESH_SIZE;
            }
        }
        return vol;
    }

}
