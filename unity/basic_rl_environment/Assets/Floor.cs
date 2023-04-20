using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Floor : MonoBehaviour
{
    public List<Vector3> m_Corners;
    public float m_MaxDist;
    
    // Start is called before the first frame update
    void Start()
    {
        m_Corners = new List<Vector3>();
        //VerticeList = new List(GetComponent().sharedMesh.vertices);
        var vertices = GetComponent<MeshFilter>().sharedMesh.vertices;
        m_Corners.Add(vertices[0]);
        m_Corners.Add(vertices[10]);
        m_Corners.Add(vertices[110]);
        m_Corners.Add(vertices[120]);

        m_MaxDist = Mathf.NegativeInfinity;
        foreach (var baseCorner in m_Corners)
        {
            foreach (var corner in m_Corners)
            {
                if (baseCorner == corner)
                {
                    continue;
                }

                var dist = Vector3.Distance(baseCorner, corner);
                if (dist > m_MaxDist)
                {
                    m_MaxDist = dist;
                }
            }
        }
    }

    // void OnDrawGizmos()
    // {
    //     foreach (var corner in m_Corners)
    //     {
    //         Gizmos.DrawSphere(corner, 0.1f);
    //     }
    // }
    
    public float GetMaxPossibleDist()
    {
        return m_MaxDist;
    }
}
