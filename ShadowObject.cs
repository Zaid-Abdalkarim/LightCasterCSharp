//MUST GIVE CREDIT: Zaid Abdalkarim

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShadowObject : MonoBehaviour
{
    public List<Vector2> objectVerts = new List<Vector2>(); // these are the corners of every gameobject, this can be calcuated but this will take more time
    public PolygonCollider2D poly;

    void Start()
    {
        for (int i = 0; i < poly.points.Length; i++)
        {
            float x = poly.points[i].x * transform.localScale.x;
            float y = poly.points[i].y *transform.localScale.y;
            objectVerts.Add(new Vector2(transform.position.x + x, transform.position.y + y));
        }

    }
}
