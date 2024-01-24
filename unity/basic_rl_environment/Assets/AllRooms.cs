using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class AllRooms
{
    private List<Room> m_AllRoomsInEnv; // Contains all the current rooms in the env.
    private bool m_TargetAlwaysInOtherRoomFromAgent;
    private bool m_CreateWall;
    private Vector3 m_CurrentAgentPos;
    private int m_CurrentAgentRoomId;
    
    /// <summary>
    /// Constructor: Setup list for the rooms and set where the target needs to be located.
    /// </summary>
    /// <param name="targetInOtherRoom"></param>
    public AllRooms(bool createWall, bool targetInOtherRoom)
    {
        m_AllRoomsInEnv = new List<Room>();
        m_TargetAlwaysInOtherRoomFromAgent = targetInOtherRoom;
        m_CreateWall = createWall;
    }
    
    /// <summary>
    /// Add a new room.
    /// </summary>
    public void AddRoom(List<Vector3> corners, int roomId)
    {
        m_AllRoomsInEnv.Add(new Room(corners, roomId));
    }
    
    /// <summary>
    /// Clear stored rooms.
    /// </summary>
    public void Clear()
    {
        m_AllRoomsInEnv.Clear();
    }
    
    /// <summary>
    /// Update the information which room does contain the agent. Based on the provided global position.
    /// Relevant because of the possible requirement to position the target in a different room from the agent.
    /// </summary>
    private void UpdateAgentLocation(Vector3 agentGlobalPosition)
    {
        // Update the information which room does contain the agent in the current env state.
        m_CurrentAgentPos = agentGlobalPosition;
        
        // Parse every room. Due to the limited number of rooms, this should be no performance issue.
        foreach (var singleRoom in m_AllRoomsInEnv)
        {
            // If the room contains the agent position, set agent contained and save room id.
            if (singleRoom.DoesContainGlobalCoord(m_CurrentAgentPos))
            {
                singleRoom.SetAgentPresent();
                m_CurrentAgentRoomId = singleRoom.GetId();
            }
            else
            {
                singleRoom.SetNotAgentPresent();
            }
        }
    }
    
    /// <summary>
    /// Return the Id of the room containing the global position of the agent.
    /// </summary>
    /// <param name="agentGlobalPosition">Vector3 containing the global position of the agent.</param>
    /// <returns></returns>
    public int GetCurrentAgentRoomId(Vector3 agentGlobalPosition)
    {
        UpdateAgentLocation(agentGlobalPosition);
        return m_CurrentAgentRoomId;
    }

    public enum PositionType
    {
        /// <summary>
        /// Position for target requested.
        /// </summary>
        Target,
        /// <summary>
        /// Agent position requested.
        /// </summary>
        Agent
    }
    
    /// <summary>
    /// Get a random position for the target. Global position is returned.
    /// </summary>
    /// <param name="agentGlobalPosition">Vector3 containing the global position of the agent.</param>
    /// <returns>Vector3 with random position to be used for the target.</returns>
    public Vector3 GetRandomPosition(PositionType type, Vector3 agentGlobalPosition)
    {
        if (type is PositionType.Target)
        {
            UpdateAgentLocation(agentGlobalPosition);

            // No wall should mean there is only one room within the env.
            if (!m_CreateWall)
            {
                foreach (var singleRoom in m_AllRoomsInEnv)
                {
                    return singleRoom.GetRandomPositionWithin();
                }
            }

            // If the target always shall be in the other room from the agent.
            if (m_CreateWall && m_TargetAlwaysInOtherRoomFromAgent)
            {
                foreach (var singleRoom in m_AllRoomsInEnv)
                {
                    // Only if the room does not contain the agent the target can be positioned in the room.
                    if (!singleRoom.ContainsAgent())
                    {
                        return singleRoom.GetRandomPositionWithin();
                    }
                }
            }

            // If not a different room is requested a random position is calculated. The position can be in every room. 
            var possibleRandomPos = new List<Vector3>();
            foreach (var singleRoom in m_AllRoomsInEnv)
            {
                possibleRandomPos.Add(singleRoom.GetRandomPositionWithin());
            }

            return possibleRandomPos[Random.Range(0, possibleRandomPos.Count)];
        }

        if (type is PositionType.Agent)
        {
            var possibleRandomPos = new List<Vector3>();
            foreach (var singleRoom in m_AllRoomsInEnv)
            {
                possibleRandomPos.Add(singleRoom.GetRandomPositionWithin());
            }

            return possibleRandomPos[Random.Range(0, possibleRandomPos.Count)];
        }
        else
        {
            return Vector3.zero;
        }
    }
    
    /// <summary>
    /// Return list with all rooms in the environment.
    /// </summary>
    /// <returns></returns>
    public List<Room> GetAllRooms()
    {
        return m_AllRoomsInEnv;
    }
}
