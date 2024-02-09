using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;


public class ConfigurationMgmt : MonoBehaviour
{
    /// <summary>
    /// Requested sensor count of the agent. Default is set to 32. Range of 1 to 64 allowed.
    /// </summary>
    //public int SensorCount { get; private set; } = 32;

    /// <summary>
    /// Requested path of the stats file to be created at the end of the ml-agents run.
    /// </summary>
    //public string StatsExportPath { get; private set; } = "c:/build/test.json";

    public Configuration config { get; private set; }
    
    //private string[] m_Args;
    public GameObject allTrainingAreas;

    private string m_GuiText;
    
    ///<summary> Called before loading of agents to gather requested settings from CLI. </summary>
    /// <remarks>This function is always called before any Start functions and also just after a prefab
    /// is instantiated.
    /// (If a GameObject is inactive during start up Awake is not called until it is made active.)</remarks>
    protected void Awake()
    {
        // Read the JSON file from the DATA dir: If editor is used, this is the Assets folder. Otherwise Data folder.
        string jsonFilePath = Application.dataPath + "/env_config.json";
        if (!File.Exists(jsonFilePath))
        {
            throw new FileNotFoundException("Config file for Unity environment not found.");
        }
        
        string jsonString = File.ReadAllText(jsonFilePath);

        // Deserialize the JSON data into a C# object
        config = JsonUtility.FromJson<Configuration>(jsonString);

        m_GuiText = string.Format("Run {0}\nSensor count {1}\nStats file {2}", config.runId, config.sensorCount, config.statsExportPath);
        
        // Activate the training areas. This ensure the correct call order of Awake() within the areas.
        allTrainingAreas.SetActive(true);
    }
    
    private void OnGUI()
    {
        GUI.Label(new Rect(10, 400, 1000, 2000), m_GuiText);
    }
    
}
