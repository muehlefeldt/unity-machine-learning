using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Unity.Mathematics;
//using DefaultNamespace;
using UnityEngine;
using UnityEditor;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.VisualScripting;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

/// <summary>
/// Class to manage the agent.
/// </summary>
public class RollerAgent : Agent
{
    private Rigidbody m_RBody;
    //public Target target;
    public Floor floor;
    private float m_MaxDist;
    
    // Distances.
    private float m_DistToTarget;
    private float m_DistToTargetNormal;
    private float m_LastDistToTarget;
    private float m_LastDistToTargetNormal;
    
    // Set how much force is applied to the rigid body component.
    public float forceMultiplier = 10f;
    
    /// <summary>Select sensor count of the agent. These are the main sensors to aid navigation and exploration.
    /// Has no influence on sensors along y axis, i.e. height sensors remain constant.</summary>
    [Range(1, 32)]
    [Tooltip("Number of horizontal sensors.")]
    public int sensorCount = 4;
    private readonly List<float> m_RayDistances = new List<float>();
    
    /// <summary>
    /// Allow or disallow height change by the drone. Has influence on the sensor count and inputs.
    /// </summary>
    [Tooltip("Can the drone change its height along the y axis.")]
    public bool allowYMovement = false;
    
    // Internal sensor setup.
    private List<Vector3> m_SensorDirections;
    
    // Last detected collision between agent and other object. Also last implausible position.
    private Vector3 m_LastCollision = Vector3.zero;
    private Vector3 m_LastImplausiblePos = Vector3.zero;
    
    // Number of complete door passages. Reset at every episode start.
    private int m_DoorPassages;
    
    // Gui text for debugging in the editor.
    private string m_GuiText;

    private Configuration m_Config;
    
    /// <summary>
    /// Called on loading. Setup of the sensors.
    /// </summary>
    /// <remarks>This function is always called before any Start functions and also just after a prefab
    /// is instantiated.
    /// (If a GameObject is inactive during start up Awake is not called until it is made active.)</remarks>
    protected override void Awake()
    {
        // Crucial to call Awake() from the base class to ensure proper initialisation.
        base.Awake();
        
        // Get the configuration from the the configMgmt and set the requested sensor count from CLI arguments.
        m_Config = FindObjectOfType<ConfigurationMgmt>().config;
        sensorCount = m_Config.sensorCount;
        MaxStep = m_Config.maxStep;
        
        // Set behavior parameters based on selected options for movement and sensor number.
        var brainParameters = GetComponent<BehaviorParameters>().BrainParameters;
        
        // Set the observation size to the requested horizontal sensor count + 2 sensors up and down.
        var verticalSensors = allowYMovement ? 2 : 0;
        brainParameters.VectorObservationSize = verticalSensors + sensorCount + 4;
        
        // Set action branch size based on allowed movement.
        brainParameters.ActionSpec.BranchSizes[0] = allowYMovement ? 9 : 7;
    }

    private CustomStatsManager m_StatsManager;
    // Is called before the first frame update
    public void Start() {
        m_RBody = GetComponent<Rigidbody>();
        m_SensorDirections = GetSensorDirections();
        m_StatsManager = FindObjectOfType<CustomStatsManager>();
    }
    
    /// <summary>
    /// Based on the requested number of horizontal sensors setup the directions of the rays in reference to the agent.
    /// </summary>
    private List<Vector3> GetSensorDirections()
    {
        var directions = new List<Vector3>();
        directions.Add(Vector3.forward);

        var angle = 360f / sensorCount;
        
        for (int i = 1; i < sensorCount; i++)
        {
            var vector = Quaternion.Euler(0, - angle * i, 0) * Vector3.forward;
            directions.Add(vector);
        }

        return directions;
    }

