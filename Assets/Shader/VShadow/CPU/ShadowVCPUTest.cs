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

            Vector3 normalRaw = Vector3.Cross(BAVec, CAVec);

            float normalLength = Mathf.Sqrt(Vector3.Dot(normalRaw, normalRaw));
            Vector3 normal = normalRaw / normalLength;

            float planeD = normal.x * pointA.x - normal.y * pointA.y - normal.z * pointA.z;

            // if caster's surface is align to light direction then ignore
            float dotNLocalLight = Vector3.Dot(directionalLight.transform.forward, normal);
            if (dotNLocalLight > 0)
                continue;

            Vector3 vec = shadowReceivePoint.transform.position - pointA;
            float dotVN = Vector3.Dot(vec, normal);
            float t = (dotVN) / -dotNLocalLight;

            // if direction to closest point is opposite to received's surface normal 
            // or too close to receiver's point (consider as same point)
            // then ignore
            if (t > 0)
                continue;

            // progject receiver's point(pixel) to caster's surface
            Vector3 snapLocalPosition = shadowReceivePoint.transform.position + (directionalLight.transform.forward * (t));

            // find uv at projected point on surface
            float uabcArea = 0;
            float uaArea = Vector3.Cross(pointB - snapLocalPosition, pointC - snapLocalPosition).magnitude;
            uabcArea += uaArea;

            if (uabcArea > normalLength)
                continue;

            float ubArea = Vector3.Cross(pointC - snapLocalPosition, pointA - snapLocalPosition).magnitude;
            uabcArea += ubArea;

            if (uabcArea > normalLength)
                continue;

            float ucArea = Vector3.Cross(pointA - snapLocalPosition, pointB - snapLocalPosition).magnitude;
            uabcArea += ucArea;

            if (uabcArea > normalLength)
                continue;

            Vector2 uva = shadowCastMeshFilter.mesh.uv[indexA];
            Vector2 uvb = shadowCastMeshFilter.mesh.uv[indexB];
            Vector2 uvc = shadowCastMeshFilter.mesh.uv[indexC];

            float ua = uaArea / normalLength;
            float ub = ubArea / normalLength;
            float uc = ucArea / normalLength;

            Vector2 uv = uva * ua + uvb * ub + uvc * uc;
            Debug.Log(uv);

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(snapLocalPosition, 0.05f);
        }
    }
}
