using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Not used right now, might be involved in the future for raycasting the field of view for guards
public class FieldOfView : MonoBehaviour
{

    [SerializeField]
    private LayerMask lm; //which layers can we hit with the fov
    Vector3 origin;
    private Mesh mesh;
    private float startingAngle;
    private float fov;
    private int viewDistance;
    private int rayCount;
    private float angle;
    private float angleIncrease;

    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        origin = Vector3.zero;
        fov = 60f;
        viewDistance = 5;
        rayCount = 100;
        angle = 0f;
        angleIncrease = fov / rayCount;
}

    void LateUpdate()
    {

        Vector3[] vertices = new Vector3[rayCount + 1 + 1]; //one vertex in origin, one for the zero ray and one per other ray
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[rayCount * 3];

        vertices[0] = origin;

        int vertexIndex = 1;
        int triangleIndex = 0;
        for (int i = 0; i <= rayCount; ++i)
        {
            Vector3 vertex;
            RaycastHit2D hit = Physics2D.Raycast(origin, GetVectorFromAngle(angle), viewDistance, lm);
            if(hit.collider == null)
            {
                vertex = origin + GetVectorFromAngle(angle) * viewDistance;
            } 
            else
            {
                vertex = hit.point;
                vertex.z = 1;
            }
            vertices[vertexIndex] = vertex;

            if (i > 0)
            {
                triangles[triangleIndex + 0] = 0;
                triangles[triangleIndex + 1] = vertexIndex - 1;
                triangles[triangleIndex + 2] = vertexIndex;
                triangleIndex += 3;
            }

            ++vertexIndex;
            angle -= angleIncrease; //subtract because in unity when we increase we go counter clockwise and we want to go clockwise
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
    }

    public void SetAimDirection(Vector3 direction)
    {
        startingAngle = GetAngleFromVector(direction) - fov/2f;
    }

    public void SetOrigin(Vector3 origin)
    {
        this.origin = origin;
    }

    private static float GetAngleFromVector(Vector3 direction)
    {
        direction = direction.normalized;
        float n = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (n < 0) n += 360;
        return n;
    }

    //returns a vector pointing in the angle (0-306 degs)
    private static Vector3 GetVectorFromAngle(float angle)
    {
        float angleRad = angle * (Mathf.PI / 180f);
        return new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
    }



}
