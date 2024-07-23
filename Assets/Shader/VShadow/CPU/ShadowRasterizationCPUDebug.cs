using UnityEngine;

public class ShadowRasterizationCPUDebug : MonoBehaviour {

    [SerializeField]
    private MeshFilter shadowCasterMeshFilter;

    [SerializeField]
    private Light mainDirectionLight;

    [SerializeField]
    private float distance;

    private Texture2D textureRect;

    private void Start () {
        textureRect = new Texture2D(1, 1);
        textureRect.SetPixel(0, 0, new Color(0, 0, 0, 0.5f));
        textureRect.Apply();
    }

    private Vector4 MatrixMulVector4(Matrix4x4 matrix, Vector4 vector) {
        float x = matrix.m00 * vector.x + matrix.m01 * vector.y + matrix.m02 * vector.z + matrix.m03 * vector.w;
        float y = matrix.m10 * vector.x + matrix.m11 * vector.y + matrix.m12 * vector.z + matrix.m13 * vector.w;
        float z = matrix.m20 * vector.x + matrix.m21 * vector.y + matrix.m22 * vector.z + matrix.m23 * vector.w;
        float w = matrix.m30 * vector.x + matrix.m31 * vector.y + matrix.m32 * vector.z + matrix.m33 * vector.w;

        return new Vector4(x, y, z, w);
    }

    private Vector2 LinesIntersect(Vector3 line1, Vector3 line2) {
        float x = ((line1.z * line2.y) - (line1.y * line2.z)) / ((line1.x * line2.y) - (line1.y * line2.x));
        float y = ((line1.x * line2.z) - (line1.z * line2.x)) / ((line1.x * line2.y) - (line1.y * line2.x));

        return new Vector2(x, y);
    }

