using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GBufferDebugCPU : MonoBehaviour {
    public MeshFilter shadowCastMeshFilter;

    public GameObject shadowReceivePoint;

    public Vector3 lightDirection;

    public Vector3 localPoint;

    public float closeOut = 0.0000001f;
    public float dotOut = -0.001f;

    public bool calculate;

    public bool hideSnapSphere;

    [Range(0.0f, 1.0f)]
    public float pointSize = 0.01f; 

    [SerializeField]
    private Vector3[] vertices;

    private List<Vector3> shadowVertices = new List<Vector3>();
    private List<Vector3> snapPos = new List<Vector3>();

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
            StartCoroutine(CalculateShadow());
            calculate = false;
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

        Vector3 localLightDirection = shadowCastMeshFilter.transform.InverseTransformPoint(lightDirection);
        localLightDirection.Normalize();

        foreach (Vector3 point in vertices) {
            Vector3 worldPosition = shadowReceivePoint.transform.TransformPoint(point);
            Vector3 localPosition = shadowCastMeshFilter.transform.InverseTransformPoint(worldPosition);
            //localPosition = localPoint;

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

                Vector3 normal = shadowCastMeshFilter.mesh.normals[indexA];

                Vector3 center = (pointA + pointB + pointC) / 3;
                Vector3 vec = localPosition - center;
                
                // if point is above caster's surface then ignore
                float dotVN = Vector3.Dot(vec, normal);
                if (dotVN > 0)
                    continue;

                // if caster's surface is align to light direction then ignore
                float dotNLocalLight = Vector3.Dot(localLightDirection, normal);
                if (dotNLocalLight < dotOut)
                    continue;


                //if (Vector3.Dot(vec, normal) < 0.000f)
                //    continue;

                float d = Vector3.Dot(normal, pointA);

                //float distance = Vector3.Dot(vec, normal);
                //Vector3 surfacePoint = localPosition - (normal * distance);
                //Vector3 l = (surfacePoint - localPosition);

                //Vector3 snapLocalPosition = localPosition + (localLightDirection * Vector3.Dot(l, localLightDirection));

                float t = -(Vector3.Dot(localPosition, normal) - d) / dotNLocalLight;

                // if direction to closest point is opposite to received's surface normal 
                // or too close to receiver's point (consider as same point)
                // then ignore
                if (t <= closeOut)
                    continue;

                // progject receiver's point(pixel) to caster's surface
                Vector3 snapLocalPosition = localPosition + (localLightDirection * t);
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

                Vector3 shadowPosition = shadowCastMeshFilter.transform.TransformPoint(snapLocalPosition);
                shadowVertices.Add(worldPosition);

                break;
            }
        }
    }
    public void OnValidate() {
        //shadowCastMeshFilter.mesh.SetVertices(vertices);

        //shadowCastMeshFilter.mesh.RecalculateNormals();
        //shadowCastMeshFilter.mesh.RecalculateTangents();

    }
}
