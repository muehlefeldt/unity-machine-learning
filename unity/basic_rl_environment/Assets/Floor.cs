using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class Floor : MonoBehaviour
{
    private List<Vector3> m_CornerCoord = new List<Vector3>();
    private Vector3[] m_Vertices;
    public float m_MaxDist = 1f;
    private int[] m_PossibleWallLocations = new int[] {0, 11, 22, 33, 66, 77, 88, 99, 110};
    private int m_IndexWallEnd = 10;
    
    public InnerWallCreator innerWallCreator;

    // Start is called before the first frame update
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

            m_CornerCoord.Add(floorMatrix.MultiplyPoint3x4(m_Vertices[0]));
            m_CornerCoord.Add(floorMatrix.MultiplyPoint3x4(m_Vertices[10]));
            m_CornerCoord.Add(floorMatrix.MultiplyPoint3x4(m_Vertices[110]));
            m_CornerCoord.Add(floorMatrix.MultiplyPoint3x4(m_Vertices[120]));
            
            CalculateMaxDist();
        }
    }
    
    /// <summary>
    /// Calculate the maximum possible distance for given floor. Is based on the distances between the corners.
    /// </summary>
    private void CalculateMaxDist()
    {
        m_MaxDist = Mathf.NegativeInfinity;
        foreach (var baseCorner in m_CornerCoord)
        {
            foreach (var corner in m_CornerCoord)
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
        foreach (var corner in m_CornerCoord)
        {
            Gizmos.DrawSphere(corner, 0.1f);
        }
    }

    public float GetMaxPossibleDist()
    {
        return m_MaxDist;
    }

    public void Prepare()
    {
        innerWallCreator.DestroyAll();
    }

    /// <summary>
    /// Create a inner wall.
    /// Reference: https://answers.unity.com/questions/52747/how-i-can-create-a-cube-with-specific-coordenates.html
    /// </summary>
    public void CreateInnerWall()
    {
        var randomWallIndex = Random.Range(3, 6);
        var begin = m_PossibleWallLocations[randomWallIndex];
        var randomDoorIncrement = Random.Range(2, 8);
        //var vertices = GetComponent<MeshFilter>().sharedMesh.vertices;
        var coordStartWall = m_Vertices[begin];
        var coordDoor = m_Vertices[begin + randomDoorIncrement];
        var coordEndWall = m_Vertices[begin + m_IndexWallEnd];

        innerWallCreator.CreateWallWithDoor(coordDoor, coordStartWall, coordEndWall);
    }
}
