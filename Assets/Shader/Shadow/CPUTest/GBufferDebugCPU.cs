using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GBufferDebugCPU : MonoBehaviour {
    public MeshFilter shadowCastMeshFilter;

    public GameObject shadowReceivePoint;

    public Vector3 lightDirection;

    public Vector3 localPoint;

    public float closeOut = 0.0000001f;
    public float dotOut = -0.001f;

    public bool debugGPU;
    public bool calculate;

    public bool hideSnapSphere;

    [Range(0.0f, 1.0f)]
    public float pointSize = 0.01f; 

    [SerializeField]
    private Vector3[] vertices;

    private List<Vector3> shadowVertices = new List<Vector3>();
    private List<Vector3> snapPos = new List<Vector3>();

    [SerializeField]
    private SphereDebugCPU sphereDebugCPU;

    private List<SphereDebugCPU> sphereDebugs = new List<SphereDebugCPU>();

    public void Start() {
        //vertices = shadowCastMeshFilter.mesh.vertices;
        //List<int> triangle = new List<int>();
        //triangle.AddRange(shadowCastMeshFilter.mesh.triangles);
        //triangle.Remove(triangle.Count - 1);
        //shadowCastMeshFilter.mesh.SetTriangles(triangle.ToArray(), 0);
    }

    public void Update() {
        if (calculate)
        {
            if(debugGPU) { 
                WorldConstructPass.readBuffer = true;
                StartCoroutine(CalculateScreenShadow());
            } else {
                StartCoroutine(CalculateShadow());
            }
            calculate = false;
        }

        if (Input.GetMouseButtonUp(0)) {
            foreach (Vector3 point in shadowVertices) {
            }
        }
    }

    public void OnDrawGizmos() {
        if(!calculate)
        {

            Gizmos.color = Color.black;
            foreach (Vector3 point in shadowVertices)
            {
                Gizmos.DrawSphere(point, pointSize);
            }

            if(!hideSnapSphere) { 
                Gizmos.color = Color.white;
                foreach (Vector3 point in snapPos)
                {
                    Gizmos.DrawSphere(point, pointSize / 2);
                }
            }
        }
    }

    public IEnumerator CalculateShadow() { 
        shadowVertices.Clear();
        snapPos.Clear();

        MeshFilter meshFilter = shadowReceivePoint.GetComponent<MeshFilter>();
        Vector3[] vertices = meshFilter.sharedMesh.vertices;

        int iteratorTime = 500;

        Vector3 localLightDirection = shadowCastMeshFilter.transform.InverseTransformDirection(lightDirection);
        localLightDirection.Normalize();

        string lPoints = string.Empty;

        for(int i = 0; i < sphereDebugs.Count; i++) {
            Destroy(sphereDebugs[i].gameObject);   
        }
        sphereDebugs.Clear();

        foreach (Vector3 point in vertices) {
            Vector3 worldPosition = shadowReceivePoint.transform.TransformPoint(point);
            Vector3 localPosition = shadowCastMeshFilter.transform.InverseTransformPoint(worldPosition);
            //localPosition = localPoint;

            lPoints += localPosition.ToString() + "\n";

            for (int i = 0; i < shadowCastMeshFilter.mesh.triangles.Length; i += 3) {
                if(--iteratorTime <= 0) { 
                    iteratorTime = 500;
                    yield return null;
                }

                int indexA = shadowCastMeshFilter.mesh.triangles[i + 0];
                int indexB = shadowCastMeshFilter.mesh.triangles[i + 1];
                int indexC = shadowCastMeshFilter.mesh.triangles[i + 2];

                Vector3 pointA = shadowCastMeshFilter.mesh.vertices[indexA];
                Vector3 pointB = shadowCastMeshFilter.mesh.vertices[indexB];
                Vector3 pointC = shadowCastMeshFilter.mesh.vertices[indexC];

                //Vector3[] points = new Vector3[] { pointA, pointB, pointC };

                //Gizmos.DrawLineStrip(points, true);

                Vector3 BAVec = pointB - pointA;
                Vector3 CAVec = pointC - pointA;

                Vector3 normal = Vector3.Cross(BAVec, CAVec).normalized;
                Vector3 normalA = Vector3.Cross(BAVec, CAVec);
                float area = normal.magnitude / 2;

                normal.Normalize();

                Vector3 center = (pointA + pointB + pointC) / 3;
                Vector3 vec = localPosition - center;
                float planeD = normal.x * pointA.x - normal.y * pointA.y - normal.z * pointA.z;

                // if point is above caster's surface then ignore
                float dotVN = Vector3.Dot(vec, normal);
                if (dotVN > 0)
                    continue;

                // if caster's surface is align to light direction then ignore
                float dotNLocalLight = Vector3.Dot(localLightDirection, normal);
                if (dotNLocalLight > dotOut)
                    continue;

                float d = Vector3.Dot(normal, pointA);
                float t = (dotVN + d) / dotNLocalLight;

                // if direction to closest point is opposite to received's surface normal 
                // or too close to receiver's point (consider as same point)
                // then ignore
                if (t <= closeOut)
                    continue;

                //Debug.Log(dotNLocalLight);

                // progject receiver's point(pixel) to caster's surface
                Vector3 snapLocalPosition = localPosition + (localLightDirection * -(t - planeD));
                snapPos.Add(shadowCastMeshFilter.transform.TransformPoint(snapLocalPosition));

                //Gizmos.DrawRay(shadowCastMeshFilter.transform.position, normal);
                //Gizmos.DrawWireSphere(shadowCastMeshFilter.transform.TransformPoint(localPosition), 0.05f);

                //Vector3 na = Vector3.Cross(pointA - pointC, snapLocalPosition - pointA);
                //Vector3 nb = Vector3.Cross(pointB - pointA, snapLocalPosition - pointB);
                //Vector3 nc = Vector3.Cross(pointC - pointB, snapLocalPosition - pointC);

                //float d_ab = Vector3.Dot(na, nb);
                //float d_bc = Vector3.Dot(nb, nc);


                //Debug.Log("S2: " + i + " " + d_ab + " " + d_bc);

                //// if projected point is out side surface then ignore
                //if (d_ab <= closeOut || d_bc <= closeOut)
                //    continue;

                Vector3 na = Vector3.Cross(pointB - pointA, snapLocalPosition - pointA);
                Vector3 nb = Vector3.Cross(snapLocalPosition - pointA, pointC - pointA);

                float alpha = Vector3.Dot(na, normalA) / (normalA.sqrMagnitude);
                float beta = Vector3.Dot(nb, normalA) / (normalA.sqrMagnitude);
                float gamma = 1 - alpha - beta;

                SphereDebugCPU debugSphere = Instantiate(sphereDebugCPU);
                debugSphere.transform.position = shadowCastMeshFilter.transform.TransformPoint(snapLocalPosition);
                debugSphere.alpha = alpha;
                debugSphere.beta = beta;
                debugSphere.gamma = gamma;
                sphereDebugs.Add(debugSphere);

                float inv_abg = 1 - (alpha + beta + gamma);

                bool isInside = alpha >= -0.005f && alpha <= 1.005f &&
                                beta >= -0.005f && beta <= 1.005f &&
                                gamma >= -0.005f && gamma <= 1.005f;

                // if projected point is out side surface then ignore
                if (!isInside)
                    continue;

                // find uv at projected point on surface
                float un = Vector3.Cross(pointA - pointB, pointA - pointC).magnitude;
                float ua = Vector3.Cross(pointB - snapLocalPosition, pointC - snapLocalPosition).magnitude / un;
                float ub = Vector3.Cross(pointC - snapLocalPosition, pointA - snapLocalPosition).magnitude / un;
                float uc = Vector3.Cross(pointA - snapLocalPosition, pointB - snapLocalPosition).magnitude / un;

                Vector2 uva = shadowCastMeshFilter.mesh.uv[indexA];
                Vector2 uvb = shadowCastMeshFilter.mesh.uv[indexB];
                Vector2 uvc = shadowCastMeshFilter.mesh.uv[indexC];

                Vector2 uv = uva * ua + uvb * ub + uvc * uc;
                //Debug.Log(na.magnitude + " " + ub + " " + uc + " " + un + " = " + uv);

                shadowVertices.Add(worldPosition);

                break;
            }
        }

        Debug.Log("S_LP" + lPoints);
    }

    public IEnumerator CalculateScreenShadow() {
        yield return new WaitUntil(() => { return !WorldConstructPass.readBuffer; });

        shadowVertices.Clear();
        snapPos.Clear();

        MeshFilter meshFilter = shadowReceivePoint.GetComponent<MeshFilter>();
        Vector3[] vertices = meshFilter.sharedMesh.vertices;

        int iteratorTime = 500;

        Vector3 localLightDirection = shadowCastMeshFilter.transform.InverseTransformDirection(lightDirection);
        localLightDirection.Normalize();

        string lPoints = string.Empty;

        List<Vector4> screenWorldSpacePoints = WorldConstructPass.debugData.ToList();
        screenWorldSpacePoints.Reverse();

        foreach (Vector4 point in screenWorldSpacePoints) {
            Vector3 localPosition = point;
            Vector3 worldPosition = shadowCastMeshFilter.transform.TransformPoint(point);

            lPoints += point.ToString() + "\n";

            for (int i = 0; i < shadowCastMeshFilter.mesh.triangles.Length; i += 3) {
                if (--iteratorTime <= 0) {
                    iteratorTime = 500;
                    yield return null;
                }

                int indexA = shadowCastMeshFilter.mesh.triangles[i + 0];
                int indexB = shadowCastMeshFilter.mesh.triangles[i + 1];
                int indexC = shadowCastMeshFilter.mesh.triangles[i + 2];

                Vector3 pointA = shadowCastMeshFilter.mesh.vertices[indexA];
                Vector3 pointB = shadowCastMeshFilter.mesh.vertices[indexB];
                Vector3 pointC = shadowCastMeshFilter.mesh.vertices[indexC];

                //Vector3[] points = new Vector3[] { pointA, pointB, pointC };

                //Gizmos.DrawLineStrip(points, true);

                Vector3 normal = Vector3.Cross(pointB - pointA, pointC - pointA).normalized;

                Vector3 center = (pointA + pointB + pointC) / 3;
                Vector3 vec = localPosition - center;
                float planeD = normal.x * pointA.x - normal.y * pointA.y - normal.z * pointA.z;

                // if point is above caster's surface then ignore
                float dotVN = Vector3.Dot(vec, normal);
                if (dotVN > 0)
                    continue;

                // if caster's surface is align to light direction then ignore
                float dotNLocalLight = Vector3.Dot(localLightDirection, normal);
                if (dotNLocalLight > dotOut)
                    continue;

                float d = Vector3.Dot(normal, pointA);
                float t = (Vector3.Dot(vec, normal) + d) / dotNLocalLight;

                // if direction to closest point is opposite to received's surface normal 
                // or too close to receiver's point (consider as same point)
                // then ignore
                if (t <= closeOut)
                    continue;

                Debug.Log(dotNLocalLight);
                // progject receiver's point(pixel) to caster's surface
                Vector3 snapLocalPosition = localPosition + (localLightDirection * -(t + planeD));
                snapPos.Add(shadowCastMeshFilter.transform.TransformPoint(snapLocalPosition));

                //Gizmos.DrawRay(shadowCastMeshFilter.transform.position, normal);
                //Gizmos.DrawWireSphere(shadowCastMeshFilter.transform.TransformPoint(localPosition), 0.05f);

                Vector3 na = Vector3.Cross(pointA - pointC, snapLocalPosition - pointA);
                Vector3 nb = Vector3.Cross(pointB - pointA, snapLocalPosition - pointB);
                Vector3 nc = Vector3.Cross(pointC - pointB, snapLocalPosition - pointC);

                float d_ab = Vector3.Dot(na, nb);
                float d_bc = Vector3.Dot(nb, nc);

                //Debug.Log("S2: " + i + " " + d_ab + " " + d_bc);

                // if projected point is out side surface then ignore

                if (d_ab <= closeOut || d_bc <= closeOut)
                    continue;


                // find uv at projected point on surface
                float un = Vector3.Cross(pointA - pointB, pointA - pointC).magnitude;
                float ua = Vector3.Cross(pointB - snapLocalPosition, pointC - snapLocalPosition).magnitude / un;
                float ub = Vector3.Cross(pointC - snapLocalPosition, pointA - snapLocalPosition).magnitude / un;
                float uc = Vector3.Cross(pointA - snapLocalPosition, pointB - snapLocalPosition).magnitude / un;

                Vector2 uva = shadowCastMeshFilter.mesh.uv[indexA];
                Vector2 uvb = shadowCastMeshFilter.mesh.uv[indexB];
                Vector2 uvc = shadowCastMeshFilter.mesh.uv[indexC];

                Vector2 uv = uva * ua + uvb * ub + uvc * uc;
                //Debug.Log(na.magnitude + " " + ub + " " + uc + " " + un + " = " + uv);

               shadowVertices.Add(worldPosition);

                break;
            }
        }

        Debug.Log("S_LP" + lPoints);
    }

    public void OnValidate() {
        //shadowCastMeshFilter.mesh.SetVertices(vertices);

        //shadowCastMeshFilter.mesh.RecalculateNormals();
        //shadowCastMeshFilter.mesh.RecalculateTangents();

    }
}
