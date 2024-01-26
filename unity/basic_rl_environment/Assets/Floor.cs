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
    // Maximum possible distance on this floor.
    private float m_MaxDist = 1f;
    
    private InnerWallCreator innerWallCreator;

    public AllRooms RoomsInEnv;
    public RollerAgent agent;
    public Target target;
    
    // Select if a decoy object is requested.
    public bool useDecoy = false;
    private Decoy m_Decoy;
    
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
    
    // Set width of the door through the unity editor. Use the prefab.
    public float doorWidth = 5f;
    
    // Public bools to be set through the Unity editor. Allow for a simpler environment during the training. 
    public bool CreateWall = true;
    public bool RandomWallPosition = true;
    public bool RanndomDoorPosition = true;
    public bool targetAlwaysInOtherRoomFromAgent = false;
   
    // Start is called before the first frame update
    void Awake()
    {
        // Get the MeshFilter component of the floor object.
        MeshFilter floorMeshFilter = GetComponent<MeshFilter>();
        
        // Initialise the creator object for the inner wall. 
        innerWallCreator = new InnerWallCreator(transform);
        
        // Setup the room management.
        RoomsInEnv = new AllRooms(CreateWall, targetAlwaysInOtherRoomFromAgent);

        // Ensure the MeshFilter and its shared mesh exist
        if (floorMeshFilter != null && floorMeshFilter.sharedMesh != null)
        {
            // Get the shared mesh of the floor
            Mesh floorMesh = floorMeshFilter.sharedMesh;

            var corners = new List<Vector3>();
            m_CornersGlobalCoords = corners;
            
            corners.Add(transform.TransformPoint(floorMesh.vertices[0]));
            corners.Add(transform.TransformPoint(floorMesh.vertices[10]));
            corners.Add(transform.TransformPoint(floorMesh.vertices[110]));
            corners.Add(transform.TransformPoint(floorMesh.vertices[120]));

            // Store max and min values corners for later use.
            var sortedX = corners.OrderBy(v => v.x).ToList();
            var sortedZ = corners.OrderBy(v => v.z).ToList();
            m_MinXGlobalCoord = sortedX[0];
            m_MaxXGlobalCoord = sortedX[^1];
            m_MinZGlobalCoord = sortedZ[0];
            m_MaxZGlobalCoord = sortedZ[^1];
            
            CalculateMaxDist();

            if (useDecoy)
            {
                m_Decoy = new Decoy(this.transform);
            }
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
    
    /// <summary>
    /// Check if global position is outside the floor perimeter.
    /// </summary>
    /// <param name="pos">Global position to check.</param>
    /// <returns>True if the position is outside of the floor.</returns>
    public bool IsOutsideFloor(Vector3 pos)
    {
        if (pos.x < m_MinXGlobalCoord.x || pos.x > m_MaxXGlobalCoord.x)
        {
            return true;
        }

        if (pos.z < m_MinZGlobalCoord.z || pos.z > m_MaxZGlobalCoord.z)
        {
            return true;
        }

        return false;
    }
    
#if UNITY_EDITOR
    /// <summary>
    /// Visual cues for debugging in the editor.
    /// </summary>
    void OnDrawGizmos()
    {   
        // In case of an inner wall. Highlight start and end of the wall including the door.
        if (CreateWall)
        {
            foreach (var coord in new List<Vector3>() { m_WallStartGlobalCoord, m_WallEndGlobalCoord })
            {
                Gizmos.DrawSphere(coord, 0.3f);
                Handles.Label(coord, "Wall");
            }
            
            foreach (var coord in new List<Vector3>()
                         { m_DoorStartGlobalCoord, m_DoorCentreGlobalCoord, m_DoorEndGlobalCoord })
            {
                Gizmos.DrawWireSphere(coord, 0.3f);
                Handles.Label(coord, "Door");
            }
        }
        
        // Show id of the rooms.
        foreach (var singleRoom in RoomsInEnv.GetAllRooms())
        {
            Handles.Label(singleRoom.GetMiddlePosition(), singleRoom.GetId().ToString());
        }
    }
    
    private void OnGUI()
    {
        var text = "";
        foreach (var singleRoom in RoomsInEnv.GetAllRooms())
        {
            text += String.Format(
                "Room {0}\n" +
                "Contains agent: {1}\n" +
                "Contains target: {2}\n", 
                singleRoom.GetId(), singleRoom.ContainsAgent(), singleRoom.ContainsTarget());
        }

        text += String.Format("Agent and Target same room: {0}\n", RoomsInEnv.AreAgentAndTargetInSameRoom());
        GUI.Label(new Rect(10, 200, 1000, 200), text);
    }

#endif
    
    /// <summary>
    /// Get the max possible distance on the current training area.
    /// </summary>
    public float GetMaxPossibleDist()
    {
        return m_MaxDist;
    }
    
    /// <summary>
    /// Prepare the floor for new episode.
    /// </summary>
    private void Prepare()
    {
        innerWallCreator.DestroyAll();
        
        // Previously created rooms can be removed.
        RoomsInEnv.Clear();
    }
    
    /// <summary>
    /// Create rooms by creating a inner wall.
    /// </summary>
    /// <remarks>Function is also called if only one room is used.
    /// In this case the single room is stored and prepared for further use.
    /// Reference: https://answers.unity.com/questions/52747/how-i-can-create-a-cube-with-specific-coordenates.html
    /// </remarks>
    public void CreateInnerWall()
    {
        // Prepare the floor for a new wall.
        Prepare();
        
        // The wall inner wall is only created if selected in the Unity editor. Set to true as default.
        if (CreateWall)
        {
            // Calculate and store inner wall and door positions.
            SetWallCoords();
            SetDoorCoords();
            
            // Create the game objects to construct the wall. 
            innerWallCreator.CreateWallWithDoor((m_WallStartGlobalCoord, m_WallEndGlobalCoord), (m_DoorStartGlobalCoord, m_DoorEndGlobalCoord));

            // Add the created rooms to the room management object.
            RoomsInEnv.AddRoom(new List<Vector3>{m_CornersGlobalCoords[0], m_CornersGlobalCoords[1], m_WallStartGlobalCoord, m_WallEndGlobalCoord}, roomId: 0);
            RoomsInEnv.AddRoom(new List<Vector3>{m_CornersGlobalCoords[2], m_CornersGlobalCoords[3], m_WallStartGlobalCoord, m_WallEndGlobalCoord}, roomId: 1);
        }
        else
        {
            // Env has only one room when no wall is created. Only one room needs to be stored.
            RoomsInEnv.AddRoom(m_CornersGlobalCoords, roomId: 0);
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
        if (RanndomDoorPosition)
        {
            r = GetRandom(0.3f);
        }
        var pos = Vector3.zero;
        pos.x = Vector3.Lerp(m_MinZGlobalCoord, m_MaxZGlobalCoord, r).x;
        
        // Set y to a fixed value to ensure a more realistic distance calculation to the target through the door.
        pos.y = 0.75f;
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
    /// <remarks>Be sure to call this function only after the position of the agent was changed.</remarks>
    public bool targetFixedPosition = false;
    public void ResetTargetPosition()
    {
        if (targetFixedPosition) // Target shall always be spawned at the same position.
        {
            target.transform.localPosition = new Vector3(0f, 0.5f, -12f);
        }
        else // Random position of the target requested.
        {
            var distToDoor = 0f;
            var distToAgent = 0f;
            
            var newTargetPos = Vector3.zero;
            
            // While distance to door or to the agent is too short, get new position.
            while (distToDoor < 4f || distToAgent < 4f)
            {
                // Get the possible condition of the target.
                newTargetPos = RoomsInEnv.GetRandomPosition(AllRooms.PositionType.Target, agent.transform.position);
                
                // Get distance to the door.
                distToDoor = CreateWall ? Vector3.Distance(newTargetPos, m_DoorCentreGlobalCoord) :
                    // If we do not have a door present, the distance is in every case ok and can be assumed as infinity.
                    float.PositiveInfinity;
                
                // Get the distance to the agent.
                distToAgent = Vector3.Distance(newTargetPos, agent.transform.position);
            }
            
            // Set the position of the target.
            target.ResetPosition(newTargetPos);
        }
    }
    
    /// <summary>
    /// Get random position for the agent. Position can be in all rooms.
    /// </summary>
    /// <returns>Global position within the training area.</returns>
    public Vector3 GetRandomAgentPosition()
    {
        return RoomsInEnv.GetRandomPosition(AllRooms.PositionType.Agent, agent.transform.position);
    }

    public int GetAgentRoomId(Vector3 agentGlobalPosition)
    {
        return RoomsInEnv.GetCurrentAgentRoomId(agentGlobalPosition);
    }

    
    /// <summary>
    /// Reset the position of the decoy object. Takes distance to target and door into account.
    /// </summary>
    public void ResetDecoyPosition()
    {
        if (useDecoy)
        {
            var agentPos = agent.transform.position;
            var targetPos = target.transform.position;

            var newDecoyPos = RoomsInEnv.GetRandomPosition(AllRooms.PositionType.Target, agentPos);

            var distToTarget = Vector3.Distance(newDecoyPos, targetPos);
            var distToDoor = Vector3.Distance(newDecoyPos, m_DoorCentreGlobalCoord);
            while (distToTarget < 4f || distToDoor < 4f)
            {
                newDecoyPos = RoomsInEnv.GetRandomPosition(AllRooms.PositionType.Target, agentPos);
                distToTarget = Vector3.Distance(newDecoyPos, targetPos);
                distToDoor = Vector3.Distance(newDecoyPos, m_DoorCentreGlobalCoord);
            }

            // Set the correct height of the decoy.
            newDecoyPos.y = 1f;
            m_Decoy.ResetPosition(newDecoyPos);
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
