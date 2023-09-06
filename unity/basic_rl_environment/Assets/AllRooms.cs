using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class AllRooms
{
    private List<Room> m_AllRoomsInEnv; // Contains all the current rooms in the env.
    private bool m_TargetAlwaysInOtherRoomFromAgent;
    private Vector3 m_CurrentAgentPos;
    
    /// <summary>
    /// Constructor: Setup list for the rooms and set where the target needs to be located.
    /// </summary>
    /// <param name="targetInOtherRoom"></param>
    public AllRooms(bool targetInOtherRoom)
    {
        m_AllRoomsInEnv = new List<Room>();
        m_TargetAlwaysInOtherRoomFromAgent = targetInOtherRoom;
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
    /// <param name="agentGlobalPosition"></param>
    private void UpdateAgentLocation()
    {
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
    
    /// <summary>
    /// Get a random position for the target. Global position is returned.
    /// </summary>
    /// <param name="agentGlobalPosition"></param>
    /// <returns></returns>
    public Vector3 GetRandomTargetPosition(Vector3 agentGlobalPosition)
    {
        // Update the information which room does contain the agent in the current env state.
        m_CurrentAgentPos = agentGlobalPosition;
        UpdateAgentLocation();
        
        // If the target always shall be in the other room from the agent.
        if (m_TargetAlwaysInOtherRoomFromAgent)
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

        return possibleRandomPos[Random.Range(0, possibleRandomPos.Count - 1)];
        
    }
}
