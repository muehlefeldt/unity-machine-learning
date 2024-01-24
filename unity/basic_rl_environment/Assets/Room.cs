using System;
using UnityEngine;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Random = UnityEngine.Random;

public class Room
{
    private List<Vector3> m_CornersGlobalCoords;
    private Vector3 m_MinXGlobalCoord;
    private Vector3 m_MaxXGlobalCoord;
    private Vector3 m_MinZGlobalCoord;
    private Vector3 m_MaxZGlobalCoord;

    private int m_Id;

    private bool m_ContainsTarget = false;
    private bool m_ContainsAgent = false;
    
    /// <summary>
    /// Constructor:
    /// </summary>
    /// <param name="corners"></param>
    public Room(List<Vector3> corners, int roomId)
    {
        // Store the corner coords sorted by x value..
        m_CornersGlobalCoords = corners.OrderBy(v => v.x).ToList();
        
        // Set room ID.
        m_Id = roomId;
        
        // Store max and min values corners for later use.
        var sortedX = m_CornersGlobalCoords;
        var sortedZ = corners.OrderBy(v => v.z).ToList();
        m_MinXGlobalCoord = sortedX[0];
        m_MaxXGlobalCoord = sortedX[^1];
        m_MinZGlobalCoord = sortedZ[0];
        m_MaxZGlobalCoord = sortedZ[^1];
    }

    /// <summary>Get the ID of the room.</summary>
    public int GetId()
    {
        return m_Id;
    } 
    
    /// <summary>
    /// Does this room contain the given global coordinates? Decision is based on the known global corner coordinates
    /// of the room. Does ignore walls and other objects in the room. Simply based on coords.
    /// </summary>
    /// <returns>Returns true if global coords are within in the room. Otherwise false.</returns>
    public bool DoesContainGlobalCoord(Vector3 coord)
    {
        // Check if coord lies between the corner coords. Only x and z is relevant.
        if (m_MinXGlobalCoord.x <= coord.x && coord.x <= m_MaxXGlobalCoord.x &&
            m_MinZGlobalCoord.z <= coord.z && coord.z <= m_MaxZGlobalCoord.z)
        {
            return true;
        }

        return false;
    }

    public bool ContainsTarget()
    {
        return m_ContainsTarget;
    }
    
    /// <summary>
    /// Does this room contain the agent?
    /// </summary>
    /// <returns>True if agent is the room. Otherwise false.</returns>
    public bool ContainsAgent()
    {
        return m_ContainsAgent;
    }
    
    /// <summary>
    /// Set the room contains the agent.
    /// </summary>
    public void SetAgentPresent()
    {
        m_ContainsAgent = true;
    }
    
    /// <summary>
    /// Set this room does not contain in the agent.
    /// </summary>
    public void SetNotAgentPresent()
    {
        m_ContainsAgent = false;
    }

    public void SetTargetPresent()
    {
        m_ContainsTarget = true;
    }
    /// <summary>
    /// Calculate a random position within the room. Returns a Vector3 containing global coordinates (world coords).
    /// Calculation is based on the known corners of the room. Takes an minimum distance from the walls into
    /// account.
    /// </summary>
    /// <returns></returns>
    public Vector3 GetRandomPositionWithin()
    {
        var pos = Vector3.zero;
        pos.x = Vector3.Lerp(m_MinXGlobalCoord, m_MaxXGlobalCoord, GetRandom()).x;
        pos.y = 0.5f;
        pos.z = Vector3.Lerp(m_MinZGlobalCoord, m_MaxZGlobalCoord, GetRandom()).z;;
        return pos;
    }

    public Vector3 GetMiddlePosition()
    {
        var pos = Vector3.Lerp(m_CornersGlobalCoords[0], m_CornersGlobalCoords.Last(), 0.5f);
        pos.y = 0.5f;
        return pos;
    }
    
    /// <summary>
    /// Get random value between 0f and 1f but taking the set distance from all walls into account.
    /// </summary>
    /// <returns>Random value between 0f and 1f.</returns>
    private readonly float m_DistFromWall = 0.1f;
    private float GetRandom()
    {
        return Random.Range(0f + m_DistFromWall, 1f - m_DistFromWall);
    }
}