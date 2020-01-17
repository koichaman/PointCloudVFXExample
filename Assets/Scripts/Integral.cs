using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;

// [ExecuteInEditMode]
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
    [SerializeField]private List<Vector2> poly;
    // [SerializeField]private float DENSE_AREA_SIZE;
    private List<Vector3> points = new List<Vector3>();
    private float xMax, xMin, zMax, zMin;
    private float vol = 0f;
    private int xMeshCnt, zMeshCnt, xBias, zBias;
    private float[,] mesh; //the center point of mesh[x,z] is [(x+xBias)*MESH_SIZE, (z+zBias)*MESH_SIZE]
    private int[,] numPoints; //the number of points which are included in the mesh
    private float excludeValue = -10000;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(Application.dataPath + PATH_TO_PCACHE_DIR + PCACHE_FILE_NAME);
        StreamReader sr = new StreamReader(Application.dataPath + PATH_TO_PCACHE_DIR + PCACHE_FILE_NAME);
        bool isHeader = true;

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
        cubes = new GameObject[xMeshCnt, zMeshCnt];
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
        // preparing visualization cubes
        for(int z=0; z<zMeshCnt; z++){
            for(int x=0; x<xMeshCnt; x++) cubes[x,z] = Instantiate(cube, new Vector3(0f, 0f, 0f), Quaternion.identity);
        }
        // zoning of  the integral area
        zoneMask();
        // visualization
        for(int z=0; z<zMeshCnt; z++){
            for(int x=0; x<xMeshCnt; x++){
                float height = mesh[x,z]+BASE_ELE;
                Vector3 pos = new Vector3((x+xBias)*MESH_SIZE, height/2f, (z+zBias)*MESH_SIZE);
                Vector3 scale = new Vector3(MESH_SIZE, Mathf.Abs(height), MESH_SIZE);
                Transform tr = cubes[x,z].transform;
                tr.position = pos;
                tr.localScale = scale;
                if(height==0f)cubes[x,z].GetComponent<Renderer>().material.color = Color.red;
            }
        }
        Debug.Log(vol);
    }

    // Update is called once per frame
    void Update()
    {

    }

    void zoneMask(){
        // convert poly points to mesh positions
        for(int i=0; i<poly.Count; i++){
            Vector2 p = poly[i];
            p.x = Mathf.Round(poly[i].x/MESH_SIZE)-(float)xBias;
            p.y = Mathf.Round(poly[i].y/MESH_SIZE)-(float)zBias;
        }
        // set the value of meshes on polyline to 0
        for(int i=0; i<poly.Count; i++){
            Debug.Log("i:"+i);
            int next = (i+1)%poly.Count;
            int diffX = (int)(poly[next].x-poly[i].x);
            if(diffX!=0){
                float slope = (poly[next].y-poly[i].y)/(poly[next].x-poly[i].x);
                float intercept = (poly[next].x*poly[i].y - poly[i].x*poly[next].y)/(poly[next].x - poly[i].x);
                for(int j=0; Mathf.Abs(j)<Mathf.Abs(diffX); j+=(int)Mathf.Sign(diffX)){
                    Debug.Log("j:"+j);
                    int now = Mathf.RoundToInt(slope*(poly[i].x+j)+intercept);
                    int nextZ = Mathf.RoundToInt(slope*(poly[i].x+j+1)+intercept);
                    Debug.Log("slope:"+slope+" intercept:"+intercept);
                    Debug.Log("now:"+now+" nextY:"+nextZ);
                    mesh[(int)poly[i].x+j, now] = excludeValue;
                    cubes[(int)poly[i].x+j, now].GetComponent<Renderer>().material.color = Color.blue;
                    for(int k=1; Mathf.Abs(k)<Mathf.Abs(nextZ-now); k+=(int)Mathf.Sign(nextZ-now)){
                        Debug.Log("k:"+k);
                        mesh[(int)poly[i].x+j, now+k] = excludeValue;
                        cubes[(int)poly[i].x+j, now+k].GetComponent<Renderer>().material.color = Color.blue;
                    }
                }
            }
            else{
                int diffZ = (int)(poly[next].y-poly[i].y);
                for(int j=0; Mathf.Abs(j)<=Mathf.Abs(diffZ); j+=(int)Mathf.Sign(diffZ)){
                    mesh[(int)poly[i].x, (int)poly[i].y+j] = excludeValue;
                    cubes[(int)poly[i].x, (int)poly[i].y+j].GetComponent<Renderer>().material.color = Color.blue;
                }
            }
        }
        // set the value of meshes outside of poly to 0
        bool keepLoop = true;
        while(keepLoop){
            keepLoop = false;
            List<int> excludeKeys;
            for(int x=0; x<xMeshCnt; x++){
                for(int z=0; z<zMeshCnt; z++){
                    // kokokara
                }
            }

        }
    }
}
