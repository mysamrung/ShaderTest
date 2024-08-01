using UnityEngine;

public class ShadowVCPUTest : MonoBehaviour
{
    public GameObject shadowReceivePoint;
    public MeshFilter shadowCastMeshFilter;

    public Light directionalLight;

    public void OnDrawGizmos()
    {
        DebugVShadow();
    }

    public void DebugVShadow()
    {
        MeshRenderer meshRenderer = shadowCastMeshFilter.GetComponent<MeshRenderer>();

        
        Gizmos.color = Color.white;
        Gizmos.DrawSphere(shadowReceivePoint.transform.position, 0.025f);

        for (int i = 0; i < shadowCastMeshFilter.mesh.triangles.Length; i += 3)
        {    
            int indexA = shadowCastMeshFilter.mesh.triangles[i + 0];
            int indexB = shadowCastMeshFilter.mesh.triangles[i + 1];
            int indexC = shadowCastMeshFilter.mesh.triangles[i + 2];

            Vector3 pointA = shadowCastMeshFilter.mesh.vertices[indexA];
            Vector3 pointB = shadowCastMeshFilter.mesh.vertices[indexB];
            Vector3 pointC = shadowCastMeshFilter.mesh.vertices[indexC];

            pointA = shadowCastMeshFilter.transform.TransformPoint(pointA);
            pointB = shadowCastMeshFilter.transform.TransformPoint(pointB);
            pointC = shadowCastMeshFilter.transform.TransformPoint(pointC);

            Vector3 BAVec = pointB - pointA;
            Vector3 CAVec = pointC - pointA;
            Vector3 BCVec = pointB - pointC;

            Vector3 normalRaw = directionalLight.transform.forward;
            Vector3 center = (pointA + pointB + pointC) / 3;
            Vector3 centerToPoint = shadowReceivePoint.transform.position - center;

            Vector3 normalBA = Vector3.Cross(normalRaw, BAVec).normalized;
            float dotNBA = Vector3.Dot(normalBA, centerToPoint);

            Vector3 normalCA = Vector3.Cross(normalRaw, CAVec).normalized;
            float dotNCA = Vector3.Dot(normalCA, centerToPoint);

            Vector3 normalBC = Vector3.Cross(normalRaw, BCVec).normalized;
            float dotNBC = Vector3.Dot(normalBC, centerToPoint);

            Gizmos.DrawRay((pointB + pointA) / 2, normalBA * 100);
            Gizmos.DrawRay((pointC + pointA) / 2, normalCA * 100);
            Gizmos.DrawRay((pointB + pointC) / 2, normalBC * 100);

            if (dotNCA > 0)
                continue;
            if (dotNBA > 0)
                continue;
            if (dotNBC > 0)
                continue;
            
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(shadowReceivePoint.transform.position, 0.05f);
        }
    }
}
