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
    //public List<Vector3> globalCornerCoord = new List<Vector3>();
    //private Vector3[] m_GlobalVertices;
    //private List<Vector3> m_GlobalVertices = new List<Vector3>();
    
    // Maximum possible distance on this floor.
    public float m_MaxDist = 1f;
    
    //private int[] m_PossibleWallLocations = new int[] {0, 11, 22, 33, 66, 77, 88, 99, 110};
    //private int m_IndexWallEnd = 10;

    //public List<Tuple<Vector3, Vector3>> CreatedWallsCoord = new List<Tuple<Vector3, Vector3>>();
    //public List<Vector3> CreatedDoorsCoord = new List<Vector3>();

    public InnerWallCreator innerWallCreator;

    public AllRooms RoomsInEnv;
    public RollerAgent agent;
    public Target target;

    //public bool finished = false;
    
    // Store global coords of the floor. Populated during startup.
    private List<Vector3> m_CornersGlobalCoords;
    private Vector3 m_MinXGlobalCoord;
    private Vector3 m_MaxXGlobalCoord;
    private Vector3 m_MinZGlobalCoord;
    private Vector3 m_MaxZGlobalCoord;
    
    // Store inner wall and door global coords.
    private Vector3 m_WallStartGlobalCoord;
    private Vector3 m_WallEndGlobalCoord;
    
    private Vector3 m_DoorStartGlobalCoord;
    private Vector3 m_DoorEndGlobalCoord;
    private Vector3 m_DoorCentreGlobalCoord;
    

    public float doorWidth = 5f;
    
    // Start is called before the first frame update
    void Awake()
    {
        // Get the MeshFilter component of the floor object
        MeshFilter floorMeshFilter = GetComponent<MeshFilter>();
        
        // Setup the room management.
        RoomsInEnv = new AllRooms(CreateWall, targetAlwaysInOtherRoomFromAgent);

        // Ensure the MeshFilter and its shared mesh exist
        if (floorMeshFilter != null && floorMeshFilter.sharedMesh != null)
        {
            // Get the shared mesh of the floor
            Mesh floorMesh = floorMeshFilter.sharedMesh;
            
            // Get the corner coordinates in world space
            // Retrieve the vertices of the floor mesh and transform them to world space
            //m_GlobalVertices = floorMesh.vertices;

            /*var globalVertices = new List<Vector3>();
            foreach (var vert in floorMesh.vertices)
            {
                globalVertices.Add(transform.TransformPoint(vert));
            }*/

            /*globalCornerCoord.Add(m_FloorMatrix.MultiplyPoint3x4(m_GlobalVertices[0]));
            globalCornerCoord.Add(m_FloorMatrix.MultiplyPoint3x4(m_GlobalVertices[10]));
            globalCornerCoord.Add(m_FloorMatrix.MultiplyPoint3x4(m_GlobalVertices[110]));
            globalCornerCoord.Add(m_FloorMatrix.MultiplyPoint3x4(m_GlobalVertices[120]));*/
            var corners = new List<Vector3>();
            m_CornersGlobalCoords = corners;
            
            corners.Add(transform.TransformPoint(floorMesh.vertices[0]));
            corners.Add(transform.TransformPoint(floorMesh.vertices[10]));
            corners.Add(transform.TransformPoint(floorMesh.vertices[110]));
            corners.Add(transform.TransformPoint(floorMesh.vertices[120]));
            //globalCornerCoord.Add(m_GlobalVertices[110]);
            //globalCornerCoord.Add(m_GlobalVertices[120]);

            // Store max and min values corners for later use.
            var sortedX = corners.OrderBy(v => v.x).ToList();
            var sortedZ = corners.OrderBy(v => v.z).ToList();
            m_MinXGlobalCoord = sortedX[0];
            m_MaxXGlobalCoord = sortedX[^1];
            m_MinZGlobalCoord = sortedZ[0];
            m_MaxZGlobalCoord = sortedZ[^1];
        

            //finished = true;
            
            CalculateMaxDist();
        }
    }
    
    /// <summary>
    /// Calculate the maximum possible distance for given floor. Is based on the distances between the corners.
    /// </summary>
    private void CalculateMaxDist()
    {
        m_MaxDist = Mathf.NegativeInfinity;
        foreach (var baseCorner in m_CornersGlobalCoords)
        {
            foreach (var corner in m_CornersGlobalCoords)
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
        /*if (m_GlobalVertices != null)
        {
            foreach (var corner in m_GlobalVertices)
            {
                Gizmos.DrawSphere(m_FloorMatrix.MultiplyPoint3x4(corner), 0.1f);
            }
        }*/
        // if (m_CornersGlobalCoords != null)
        // {
        //     foreach (var coord in m_CornersGlobalCoords)
        //     {
        //         Gizmos.DrawSphere(coord, 0.5f);
        //     }
        // }
        // if (CreatedWallsCoord != null)
        // {
        //     foreach (var coords in CreatedWallsCoord)
        //     {
        //         Gizmos.DrawSphere(coords.Item1, 0.5f);
        //         Gizmos.DrawSphere(coords.Item2, 0.5f);
        //     }
        // }
        foreach (var coord in new List<Vector3>(){m_WallStartGlobalCoord, m_WallEndGlobalCoord})
        {
            Gizmos.DrawSphere(coord, 0.5f);
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
    public bool CreateWall = true;
    public bool RandomWallPosition = true;
    public bool RanndomDoorPosition = true;
    public bool targetAlwaysInOtherRoomFromAgent = false;

    //private bool m_AgentInFirstRoom = false;
    
    /// <summary>
    /// Create rooms by creating a inner wall.
    /// </summary>
    public void CreateInnerWall()
    {
        // The wall inner wall is only created if selected in the Unity editor. Set to true as default.
        if (CreateWall)
        {
            // Previously created rooms can be removed.
            RoomsInEnv.Clear();
            
            // Calculate and store inner wall and door positions.
            SetWallCoords();
            SetDoorCoords();
            innerWallCreator.CreateWallWithDoor((m_WallStartGlobalCoord, m_WallEndGlobalCoord), (m_DoorStartGlobalCoord, m_DoorEndGlobalCoord));

            // Add the created rooms to the room management object.
            RoomsInEnv.AddRoom(new List<Vector3>{m_CornersGlobalCoords[0], m_CornersGlobalCoords[1], m_WallStartGlobalCoord, m_WallEndGlobalCoord});
            RoomsInEnv.AddRoom(new List<Vector3>{m_CornersGlobalCoords[2], m_CornersGlobalCoords[3], m_WallStartGlobalCoord, m_WallEndGlobalCoord});
        }
        else
        {
            // Env has only one room when no wall is created. Only one room needs to be stored.
            RoomsInEnv.Clear(); 
            RoomsInEnv.AddRoom(m_CornersGlobalCoords);
        }
    }
    
    private void SetWallCoords()
    {
        var r = 0.5f; // Default position in the middle of the floor.
        if (RandomWallPosition)
        {
            r = GetRandom(0.3f);
        }
        var startCoord = Vector3.zero;
        startCoord.x = m_MinXGlobalCoord.x; // x value can be taken from floor corner.
        startCoord.z = Vector3.Lerp(m_MinZGlobalCoord, m_MaxZGlobalCoord, r).z;
        
        var endCoord = startCoord;
        endCoord.x = m_MaxXGlobalCoord.x;

        m_WallStartGlobalCoord = startCoord;
        m_WallEndGlobalCoord = endCoord;
    }
    
    private void SetDoorCoords()
    {
        var r = 0.5f;
        if (RandomWallPosition)
        {
            r = GetRandom(0.3f);
        }
        var pos = Vector3.zero;
        pos.x = Vector3.Lerp(m_MinZGlobalCoord, m_MaxZGlobalCoord, r).x;
        pos.z = m_WallStartGlobalCoord.z;
        m_DoorCentreGlobalCoord = pos;
        
        var direction = Vector3.Normalize(m_WallEndGlobalCoord - pos);
        m_DoorStartGlobalCoord = pos - direction * doorWidth / 2f;
        m_DoorEndGlobalCoord = pos + direction * doorWidth / 2f;
    }
    
    /// <summary>
    /// Get random value between 0f and 1f but taking the set distance from all walls into account.
    /// </summary>
    /// <returns>Random value between 0f and 1f.</returns>
    //private readonly float m_DistFromWall = 0.3f;
    private float GetRandom(float dist)
    {
        return Random.Range(0f + dist, 1f - dist);
    }

    /// <summary>
    /// Reset the position of the target to a random position. Based on the created rooms in the environment.
    /// </summary>
    public bool targetFixedPosition = false;
    public void ResetTargetPosition()
    {
        if (targetFixedPosition)
        {
            target.transform.localPosition = new Vector3(0f, 0.5f, -12f);
        }
        else
        {
            var newTargetPos = RoomsInEnv.GetRandomTargetPosition(agent.transform.position);
            target.ResetPosition(newTargetPos);
        }
    }
    
    /// <summary>
    /// Get the global position of the created door in the inner wall.
    /// </summary>
    public Vector3 GetDoorPosition()
    {
        return m_DoorCentreGlobalCoord;
    }
}
