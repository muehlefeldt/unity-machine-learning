using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class AllRooms
{
    private List<Room> m_AllRoomsInEnv; // Contains all the current rooms in the env.
    private bool m_TargetAlwaysInOtherRoomFromAgent;
    private bool m_CreateWall;
    private Vector3 m_CurrentAgentPos;
    
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
    public void AddRoom(List<Vector3> corners)
    {
        m_AllRoomsInEnv.Add(new Room(corners));
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
    private void UpdateAgentLocation()
    {
        // Parse every room. Due to the limited number of rooms, this should be no performance issue.
        foreach (var singleRoom in m_AllRoomsInEnv)
        {
            if (singleRoom.DoesContainGlobalCoord(m_CurrentAgentPos))
            {
                singleRoom.SetAgentPresent();
            }
            else
            {
                singleRoom.SetNotAgentPresent();
            }
        }
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
            // Update the information which room does contain the agent in the current env state.
            m_CurrentAgentPos = agentGlobalPosition;
            UpdateAgentLocation();

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
}
