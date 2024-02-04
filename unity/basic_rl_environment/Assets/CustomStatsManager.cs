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
    private Stats m_Stats = new Stats();

    public string exportPath;
    
    private void Awake()
    {
        exportPath = @"C:\Users\max.muehlefeldt\Documents\GitHub\unity-machine-learning\python\basic_rl_env\stats\" + DateTime.Now + ".json";
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
    
    /// <summary>
    /// On exit to the program write stats to a file.
    /// </summary>
    private void OnApplicationQuit()
    {
        if (!File.Exists(exportPath))
        {
            lock (m_LockObject)
            {
                var json = JsonUtility.ToJson(m_Stats, prettyPrint: true);
                System.IO.File.WriteAllText(exportPath, json, Encoding.UTF8);
            }
        }
    }
}
