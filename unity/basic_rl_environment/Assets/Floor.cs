using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class Floor : MonoBehaviour
{
    public List<Vector3> m_CornerCoordinates;
    public float m_MaxDist = 1f;
    private Vector3[] m_Vertices;

    public InnerWallCreator innerWallCreator;

    // Start is called before the first frame update
    /*void Start()
    {
        m_Corners = new List<Vector3>();
        //VerticeList = new List(GetComponent().sharedMesh.vertices);
        m_Vertices = GetComponent<MeshFilter>().sharedMesh.vertices;
        print(m_Vertices.Length);
        m_Corners.Add(m_Vertices[0]);
        m_Corners.Add(m_Vertices[10]);
        m_Corners.Add(m_Vertices[110]);
        m_Corners.Add(m_Vertices[120]);

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
    }*/
    private void Start()
    {
        // Get the MeshFilter component of the floor object
        MeshFilter floorMeshFilter = GetComponent<MeshFilter>();

        // Ensure the MeshFilter and its shared mesh exist
        if (floorMeshFilter != null && floorMeshFilter.sharedMesh != null)
        {
            // Get the shared mesh of the floor
            Mesh floorMesh = floorMeshFilter.sharedMesh;

            // Get the local-to-world transformation matrix of the floor object
            Matrix4x4 floorMatrix = transform.localToWorldMatrix;

            // Get the corner coordinates in world space
            // Retrieve the vertices of the floor mesh and transform them to world space
            m_Vertices = floorMesh.vertices;

            m_CornerCoordinates.Add(floorMatrix.MultiplyPoint3x4(m_Vertices[0]));
            m_CornerCoordinates.Add(floorMatrix.MultiplyPoint3x4(m_Vertices[10]));
            m_CornerCoordinates.Add(floorMatrix.MultiplyPoint3x4(m_Vertices[110]));
            m_CornerCoordinates.Add(floorMatrix.MultiplyPoint3x4(m_Vertices[120]));
            //m_CornerCoordinates.Add(floorMatrix.MultiplyPoint3x4(vertices[55]));
            //m_CornerCoordinates.Add(floorMatrix.MultiplyPoint3x4(vertices[65]));

            CalculateMaxDist();
        }
    }

    private void CalculateMaxDist()
    {
        m_MaxDist = Mathf.NegativeInfinity;
        foreach (var baseCorner in m_CornerCoordinates)
        {
            foreach (var corner in m_CornerCoordinates)
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

    void OnDrawGizmos()
    {
        foreach (var corner in m_CornerCoordinates)
        {
            Gizmos.DrawSphere(corner, 0.1f);
        }
    }

    public float GetMaxPossibleDist()
    {
        return m_MaxDist;
    }

    /// <summary>
    /// Create a inner wall.
    /// Reference: https://answers.unity.com/questions/52747/how-i-can-create-a-cube-with-specific-coordenates.html
    /// </summary>
    public void CreateInnerWall()
    {
        //var vertices = GetComponent<MeshFilter>().sharedMesh.vertices;
        var coordStartWall = m_Vertices[55];
        var coordDoor = m_Vertices[60];
        var coordEndWall = m_Vertices[65];

        innerWallCreator.CreateWallWithDoor(coordDoor, coordStartWall, coordEndWall);
    }
}
