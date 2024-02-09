[System.Serializable]
public class Configuration
{
    /// <summary>
    /// Path to file for export of stats recorded during the run.
    /// </summary>
    public string statsExportPath;
    
    /// <summary>
    /// How many horizontal sensors are requested?
    /// </summary>
    public int sensorCount;
    
    /// <summary>
    /// ID of the ml-agents run. Provides easy way to identify run. Can be shown in the GUI.
    /// </summary>
    public int runId;
}