    enum AgentPos
    {
        /// <summary>
        /// Fixed agent position.
        /// </summary>
        Fixed,
        /// <summary>
        /// Position of the agent is only slightly varied.
        /// </summary>
        Varied,
        /// <summary>
        /// Fully random pos of the agent. Including in different room.
        /// </summary>
        FullyRandom
    }
    /// <summary>
    /// Reset the position of the agent within the training area.
    /// </summary>
    private AgentPos m_AgentPosType = AgentPos.FullyRandom;
    private void ResetAgentPosition()
    {
        m_RBody.angularVelocity = Vector3.zero;
        m_RBody.velocity = Vector3.zero;
        transform.rotation = Quaternion.identity;
        
        if (m_AgentPosType is AgentPos.Varied)
        {
            var x = Random.Range(-3f, 3f);
            var z = Random.Range(2f, 4f);
            transform.localPosition = new Vector3(x, 1f, z);
        }
        else if (m_AgentPosType is AgentPos.Fixed)
        {
            transform.localPosition = new Vector3(0, 1f, 3f);
        }
        else if (m_AgentPosType is AgentPos.FullyRandom)
        {
            // Get and set random position.
            transform.position = floor.GetRandomAgentPosition();
            
            // Set random rotation.
            transform.eulerAngles = new Vector3(0f, Random.Range(0f, 360f), 0f);
        }
    }
    
    /// <summary>
    /// Fix implausible agent rotation. Maintains Y rotation but sets x and z to 0.
    /// </summary>
    private void FixAgentRotation()
    {
        var currentRotationY = transform.eulerAngles.y;
        transform.eulerAngles = new Vector3(0f, currentRotationY, 0f);
    }

    private EpEndReasons m_EndReason = EpEndReasons.None;
    /// <summary>
    /// Called on start of the episode.
    /// </summary>
    public override void OnEpisodeBegin()
    {
        // Reset variables on ep begin.
        m_DoorPassages = 0;
        actionCount = 0;
        m_GuiText = "";
        totalReward = 0f;
        
        // Creates a wall or not if so desired within the floor parameters.
        floor.CreateInnerWall();
        
        // Gizmo: Reset last collision position. Used for visual reference only when showing Gizmos.
        m_LastCollision = Vector3.zero;
        
        // Reset the position of the agent if target was not reached or the position is not plausible.
        //if (m_EndReason is EpEndReasons.None or EpEndReasons.PositionImplausible or EpEndReasons.TargetReached)
        //{
            ResetAgentPosition();
        //}
        
        // Reset the end reason of the last episode to default.
        m_EndReason = EpEndReasons.None;
        
        // Reset target and decoy position.
        floor.ResetTargetPosition();
        floor.ResetDecoyPosition();
        
        // Get the max possible distance in the training area.
        m_MaxDist = floor.GetMaxPossibleDist();
        
        // Reset the distances at the begin of each episode.
        ResetDist();
        
        // Calculate the distance to the target.
        CalculateDistanceToTarget();
        
        // Increment the global episode counter in the stats.
        m_StatsManager.NewEpisode();
        // Write the current state of the floor to the stats.
        floor.RecordFloorState(m_StatsManager);
    }
    
    /// <summary>
    /// Prepare observations. Get sensor data to be used.
    /// </summary>
    private void PrepareObservations()
    {
        m_RayDistances.Clear(); // Removed old measurements.
        
        // Get the remaining distance measurements as requested through the editor.
        foreach (var dir in m_SensorDirections)
        {
            m_RayDistances.Add(PerformRaycastGetDistance(dir));
        }
        
        // Get up and down distance data if movement along y axis is allowed.
        if (allowYMovement)
        {
            m_RayDistances.Add(PerformRaycastGetDistance(Vector3.up));
            m_RayDistances.Add(PerformRaycastGetDistance(Vector3.down));
        }
    }
    
    /// <summary>
    /// Cast a ray in a given direction and return the normalised distance.
    /// </summary>
    /// <param name="dir">Direction of the cast ray.</param>
    /// <returns>Distance of ray to first hit.</returns>
    private float PerformRaycastGetDistance(Vector3 dir)
    {
        var dirTransform = transform.TransformDirection(dir);
        var currentRay = new Ray(transform.position, dirTransform);
        RaycastHit currentHit;
        Physics.Raycast(currentRay, out currentHit, maxDistance: floor.GetMaxPossibleDist());
        var len = currentHit.distance;
        
        #if UNITY_EDITOR
        // Draw gizmo lines to help with debugging.
        if (dir == Vector3.forward)
        {
            Debug.DrawRay(transform.position, dirTransform * len, Color.red);
        }
        else
        {
            Debug.DrawRay(transform.position, dirTransform * len, Color.gray);
        }
        #endif

        return currentHit.distance / floor.GetMaxPossibleDist();
    }
    
