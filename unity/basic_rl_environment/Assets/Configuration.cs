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

    /// <summary>
    /// Deploy a decoy object to increase complexity of the environment. Default set to false.
    /// </summary>
    public bool useDecoy;
    
    /// <summary>
    /// Create a inner wall separating two rooms. Default set to true.
    /// </summary>
    public bool createWall;
    
    /// <summary>
    /// Width of the created door. Default set to 4.0.
    /// </summary>
    public float doorWidth;
    
    /// <summary>
    /// Use random position of the inner wall. Both rooms are of varying size. Default set to true.
    /// </summary>
    public bool randomWallPosition;
    
    /// <summary>
    /// Random position of the door within the inner wall. Default set to true.
    /// </summary>
    public bool randomDoorPosition;
    
    /// <summary>
    /// Select if target and agent are always in different rooms. Default set to false.
    /// </summary>
    public bool targetAlwaysInOtherRoomFromAgent;
    
    /// <summary>
    /// Fix the position of the target. Position will be hard coded.
    /// </summary>
    public bool targetFixedPosition;
    
    /// <summary>
    /// The maximum number of steps the agent takes before being done.
    /// </summary>
    public int maxStep;
}
