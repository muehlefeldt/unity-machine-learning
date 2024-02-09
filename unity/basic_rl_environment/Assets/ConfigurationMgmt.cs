using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Class to manage loading of configuration file to setup training areas as requested by the user.
/// </summary>
public class ConfigurationMgmt : MonoBehaviour
{
    /// <summary>
    /// Property to store the loaded configuration.
    /// </summary>
    public Configuration config { get; private set; }
    
    /// <summary>
    /// Game objects that contains all training areas. To be set through the editor.
    /// </summary>
    public GameObject allTrainingAreas;

    /// <summary>
    /// Text to be displayed on the GUI.
    /// </summary>
    private string m_GuiText;
    
    ///<summary> Called before loading of agents to gather requested settings. </summary>
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
        
        // Load the actual json.
        string jsonString = File.ReadAllText(jsonFilePath);

        // Deserialize the JSON data into a C# object.
        config = JsonUtility.FromJson<Configuration>(jsonString);
        
        // Set the GUI text. No need to call this more often.
        m_GuiText = string.Format("Run {0}\nSensor count {1}\nStats file {2}", config.runId, config.sensorCount, config.statsExportPath);
        
        // Activate the training areas. This ensure the correct call order of Awake() within the areas.
        allTrainingAreas.SetActive(true);
    }
    
    private void OnGUI()
    {
        GUI.Label(new Rect(10, 400, 1000, 2000), m_GuiText);
    }
    
}