    /// <summary>
    /// Code of the agent to get the sensor data.
    /// </summary>
    public override void CollectObservations(VectorSensor sensor)
    {
        // Idea: Ensure updated sensor data when setting observations. 
        PrepareObservations();

        foreach (var dist in m_RayDistances)
        {
            sensor.AddObservation(dist);
        }
        
        // Agent velocity.
        sensor.AddObservation(m_RBody.velocity);

        // Agent rotation. Y axis only.
        Quaternion rotation = m_RBody.rotation;
        sensor.AddObservation(rotation.eulerAngles.y / 360.0f);  // [0,1]
        
    }

    /// <summary>
    /// Check if agent position is plausible. Used to detect positions outside of training area.
    /// </summary>
    /// <returns>True if position is good.</returns>
    private bool IsAgentPositionPlausible()
    {
        // Fell off platform or is beyond roof.
        var y = transform.localPosition.y;
        if (y is < 0f or > 2f)
        {
            RecordData(RecorderCodes.OutOfBounds);
            return false;
        }

        if (floor.IsOutsideFloor(transform.position))
        {
            RecordData(RecorderCodes.OutOfBounds);
            return false;
        }

        return true;
    }
    
    /// <summary>
    /// Check if the agent rotation is plausible. 
    /// </summary>
    /// <returns></returns>
    private bool IsAgentRotationPlausible()
    {
        // Check rotation.
        var rotation = transform.rotation;
        var allowedValues = new List<float>(){ 0f, -0f };
        if (!Mathf.Approximately(rotation.x, 0f) || !Mathf.Approximately(rotation.z, 0f))
        {
            RecordData(RecorderCodes.RotationError);
            Debug.Log("Rotation error.");
            return false;
        }
        return true;
    }
    
    /// <summary>
    /// Perform the movement of the agent.
    /// </summary>
    /// <param name="actionBuffers"></param>
    private void MoveAgent(ActionBuffers actionBuffers)
    {
        actionCount += 1;
        Vector3 controlSignal = Vector3.zero;
        var rotate = 0f;
        
        if (m_Actions == ActionsPerStep.Single)
        {
            var actions = actionBuffers.DiscreteActions[0];
            switch (actions)
            {
                case 1:
                    controlSignal.x = 1f;
                    break;
                case 2:
                    controlSignal.x = -1f;
                    break;
                case 3:
                    controlSignal.z = 1f;
                    break;
                case 4:
                    controlSignal.z = -1f;
                    break;
                case 5:
                    rotate = 1f;
                    break;
                case 6:
                    rotate = -1f;
                    break;
                case 7 when allowYMovement:
                    controlSignal.y = 1f;
                    break;
                case 8 when allowYMovement:
                    controlSignal.y = -1f;
                    break;
            }
        }
        else if (m_Actions == ActionsPerStep.Multiple)
        {
            var rightLeft = actionBuffers.DiscreteActions[0];
            var upDown = actionBuffers.DiscreteActions[1];
            var forwardBackwards = actionBuffers.DiscreteActions[2];
            var r = actionBuffers.DiscreteActions[3];
            
            // Check if any movement is requested.
            var all = new List<int>(){rightLeft, upDown, forwardBackwards, r};
            /*if (!(all.Contains(2) || all.Contains(3)))
            {
                return;
            } */

            switch (rightLeft)
            {
                case 1:
                    controlSignal.x = 1f;
                    break;
                case 2:
                    controlSignal.x = -1f;
                    break;
            }
        
            switch (upDown)
            {
                case 1:
                    controlSignal.y = 1f;
                    break;
                case 2:
                    controlSignal.y = -1f;
                    break;
            }
        
            switch (forwardBackwards)
            {
                case 1:
                    controlSignal.z = 1f;
                    break;
                case 2:
                    controlSignal.z = -1f;
                    break;
            }

            rotate = 0f;
            switch (r)
            {
                case 1:
                    rotate = 1f;
                    break;
                case 2:
                    rotate = -1f;
                    break;
            }
        }

        // Rotate the agent.
        //var turnSpeed = 200;
        //var rotateDir = transform.up * rotate;
        //transform.Rotate(rotateDir, Time.fixedDeltaTime * turnSpeed);
        
        var eulerAngleVelocity = new Vector3(0, 100 * rotate, 0);
        Quaternion deltaRotation = Quaternion.Euler(eulerAngleVelocity * Time.fixedDeltaTime);
        m_RBody.MoveRotation(m_RBody.rotation * deltaRotation);

        // Move the agent.
        var direction = m_RBody.rotation * controlSignal;
        //m_RBody.Move(m_RBody.position + direction * (Time.deltaTime * forceMultiplier));
        //m_RBody.MovePosition();
        m_RBody.AddForce(direction * forceMultiplier, ForceMode.Force);
    }
    