    public void OnGUI() {
        if (shadowCasterMeshFilter == null || mainDirectionLight == null)
            return;

        Mesh shadowCasterMesh = shadowCasterMeshFilter.mesh;
        Matrix4x4 vp = Camera.main.projectionMatrix * Camera.main.worldToCameraMatrix;
        Matrix4x4 mvp = vp * shadowCasterMeshFilter.transform.localToWorldMatrix;

        Vector2 lightDirectionSS = Camera.main.projectionMatrix * mainDirectionLight.transform.forward;
        lightDirectionSS.y = -lightDirectionSS.y;
        lightDirectionSS.Normalize();
        lightDirectionSS *= distance;

        float lightDot = Vector3.Dot(lightDirectionSS, new Vector2(0.5f, 0.5f));

        Debug.Log(lightDirectionSS);

        Vector3 borderXMin = new Vector3(1, 0, 0);
        Vector3 borderXMax = new Vector3(1, 0, -1);
        Vector3 borderYMin = new Vector3(0, 1, -0);
        Vector3 borderYMax = new Vector3(0, 1, -1);

        for (int i = 0; i < shadowCasterMesh.triangles.Length; i += 3) {
            int indexA = shadowCasterMesh.triangles[i + 0];
            int indexB = shadowCasterMesh.triangles[i + 1];
            int indexC = shadowCasterMesh.triangles[i + 2];

            Vector4 pointA = shadowCasterMesh.vertices[indexA];
            pointA.w = 1;

            Vector4 pointB = shadowCasterMesh.vertices[indexB];
            pointB.w = 1;

            Vector4 pointC = shadowCasterMesh.vertices[indexC];
            pointC.w = 1;

            Vector4 pointASS = MatrixMulVector4(mvp, pointA);
            pointASS.x = (pointASS.x / pointASS.w * 0.5f) + 0.5f;
            pointASS.y = 1 - ((pointASS.y / pointASS.w * 0.5f) + 0.5f);

            Vector4 pointBSS = MatrixMulVector4(mvp, pointB);
            pointBSS.x = (pointBSS.x / pointBSS.w * 0.5f) + 0.5f;
            pointBSS.y = 1 - ((pointBSS.y / pointBSS.w * 0.5f) + 0.5f);

            Vector4 pointCSS = MatrixMulVector4(mvp, pointC);
            pointCSS.x = (pointCSS.x / pointCSS.w * 0.5f) + 0.5f;
            pointCSS.y = 1 - ((pointCSS.y / pointCSS.w * 0.5f) + 0.5f);

            //Debug.Log(pointASS + " " + pointBSS + " " + pointCSS);



            float minPointX = Mathf.Min(pointASS.x, pointBSS.x, pointCSS.x);
            minPointX = Mathf.Min(minPointX + lightDirectionSS.x, minPointX);

            float maxPointX = Mathf.Max(pointASS.x, pointBSS.x, pointCSS.x);
            maxPointX = Mathf.Max(maxPointX + lightDirectionSS.x, maxPointX);

            float minPointY = Mathf.Min(pointASS.y, pointBSS.y, pointCSS.y);
            minPointY = Mathf.Min(minPointY + lightDirectionSS.y, minPointY);

            float maxPointY = Mathf.Max(pointASS.y, pointBSS.y, pointCSS.y);
            maxPointY = Mathf.Max(maxPointY + lightDirectionSS.y, maxPointY);

            float slope = (maxPointY - minPointY) / (maxPointX - minPointX);
            Vector3 line = new Vector3(slope, -1, -(slope * maxPointX - maxPointY));


            //Vector2 minPoint = new Vector2(minPointX, minPointY);
            //float tmin = Vector2.Dot(new Vector2(0.0f, 0.0f) - minPoint, lightDirectionSS);
            //if (tmin > 0)
            //    minPoint += lightDirectionSS * tmin;


            //Vector2 maxPoint = new Vector2(maxPointX, maxPointY);
            //float tmax = Vector2.Dot(new Vector2(1.0f, 1.0f) - maxPoint, lightDirectionSS);

            //if (minPointX < 0 || minPointY < 0) {
            //    float portionX = lightDirectionSS.x == 0 ? 0 : minPointX / lightDirectionSS.x;
            //    float portionY = lightDirectionSS.y == 0 ? 0 : minPointY / lightDirectionSS.y;
            //    float t = -Mathf.Min(portionX, portionY);
            //    minPointX += lightDirectionSS.x * t;
            //    minPointY += lightDirectionSS.y * t;
            //}

            //if (maxPointX > 1 || maxPointY > 1) {
            //    float t = -Mathf.Max(0, Mathf.Max((maxPointX - 1) / direction.x, (maxPointY - 1) / direction.y));
            //    maxPointX += direction.x * t;
            //    maxPointY += direction.y * t;
            //}

            Vector2 intersectBorderYMin = LinesIntersect(line, borderYMin);
            GUI.DrawTexture(new Rect(-intersectBorderYMin.x * Screen.width - 50, intersectBorderYMin.y * Screen.height - 50, 100, 100), textureRect);
            Vector2 intersectBorderYMax = LinesIntersect(line, borderYMax);
            GUI.DrawTexture(new Rect(-intersectBorderYMax.x * Screen.width - 50, -intersectBorderYMax.y * Screen.height - 50, 100, 100), textureRect);
            Vector2 intersectBorderXMin = LinesIntersect(line, borderXMin);
            GUI.DrawTexture(new Rect(-intersectBorderXMin.x * Screen.width - 50, -intersectBorderXMin.y * Screen.height - 50, 100, 100), textureRect);
            Vector2 intersectBorderXMax = LinesIntersect(line, borderXMax);
            GUI.DrawTexture(new Rect(-intersectBorderXMax.x * Screen.width - 50, -intersectBorderXMax.y * Screen.height - 50, 100, 100), textureRect);
          
            Debug.Log(minPointX + " " + minPointY + " " + maxPointX + " " + maxPointY);

            minPointX = minPointX * Screen.width;
            maxPointX = maxPointX * Screen.width;
            minPointY = minPointY * Screen.height;
            maxPointY = maxPointY * Screen.height;

            Rect rect = new Rect(minPointX, minPointY, maxPointX - minPointX, maxPointY - minPointY);
            GUI.DrawTexture(rect, textureRect);
        }
    }
}
