using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class Floor : MonoBehaviour
{
    public List<Vector3> globalCornerCoord = new List<Vector3>();
    //private Vector3[] m_GlobalVertices;
    private List<Vector3> m_GlobalVertices = new List<Vector3>();
    public float m_MaxDist = 1f;
    private int[] m_PossibleWallLocations = new int[] {0, 11, 22, 33, 66, 77, 88, 99, 110};
    private int m_IndexWallEnd = 10;

    public List<Tuple<Vector3, Vector3>> CreatedWallsCoord = new List<Tuple<Vector3, Vector3>>();

    public InnerWallCreator innerWallCreator;

    public bool finished = false;
    // Start is called before the first frame update
    void Awake()
    {
        // Get the MeshFilter component of the floor object
        MeshFilter floorMeshFilter = GetComponent<MeshFilter>();

        // Ensure the MeshFilter and its shared mesh exist
        if (floorMeshFilter != null && floorMeshFilter.sharedMesh != null)
        {
            // Get the shared mesh of the floor
            Mesh floorMesh = floorMeshFilter.sharedMesh;
            
            // Get the corner coordinates in world space
            // Retrieve the vertices of the floor mesh and transform them to world space
            //m_GlobalVertices = floorMesh.vertices;

            foreach (var vert in floorMesh.vertices)
            {
                m_GlobalVertices.Add(transform.TransformPoint(vert));
            }

            /*globalCornerCoord.Add(m_FloorMatrix.MultiplyPoint3x4(m_GlobalVertices[0]));
            globalCornerCoord.Add(m_FloorMatrix.MultiplyPoint3x4(m_GlobalVertices[10]));
            globalCornerCoord.Add(m_FloorMatrix.MultiplyPoint3x4(m_GlobalVertices[110]));
            globalCornerCoord.Add(m_FloorMatrix.MultiplyPoint3x4(m_GlobalVertices[120]));*/
            
            globalCornerCoord.Add(m_GlobalVertices[0]);
            globalCornerCoord.Add(m_GlobalVertices[10]);
            globalCornerCoord.Add(m_GlobalVertices[110]);
            globalCornerCoord.Add(m_GlobalVertices[120]);

            finished = true;
            
            CalculateMaxDist();
        }
    }
    
    /// <summary>
    /// Calculate the maximum possible distance for given floor. Is based on the distances between the corners.
    /// </summary>
    private void CalculateMaxDist()
    {
        m_MaxDist = Mathf.NegativeInfinity;
        foreach (var baseCorner in globalCornerCoord)
        {
            foreach (var corner in globalCornerCoord)
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

    /*void OnDrawGizmos()
    {
        if (m_GlobalVertices != null)
        {
            foreach (var corner in m_GlobalVertices)
            {
                Gizmos.DrawSphere(m_FloorMatrix.MultiplyPoint3x4(corner), 0.1f);
            }
        }
    }*/

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
        var coordStartWall = m_GlobalVertices[begin];
        var coordDoor = m_GlobalVertices[begin + randomDoorIncrement];
        var coordEndWall = m_GlobalVertices[begin + m_IndexWallEnd];
        
        CreatedWallsCoord.Add(new Tuple<Vector3, Vector3>(coordStartWall,  coordEndWall));
        innerWallCreator.CreateWallWithDoor(coordDoor, coordStartWall, coordEndWall);
    }
}