    /*void FixedUpdate()
    {
        // Get the distance to the target.
        CalculateDistanceToTarget();
        
        // Reached target. Success. Terminate episode.
        if (m_DistToTarget < 1.42f)
        {
            RecordData(RecorderCodes.Target);
            m_EndReason = EpEndReasons.TargetReached;
            
            AddReward(1f);
            

            totalReward += 1f;
            EndEpisode();
        }
    }*/
    
    /// <summary>
    /// Called on action input.
    /// </summary>
    public int actionCount = 0;
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Initial record base data. To ensure training data has all the same dimensions.
        RecordBaseData();
        
        // Perform the movement of the agent.
        MoveAgent(actionBuffers);
        
        // Get the distance to the target.
        CalculateDistanceToTarget();
        
        // Reached target. Success. Terminate episode.
        if (m_DistToTarget < 1.42f)
        {
            RecordData(RecorderCodes.Target);
            m_EndReason = EpEndReasons.TargetReached;
            
            AddReward(1f);
            

            totalReward += 1f;
            EndEpisode();
        }
        
        // Verify agent state (position) is plausible. Terminate episode if agent is beyond limits of the area.
        if (!IsAgentPositionPlausible())
        {
            m_EndReason = EpEndReasons.PositionImplausible;
            m_LastImplausiblePos = transform.position;
            EndEpisode();
        }
    
        // Fix the rotation of the agent. Does not require the termination of the episode.
        if (!IsAgentRotationPlausible())
        {
            FixAgentRotation();
        }
        
