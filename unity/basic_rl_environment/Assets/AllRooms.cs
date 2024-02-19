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
    
    // Indicates if agent and target are placed in the same room.
    private bool m_AgentAndTargetInSameRoom;

    private CustomStatsManager m_StatsManager;
    /// <summary>
    /// Constructor: Setup list for the rooms and set where the target needs to be located.
    /// </summary>
    /// <param name="targetInOtherRoom"></param>
    public AllRooms(bool createWall, bool targetInOtherRoom, CustomStatsManager mgmt)
    {
        m_AllRoomsInEnv = new List<Room>();
        m_TargetAlwaysInOtherRoomFromAgent = targetInOtherRoom;
        m_CreateWall = createWall;
        m_AgentAndTargetInSameRoom = false;

        m_StatsManager = mgmt;
    }
    
    /// <summary>
    /// Add a new room.
    /// </summary>
    public void AddRoom(List<Vector3> corners, int roomId)
    {
        m_AllRoomsInEnv.Add(new Room(corners, roomId));
    }
    
    /// <summary>
    /// Clear stored rooms and reset indicator for agent and target in the same room.
    /// </summary>
    public void Clear()
    {
        m_AllRoomsInEnv.Clear();
        m_AgentAndTargetInSameRoom = false;
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
    /// Reset the target location information in every room. Including the same room indicator.
    /// </summary>
    /// <remarks>Important function because multiple positions can be requested for the target due to
    /// insufficient distance to other objects. In the loop the variables must be reset to the default.
    /// </remarks>
    private void ResetTargetLocation()
    {
        m_AgentAndTargetInSameRoom = false;
        foreach (var singleRoom in m_AllRoomsInEnv)
        {
            singleRoom.SetTargetNotPresent();
        }
    }

    private Room m_SelectedRoom;
    public void SelectRandomRoom()
    {
        var index = Random.Range(0, m_AllRoomsInEnv.Count);
        m_StatsManager.AllRoomsRandomAdd(index);
        m_SelectedRoom = m_AllRoomsInEnv[index];
    }

    /// <summary>
    /// Get a random position for the target. Global position is returned.
    /// </summary>
    /// <param name="type">For what type of object a position is requested: Target or Agent.</param>
    /// <param name="agentGlobalPosition">Vector3 containing the global position of the agent.</param>
    /// <returns>Vector3 with random position to be used for the target.</returns>
    public Vector3 GetRandomPosition(PositionType type, Vector3 agentGlobalPosition)
    {
        // Generate a random position for the target.
        if (type is PositionType.Target)
        {
            // Update and store the new agent location.
            UpdateAgentLocation(agentGlobalPosition);
            
            // Ensure the target location information in every room is reset. Including same room indicator.
            ResetTargetLocation();
            
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
            // Select a random room from the possible rooms.
            /*var index = Random.Range(0, m_AllRoomsInEnv.Count);
            m_StatsManager.AllRoomsRandomAdd(index);
            var selectedRoom = m_AllRoomsInEnv[index];*/

            var selectedRoom = m_SelectedRoom;
            
            // If agent and target are going to be in the same room, indicate so.
            if (selectedRoom.ContainsAgent()) m_AgentAndTargetInSameRoom = true;
            selectedRoom.SetTargetPresent();
            return selectedRoom.GetRandomPositionWithin();
        }
        
        // Random position for the agent is requested. This position can be in any room.
        if (type is PositionType.Agent)
        {
            // Select a random room from the possible rooms.
            // ToDo: What even is this? The room select for the agent is NOT random. Revert this?
            var selectedRoom = m_AllRoomsInEnv[Random.Range(0, m_AllRoomsInEnv.Count)];
            //var selectedRoom = m_AllRoomsInEnv[0];
            // Set the appropriate agent indicator for the room.
            selectedRoom.SetAgentPresent();
            
            // Return a position.
            return selectedRoom.GetRandomPositionWithin();
        }
        
        // Bad default case. Consider to throw an exception.
        return Vector3.zero;
    }
    
    /// <summary>
    /// Are agent and target placed in the same room during creation on the current environment.
    /// </summary>
    /// <returns></returns>
    public bool AreAgentAndTargetInSameRoom()
    {
        return m_AgentAndTargetInSameRoom;
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
