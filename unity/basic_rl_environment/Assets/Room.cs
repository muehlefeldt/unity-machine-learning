using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

public class Room
{
    private List<Vector3> m_CornersGlobalCoords;
    private Vector3 m_MinXGlobalCoord;
    private Vector3 m_MaxXGlobalCoord;
    private Vector3 m_MinZGlobalCoord;
    private Vector3 m_MaxZGlobalCoord;

    private bool m_ContainsTarget = false;
    private bool m_ContainsAgent = false;
    
    /// <summary>
    /// Constructor:
    /// </summary>
    /// <param name="corners"></param>
    public Room(List<Vector3> corners)
    {
        m_CornersGlobalCoords = corners;
        var sortedX = corners.OrderBy(v => v.x).ToList();
        var sortedZ = corners.OrderBy(v => v.z).ToList();
        m_MinXGlobalCoord = sortedX[0];
        m_MaxXGlobalCoord = sortedX[^1];
        m_MinZGlobalCoord = sortedZ[0];
        m_MaxZGlobalCoord = sortedZ[^1];
    }
    
    /// <summary>
    /// Does this room contain the given global coordinates? Decision is based on the known global corner coordinates
    /// of the room.
    /// </summary>
    /// <returns></returns>
    public bool DoesContainGlobalCoord(Vector3 coord)
    {
        var sortedByX = m_CornersGlobalCoords;
        var sortedByZ = m_CornersGlobalCoords.OrderBy(v => v.z).ToList();
        
        if (coord.x <= sortedByX[0].x &&
            coord.x >= sortedByX[3].x &&
            coord.z <= sortedByZ[0].z &&
            coord.z <= sortedByZ[3].z)
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
    /// Does this contain the agent?
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
    /// Calculation is based on the known corners of the room.
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