        AddReward(GetReward());
    }
    
    /// <summary>
    /// Reasons to the episode.
    /// </summary>
    enum EpEndReasons
    {   
        /// <summary>
        /// Default reason. Reason remains set if the maxstep limit is hit by the agent.
        /// </summary>
        None,
        /// <summary>
        /// The target was found.
        /// </summary>
        TargetReached,
        /// <summary>
        /// Position of the agent not plausible. Position outside of the training area.
        /// </summary>
        PositionImplausible
    }
    
    
    public float currentReward = 0f;
    public float totalReward;
    /// <summary>
    /// Calculate and return reward based on current distance to target.
    /// </summary>
    private float GetReward()
    {
        /*var dist = m_DistToTargetNormal;
        if (float.IsPositiveInfinity(dist))
        {
            dist = 1f;
        }
        currentReward = -0.01f * dist;*/
        /*var scalar = -1f;
        if (m_LastDistToTarget > m_DistToTarget)
        {
            scalar = 1f;
        }

        currentReward = scalar * (1f - m_DistToTargetNormal);*/
        //currentReward = -1f / MaxStep;
        currentReward = m_Config.stepPenalty;
        totalReward += currentReward;
        return currentReward;
    }

    /// <summary>
    /// Get the ID of the room the agent is currently in.
    /// </summary>
    /// <returns>Room id based on the provided position.</returns>
    private int GetCurrentRoomId()
    {
        return floor.GetAgentRoomId(transform.position);
    }
    
    /// <summary>
    /// Update the current room of the agent on entering the door trigger.
    /// </summary>
    private int m_DoorPassageStartInRoom;
    private void OnTriggerEnter(Collider other)
    {
        m_DoorPassageStartInRoom = GetCurrentRoomId();
    }
    
    /// <summary>
    /// Called on trigger exit after collider contact finishes with the door.
    /// </summary>
    /// <remarks>Takes into account if agent and target are placed in the same room.
    /// Important change to the reward function due to direction change of what is a good door passage.</remarks>
    private void OnTriggerExit(Collider other)
    {
        
        // Make sure a proper door passage occured.
        if (m_DoorPassageStartInRoom != GetCurrentRoomId())
        {
            // Door passage can only be rewarded if target and agent are NOT in the same room.
            if (!floor.RoomsInEnv.AreAgentAndTargetInSameRoom())
            {
                if (m_DoorPassages % 2 == 0)
                {
                    AddReward(0.05f);
                    totalReward += 0.05f;
                    m_GuiText = "Not same room: Door passed +0.5f";
                    RecordData(RecorderCodes.GoodDoorPassage);
                }
                else
                {
                    AddReward(-0.1f);
                    totalReward += -0.1f;
                    m_GuiText = "Not same room: Door passed -0.6f";
                    RecordData(RecorderCodes.BadDoorPassage, recordValue:-1f);
                }
            }
            else // Agent and target start in the same room.
            {
                // Door passage now in the wrong direction. Away from the target. Both started in the same room.
                if (m_DoorPassages % 2 == 0)
                {
                    AddReward(-0.1f);
                    totalReward += -0.1f;
                    m_GuiText = "Same room: Door passed -0.6f";
                    RecordData(RecorderCodes.BadDoorPassage, recordValue:-1f);
                }
                else // Now door passage back to the target room. Reward must be less to inhibit circular movement through the door.
                {
                    AddReward(0.05f);
                    totalReward += 0.05f;
                    m_GuiText = "Same room: Door passed +0.5f";
                    RecordData(RecorderCodes.GoodDoorPassage);
                }
            }

            m_DoorPassages++;
        }
        
    }
    
    /// <summary>
    /// Initial collision event.
    /// </summary>
    private void OnCollisionEnter(Collision other)
    {
        // Collisions with target can be disregarded.
        if (!other.gameObject.CompareTag(floor.target.tag))
        {
            m_LastCollision = transform.position; // Used for Gizmos.
            AddReward(-0.1f);
            RecordData(RecorderCodes.TotalCollision);
            RecordData(RecorderCodes.InitialCollision);
        }
    }
    
    
    /// <summary>
    /// If the collision continues after the initial event reduced punishment.
    /// </summary>
    private void OnCollisionStay(Collision other)
    {
        // Collisions with target can be disregarded.
        if (!other.gameObject.CompareTag(floor.target.tag))
        {
            m_LastCollision = transform.position;
            AddReward(-0.05f);
            RecordData(RecorderCodes.TotalCollision);
            RecordData(RecorderCodes.StayCollision);
        }
    }

    /// <summary>
    /// Codes used for tensorboard stats recorder.
    /// </summary>
    private enum RecorderCodes
    {
        TotalCollision,
        MaxSteps,
        Implausible,
        Target,
        RotationError,
        OutOfBounds,
        /// <summary>
        /// Door passage in correct direction towards the target.
        /// </summary>
        GoodDoorPassage,
        /// <summary>
        /// Door passage away from the target. Passage in the wrong direction.
        /// </summary>
        BadDoorPassage,
        /// Collision codes.
        InitialCollision,
        StayCollision
    }
    
    /// <summary>
    /// Record data for tensorboard statistics.
    /// </summary>
    /// <remarks>Write data based on selected Recorder codes within the code.</remarks>
    /// <param name="msg">Recorder code</param>
    private void RecordData(RecorderCodes msg, float recordValue = 1f)
    {
        var statsRecorder = Academy.Instance.StatsRecorder;
        switch (msg)
        {
            case RecorderCodes.StayCollision:
                statsRecorder.Add("Collision/Stay", recordValue, StatAggregationMethod.Sum);
                break;
            
            case RecorderCodes.InitialCollision:
                statsRecorder.Add("Collision/Initial", recordValue, StatAggregationMethod.Sum);
                break;
                
            case RecorderCodes.TotalCollision:
                statsRecorder.Add("Wall hit", recordValue, StatAggregationMethod.Sum);
                statsRecorder.Add("Collision/Total", recordValue, StatAggregationMethod.Sum);
                break;
            
            case RecorderCodes.MaxSteps:
                statsRecorder.Add("Max Steps reached", recordValue);
                break;

            case RecorderCodes.Target:
                statsRecorder.Add("Target Reached", recordValue, StatAggregationMethod.Sum);
                break;
            
            case RecorderCodes.RotationError:
                statsRecorder.Add("Rotation Error", recordValue);
                break;
            
            case RecorderCodes.OutOfBounds:
                statsRecorder.Add("Out of bounds", recordValue);
                break;
            
            case RecorderCodes.GoodDoorPassage:
                statsRecorder.Add("Door/Passage", recordValue, StatAggregationMethod.Sum);
                statsRecorder.Add("Door/Good passage", recordValue, StatAggregationMethod.Sum);
                break;
            
            case RecorderCodes.BadDoorPassage:
                statsRecorder.Add("Door/Passage", recordValue, StatAggregationMethod.Sum);
                statsRecorder.Add("Door/Bad passage", recordValue, StatAggregationMethod.Sum);
                break;
        }
    }
    
    /// <summary>
    /// Record dummy data to ensure dimensions of the training data is all the same.
    /// </summary>
    private void RecordBaseData()
    {   
        RecordData(RecorderCodes.BadDoorPassage, 0f);
        RecordData(RecorderCodes.GoodDoorPassage, 0f);
        RecordData(RecorderCodes.Target, 0f);
        RecordData(RecorderCodes.TotalCollision, 0f);
        RecordData(RecorderCodes.StayCollision, 0f);
        RecordData(RecorderCodes.InitialCollision, 0f);
    }

    /// <summary>
    /// Calculate the distance from the agent to the target. Taking doors into account.
    /// </summary>
    /// <remarks>Distance is basis for reward function.</remarks>
    private void CalculateDistanceToTarget()
    {
        m_LastDistToTarget = m_DistToTarget;

        // Get the path from agent to target.
        GetPath();
        
        // Calculate the distance of the path.
        m_DistToTarget = 0f;
        for (int i = 1; i < m_Path.Count; i++)
        {
            m_DistToTarget += Vector3.Distance(m_Path[i - 1], m_Path[i]);
        }
        
        // Calculate normalised distance to target.
        m_LastDistToTargetNormal = m_DistToTargetNormal;
        m_DistToTargetNormal = m_DistToTarget / m_MaxDist;
        
        // If first call during episode. Set the last and current distance to target equal.
        if (float.IsPositiveInfinity(m_LastDistToTarget))
        {
            m_LastDistToTarget = m_DistToTarget;
        }

        if (float.IsPositiveInfinity(m_LastDistToTargetNormal))
        {
            m_LastDistToTargetNormal = m_DistToTargetNormal;
        }
        
    }
    
    /// <summary>
    /// Reset the distances to default values. Called at the beginning of the episode.
    /// </summary>
    private void ResetDist()
    {
        m_LastDistToTarget = float.PositiveInfinity;
        m_LastDistToTargetNormal = float.PositiveInfinity;
        
        m_DistToTarget = float.PositiveInfinity;
    }
    
    /// <summary>
    /// Get the path from the agent to the target.
    /// </summary>
    private readonly List<Vector3> m_Path = new List<Vector3>();
    private void GetPath()
    {
        m_Path.Clear();
        
        var agentPosition = transform.position;
        
        var targetPosition = floor.target.transform.position;
        
        // First point of the path is the agent position.
        m_Path.Add(agentPosition);

        if (m_Config.createWall)
        {
            var doorPositions = floor.GetDoorPosition();
            if (agentPosition.z > targetPosition.z)
            {
                if ((agentPosition.z > doorPositions.z) && (doorPositions.z > targetPosition.z))
                {
                    m_Path.Add(doorPositions);
                }
            }
            else
            {
                if ((agentPosition.z < doorPositions.z) && (doorPositions.z < targetPosition.z))
                {
                    m_Path.Add(doorPositions);
                }
            }
        }

        // Last point of the path is the target position.
        m_Path.Add(targetPosition);
    }

