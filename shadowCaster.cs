//CREDIT: Zaid Abdalkarim (AKA Lept)
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class shadowCaster : MonoBehaviour
{

    //PolygonCollider2D poly;

    public LayerMask isWall;
    public GameObject lightCaster;
    public Collider2D[] hitObjects;

    public List<Vector2> tempPath;
    public int[] triangles;
    public int shadowCastRadius;
    Vector3 lastPos = new Vector3(-123, -123, -123);
    private void LateUpdate()
    {
        //grabs any object in a radius and adds it to the array
        hitObjects = Physics2D.OverlapCircleAll(lightCaster.transform.position, shadowCastRadius, isWall);
        lastPos = lightCaster.transform.position;
        tempPath = new List<Vector2>();

        // this is the main loop this throws rays at all the objects shadow object points and then offsets it by a tiny margin
        // so everything is accounted for.
        for (int i = 0; i < hitObjects.Length; i++)
        {
            if (hitObjects[i].gameObject.GetComponent<ShadowObject>() != null)
            {
                ShadowObject tmp = hitObjects[i].gameObject.GetComponent<ShadowObject>();
                for (int j = 0; j < tmp.objectVerts.Count; j++)
                {

                    Vector2 angleVector = (tmp.objectVerts[j] - new Vector2(lightCaster.transform.position.x, lightCaster.transform.position.y));

                    RaycastHit2D ray2 = Physics2D.Raycast(lightCaster.transform.position, angleVector, shadowCastRadius, isWall); //at the exact pos of the verticy of the object
                    if (ray2)
                        tempPath.Add(ray2.point);
                    Debug.DrawLine(lightCaster.transform.position, ray2.point);

                    RaycastHit2D ray = Physics2D.Raycast(lightCaster.transform.position, new Vector2(angleVector.x - 0.1f, angleVector.y - 0.1f), shadowCastRadius, isWall); //at the exact pos of the verticy of the object
                    if (ray)
                        tempPath.Add(ray.point);
                    Debug.DrawLine(lightCaster.transform.position, ray.point);

                    RaycastHit2D ray3 = Physics2D.Raycast(lightCaster.transform.position, new Vector2(angleVector.x + 0.1f, angleVector.y + 0.1f), shadowCastRadius, isWall); //at the exact pos of the verticy of the object
                    if (ray3)
                        tempPath.Add(ray3.point);
                    Debug.DrawLine(lightCaster.transform.position, ray3.point);
                }
            }
        }

        tempPath.Sort(new ClockwiseComparer(lightCaster.transform.position));
        Debug.Log(tempPath.Count);
        //poly.SetPath(0, tempPath); // this is dumb IDK Y. IT HAS ALL THE RIGHT POINTS BUT IT ADDS A BUNCH OF ZEROOSSSS HELOOOO


        int pointCount = 0;
        pointCount = tempPath.Count;
        MeshFilter mf = GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        Vector2[] points = tempPath.ToArray();
        Vector3[] vertices = new Vector3[pointCount];

        Vector2[] uv = new Vector2[pointCount];
        for (int j = 0; j < pointCount; j++)
        {
            Vector2 actual = points[j];
            vertices[j] = new Vector3(actual.x, actual.y, 0);
            uv[j] = actual;
        }
        Triangulator tr = new Triangulator(points);
        triangles = tr.Triangulate();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mf.mesh = mesh;
    }

    public class Triangulator
    {
        private List<Vector2> m_points = new List<Vector2>();

        public Triangulator(Vector2[] points)
        {
            m_points = new List<Vector2>(points);
        }

        public int[] Triangulate()
        {
            List<int> indices = new List<int>();

            int n = m_points.Count;
            if (n < 3)
                return indices.ToArray();

            int[] V = new int[n];
            if (Area() > 0)
            {
                for (int v = 0; v < n; v++)
                    V[v] = v;
            }
            else
            {
                for (int v = 0; v < n; v++)
                    V[v] = (n - 1) - v;
            }

            int nv = n;
            int count = 2 * nv;
            for (int v = nv - 1; nv > 2;)
            {
                if ((count--) <= 0)
                    return indices.ToArray();

                int u = v;
                if (nv <= u)
                    u = 0;
                v = u + 1;
                if (nv <= v)
                    v = 0;
                int w = v + 1;
                if (nv <= w)
                    w = 0;

                if (Snip(u, v, w, nv, V))
                {
                    int a, b, c, s, t;
                    a = V[u];
                    b = V[v];
                    c = V[w];
                    indices.Add(a);
                    indices.Add(b);
                    indices.Add(c);
                    for (s = v, t = v + 1; t < nv; s++, t++)
                        V[s] = V[t];
                    nv--;
                    count = 2 * nv;
                }
            }

            indices.Reverse();
            return indices.ToArray();
        }

        private float Area()
        {
            int n = m_points.Count;
            float A = 0.0f;
            for (int p = n - 1, q = 0; q < n; p = q++)
            {
                Vector2 pval = m_points[p];
                Vector2 qval = m_points[q];
                A += pval.x * qval.y - qval.x * pval.y;
            }
            return (A * 0.5f);
        }

        private bool Snip(int u, int v, int w, int n, int[] V)
        {
            int p;
            Vector2 A = m_points[V[u]];
            Vector2 B = m_points[V[v]];
            Vector2 C = m_points[V[w]];
            if (Mathf.Epsilon > (((B.x - A.x) * (C.y - A.y)) - ((B.y - A.y) * (C.x - A.x))))
                return false;
            for (p = 0; p < n; p++)
            {
                if ((p == u) || (p == v) || (p == w))
                    continue;
                Vector2 P = m_points[V[p]];
                if (InsideTriangle(A, B, C, P))
                    return false;
            }
            return true;
        }

        private bool InsideTriangle(Vector2 A, Vector2 B, Vector2 C, Vector2 P)
        {
            float ax, ay, bx, by, cx, cy, apx, apy, bpx, bpy, cpx, cpy;
            float cCROSSap, bCROSScp, aCROSSbp;

            ax = C.x - B.x; ay = C.y - B.y;
            bx = A.x - C.x; by = A.y - C.y;
            cx = B.x - A.x; cy = B.y - A.y;
            apx = P.x - A.x; apy = P.y - A.y;
            bpx = P.x - B.x; bpy = P.y - B.y;
            cpx = P.x - C.x; cpy = P.y - C.y;

            aCROSSbp = ax * bpy - ay * bpx;
            cCROSSap = cx * apy - cy * apx;
            bCROSScp = bx * cpy - by * cpx;

            return ((aCROSSbp >= 0.0f) && (bCROSScp >= 0.0f) && (cCROSSap >= 0.0f));
        }
    }
    public class ClockwiseComparer : IComparer<Vector2>
    {
        private Vector2 m_Origin;

        #region Properties

        /// <summary>
        ///     Gets or sets the origin.
        /// </summary>
        /// <value>The origin.</value>
        public Vector2 origin { get { return m_Origin; } set { m_Origin = value; } }

        #endregion

        /// <summary>
        ///     Initializes a new instance of the ClockwiseComparer class.
        /// </summary>
        /// <param name="origin">Origin.</param>
        public ClockwiseComparer(Vector2 origin)
        {
            m_Origin = origin;
        }

        #region IComparer Methods

        /// <summary>
        ///     Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
        /// </summary>
        /// <param name="first">First.</param>
        /// <param name="second">Second.</param>
        public int Compare(Vector2 first, Vector2 second)
        {
            return IsClockwise(first, second, m_Origin);
        }

        #endregion

        /// <summary>
        ///     Returns 1 if first comes before second in clockwise order.
        ///     Returns -1 if second comes before first.
        ///     Returns 0 if the points are identical.
        /// </summary>
        /// <param name="first">First.</param>
        /// <param name="second">Second.</param>
        /// <param name="origin">Origin.</param>
        public static int IsClockwise(Vector2 first, Vector2 second, Vector2 origin)
        {
            if (first == second)
                return 0;

            Vector2 firstOffset = first - origin;
            Vector2 secondOffset = second - origin;

            float angle1 = Mathf.Atan2(firstOffset.x, firstOffset.y);
            float angle2 = Mathf.Atan2(secondOffset.x, secondOffset.y);

            if (angle1 < angle2)
                return -1;

            if (angle1 > angle2)
                return 1;

            // Check to see which point is closest
            return (firstOffset.sqrMagnitude < secondOffset.sqrMagnitude) ? -1 : 1;
        }
    }
}