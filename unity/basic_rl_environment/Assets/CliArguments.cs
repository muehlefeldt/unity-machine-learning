using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;


public class CliArguments : MonoBehaviour
{
    /// <summary>
    /// Requested sensor count of the agent. Default is set to 32. Range of 1 to 64 allowed.
    /// </summary>
    public int SensorCount { get; private set; } = 32;

    /// <summary>
    /// Requested path of the stats file to be created at the end of the ml-agents run.
    /// </summary>
    public string StatsExportPath { get; private set; } = "c:/build/test.json";

    private string[] m_Args;
    public GameObject allTrainingAreas;
    ///<summary> Called before loading of agents to gather requested settings from CLI. </summary>
    /// <remarks>This function is always called before any Start functions and also just after a prefab
    /// is instantiated.
    /// (If a GameObject is inactive during start up Awake is not called until it is made active.)</remarks>
    protected void Awake()
    {
        var arguments = Environment.GetCommandLineArgs();
        m_Args = arguments;
        // Make all entries from CLI lower case.
        arguments = arguments.Select(s => s.ToLower()).ToArray();
        
        // Look for sensor count in provided arguments.
        var index = Array.IndexOf(arguments, "--sensors");
        if (index > 0 && int.TryParse(arguments[index + 1], out int newCount))
        {
            if (newCount is > 0 and < 64)
            {
                SensorCount = newCount;    
            }
        }
        
        index = Array.IndexOf(arguments, "--statspath");
        if (index > 0 && index + 1 <= arguments.Length - 1)
        {
            StatsExportPath = arguments[index + 1];
        }
        
        allTrainingAreas.SetActive(true);
    }

    private void OnGUI()
    {
        GUI.Label(new Rect(10, 400, 1000, 2000), string.Join(" ", m_Args));
    }
}
