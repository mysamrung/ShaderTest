using UnityEngine;

public class TriangleDebugGPU : MonoBehaviour
{
    public Vector3[] points = new Vector3[3];
    public float centerToPointLength;

    public void OnDrawGizmos() {
        Gizmos.DrawSphere(points[0], 0.1f);
        Gizmos.DrawSphere(points[1], 0.1f);
        Gizmos.DrawSphere(points[2], 0.1f);

        Gizmos.DrawLine(points[0], points[1]);
        Gizmos.DrawLine(points[1], points[2]);
        Gizmos.DrawLine(points[2], points[0]);

        Vector3 center = (points[0] + points[1] + points[2]) / 3;

        Gizmos.DrawLine(points[0], center);
        Gizmos.DrawLine(points[1], center);
        Gizmos.DrawLine(points[2], center);

        Gizmos.color = Color.red;

        Gizmos.DrawLine((points[0] + points[1]) / 2, center);
        Gizmos.DrawLine((points[1] + points[2]) / 2, center);
        Gizmos.DrawLine((points[2] + points[0]) / 2, center);


        Gizmos.color = Color.white;
        Gizmos.DrawSphere(center, 0.075f);

        centerToPointLength = (points[0] - center).sqrMagnitude;

        float l = (points[0] - transform.position).sqrMagnitude;


        // find uv at projected point on surface
        float un = Vector3.Cross(points[0] - points[1], points[0]  - points[2]).magnitude;
        float ua = Vector3.Cross(points[1] - transform.position, points[2] - transform.position).magnitude / un;
        float ub = Vector3.Cross(points[2] - transform.position, points[0] - transform.position).magnitude / un;
        float uc = Vector3.Cross(points[0] - transform.position, points[1] - transform.position).magnitude / un;

        Gizmos.color = Color.red;
        Gizmos.DrawSphere((points[0] * ua + points[1] * ub + points[2] * uc), 0.05f);
        Gizmos.color = Color.green;
        Gizmos.DrawSphere((points[0] * ua), 0.05f);
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere((points[1] * ub), 0.05f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere((points[2] * uc), 0.05f);
        Gizmos.color = Color.white;

        Debug.Log("C : " + centerToPointLength + " L :" + l);
    }
}