#if UNITY_EDITOR
    /// <summary>
    /// Debugging visual cues within in the editor.
    /// </summary>
    void OnDrawGizmos()
    {
        // Draw the path from target to the target. Takes the door into account.
        if (m_Path != null)
        {
            for (int i = 1; i < m_Path.Count; i++)
            {
                Gizmos.DrawLine(m_Path[i-1], m_Path[i]);
            }

            foreach (var coord in m_Path) 
            {
                Gizmos.DrawCube(coord, new Vector3(0.3f, 0.3f, 0.3f));    
            }
        }
        
        // Highlight last detected collision between agent and other object.
        if (m_LastCollision != Vector3.zero)
        {
            Gizmos.DrawCube(m_LastCollision, new Vector3(0.3f, 0.3f, 0.3f));
        }
        
        // Highlight implausible positions. Especially after glitches through walls.
        if (m_LastImplausiblePos != Vector3.zero)
        {
            Debug.DrawRay(m_LastImplausiblePos, Vector3.up * 100f, Color.red);
        }
    }
#endif
    
    enum ActionsPerStep
    {
        /// <summary>
        /// Allow only one type of action per step. Example: During forward motion no rotation is possible.
        /// </summary>
        Single,
        /// <summary>
        /// Allow multiple types of action per step. Example: During forward motion rotation is also possible.
        /// </summary>
        Multiple
    }
    
    /// <summary>
    /// Select how many action types are possible during each step.
    /// </summary>
    private ActionsPerStep m_Actions = ActionsPerStep.Single;
    
    /// <summary>
    /// Heuristic action handling in the editor.
    /// </summary>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        if (m_Actions == ActionsPerStep.Multiple)
        {
            var x = discreteActionsOut[0];
            var y = discreteActionsOut[1];
            var z = discreteActionsOut[2];
            var rotation = discreteActionsOut[3];

            // X. Right, left and no movement.
            if (Input.GetKey(KeyCode.D))
            {
                discreteActionsOut[0] = 1;
            }
            else if (Input.GetKey(KeyCode.A))
            {
                discreteActionsOut[0] = 2;
            }
            else
            {
                discreteActionsOut[0] = 0;
            }
            // Y. Up, down and no movement.
            if (Input.GetKey(KeyCode.X))
            {
                discreteActionsOut[1] = 1;
            }
            else if (Input.GetKey(KeyCode.Y))
            {
                discreteActionsOut[1] = 2;
            }
            else
            {
                discreteActionsOut[1] = 0;
            }
            // Z. Forward, backwards and no movement.
            if (Input.GetKey(KeyCode.W))
            {
                discreteActionsOut[2] = 1;
            }
            else if (Input.GetKey(KeyCode.S))
            {
                discreteActionsOut[2] = 2;
            }
            else
            {
                discreteActionsOut[2] = 0;
            }
            // Rotation. Right, left and no rotation.
            if (Input.GetKey(KeyCode.E))
            {
                discreteActionsOut[3] = 1;
            }
            else if (Input.GetKey(KeyCode.Q))
            {
                discreteActionsOut[3] = 2;
            }
            else
            {
                discreteActionsOut[3] = 0;
            }
        }

        else if (m_Actions == ActionsPerStep.Single)
        {
            if (Input.GetKey(KeyCode.D)) // Move right.
            {
                discreteActionsOut[0] = 1;
            }
            else if (Input.GetKey(KeyCode.A)) // Move left.
            {
                discreteActionsOut[0] = 2;
            }
            
            // Z. Forward, backwards and no movement.
            else if (Input.GetKey(KeyCode.W))
            {
                discreteActionsOut[0] = 3;
            }
            else if (Input.GetKey(KeyCode.S))
            {
                discreteActionsOut[0] = 4;
            }

            // Rotation. Right, left.
            else if (Input.GetKey(KeyCode.E))
            {
                discreteActionsOut[0] = 5;
            }
            else if (Input.GetKey(KeyCode.Q))
            {
                discreteActionsOut[0] = 6;
            }
            
            // Y. Up, down.
            else if (allowYMovement && Input.GetKey(KeyCode.X)) {
                discreteActionsOut[0] = 7;
            }
            else if (allowYMovement && Input.GetKey(KeyCode.Y))
            {
                discreteActionsOut[0] = 8;
            }
            
            // Default: No movement.
            else
            {
                discreteActionsOut[0] = 0;
            }
        }
    }
}
