using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class CustomStatsManager : MonoBehaviour
{
    // Object used to ensure safe access and use of function by multiple agents.
    private readonly object m_LockObject = new object();

    // Contains the actual stats.
    private Stats m_Stats;
    
    // Path to the export file.
    private string m_ExportPath;
    
    /// <summary>
    /// Setup the stats management.
    /// </summary>
    private void Awake()
    {
        m_Stats = new Stats();
        
        var configMgmt = FindObjectOfType<ConfigurationMgmt>();
        lock (m_LockObject)
        {
            m_Stats.runId = configMgmt.config.runId;
            m_ExportPath = configMgmt.config.statsExportPath;
            m_Stats.sensorCount = configMgmt.config.sensorCount;
        }
    }
    
    /// <summary>
    /// Increments the number of times the agent was in room 0.
    /// </summary>
    public void AgentInRoomID(int id)
    {
        lock (m_LockObject)
        {
            switch (id)
            {
                case 0:
                    m_Stats.agentInRoomID0 += 1;
                    break;
                case 1:
                    m_Stats.agentInRoomID1 += 1;
                    break;
            }
        }
    }
    
    /// <summary>
    /// Increments the number of times the target was in room 0.
    /// </summary>
    public void TargetInRoomID(int id)
    {
        lock (m_LockObject)
        {
            switch (id)
            {
                case 0:
                    m_Stats.targetInRoomID0 += 1;
                    break;
                case 1:
                    m_Stats.targetInRoomID1 += 1;
                    break;
            }
        }
    }
    
    /// <summary>
    /// Increments the number of times the target was in room 0.
    /// </summary>
    public void TargetInRoomID1()
    {
        lock (m_LockObject)
        {
            m_Stats.targetInRoomID1 += 1;
        }
    }

    /// <summary>
    /// Record that agent and target are located in the same room.
    /// </summary>
    public void TargetAndAgentSameRoom()
    {
        lock (m_LockObject)
        {
            m_Stats.sameRoom += 1;
        }
    }
    
    /// <summary>
    /// Record the start of a new episode.
    /// </summary>
    public void NewEpisode()
    {
        lock (m_LockObject)
        {
            m_Stats.episodeCount += 1;
        }
    }

    public void AllRoomsRandomAdd(int index)
    {
        lock (m_LockObject)
        {
            switch (index)
            {
                case 0:
                    m_Stats.targetRoomIndex0 += 1;
                    break;
                case 1:
                    m_Stats.targetRoomIndex1 += 1;
                    break;
            }
        }
    }

    /// <summary>
    /// GUI output to help debugging and visual supervision.
    /// </summary>
    /// <remarks>Only used if using Unity editor. Otherwise no use and can be ignored during compile.</remarks>
/*#if UNITY_EDITOR
    private void OnGUI()
    {
        GUI.Label(new Rect(500, 10, 1000, 100), string.Format("Agent and target in same room counter: {0}\nEp counter: {1}",m_Stats.sameRoom,m_Stats.episodeCount));
    }
#endif*/
    
    /*private bool m_ShowLabel = false;
    /*private void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 1000, 2000), m_GuiText);
    }#1#
    
    void OnGUI() {
        if (GUILayout.Button("Stats"))
        {
            m_ShowLabel = !m_ShowLabel;
        }

        if (m_ShowLabel)
        {
            GUILayout.Label(m_Stats.ToString());
            //Label(new Rect(10, 10, 1000, 2000), sameRoom.ToString());
        }
    }*/
    
    /// <summary>
    /// On exit to the program write stats to a file.
    /// </summary>
    private void OnApplicationQuit()
    {
        // Do not overwrite an exising file. Maybe the case, if a config is rerun without a change to the config file.
        if (!File.Exists(m_ExportPath))
        {
            lock (m_LockObject)
            {
                // Create json string from the stats object.
                var json = JsonUtility.ToJson(m_Stats, prettyPrint: true);
                
                // Ensure pure UTF8 encoding is used.
                var customUtf8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
                
                // Export the string to file.
                File.WriteAllText(m_ExportPath, json, customUtf8Encoding);
            }
        }
    }
}
