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


public class RollerAgent : Agent
{
    private Rigidbody m_RBody;

    private List<Vector3> m_SensorDirections;
    public Target target;
    public Floor floor;
    public float m_MaxDist;
    
    // Distances.
    public float m_DistToTarget;
    public float m_DistToTargetNormal;
    private float m_LastDistToTarget;
    private float m_LastDistToTargetNormal;
    
    // Set how much force is applied to the rigidbody.
    public float forceMultiplier = 10f;
    
    // Select sensor count of the agent. Has no influence on sensors along y axis, i.e. height sensors remain constant.
    public int sensorCount = 4;
    
    // Last detected collision between agent and other object. 
    private Vector3 m_LastCollision = Vector3.zero;
    
    // Number of complete door passages. Reset at every 
    private int m_DoorPassages;

    protected override void Awake()
    {
        // Crucial to call Awake() from the base class to ensure proper initialisation.
        base.Awake();
        
        // Set the observation size to the requested sensor count + 2 sensors up and down.
        GetComponent<BehaviorParameters>().BrainParameters.VectorObservationSize = 2 + sensorCount + 4;
    }
    
    // Is called before the first frame update
    //public override void Initialize() {
    public void Start() {
        m_RBody = GetComponent<Rigidbody>();
        m_SensorDirections = GetSensorDirections();
        
        
    }

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
    /// Reset the position of the agent within the trainingarea.
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
    public override void OnEpisodeBegin()
    {
        m_DoorPassages = 0;
        actionCount = 0;
        floor.Prepare();
        floor.CreateInnerWall();
        
        // Gizmo: Reset last collision position. Used for visual reference only when showing Gizmos.
        m_LastCollision = Vector3.zero;
        
        // Reset the position of the agent if target was not reached or the position is not plausible.
        if (m_EndReason is EpEndReasons.None or EpEndReasons.PositionImplausible or EpEndReasons.TargetReached)
        {
            ResetAgentPosition();
        }
        
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
    }
    
    private readonly List<float> m_RayDistances = new List<float>();
    
    /// <summary>
    /// Prepare observations. Get sensor data to be used.
    /// </summary>
    private void PrepareObservations()
    {
        m_RayDistances.Clear(); // Removed old measurements.
        
        // Get up and down distance data.
        //m_RayDistances.Add(PerformRaycastGetDistance(Vector3.up));
        //m_RayDistances.Add(PerformRaycastGetDistance(Vector3.down));
        
        // Get the remaining distance measurements as requested through the editor.
        foreach (var dir in m_SensorDirections)
        {
            m_RayDistances.Add(PerformRaycastGetDistance(dir));
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
                /*case 3:
                    controlSignal.y = 1f;
                    break;
                case 4:
                    controlSignal.y = -1f;
                    break;*/
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
        var turnSpeed = 200;
        var rotateDir = transform.up * rotate;
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
    
    /// <summary>
    /// Called on action input.
    /// </summary>

    public int actionCount = 0;
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        MoveAgent(actionBuffers);
        
        // Get the distance to the target.
        CalculateDistanceToTarget();

        /*if (floor.RoomsInEnv.CheckForDoorPassage())
        {
            Debug.Log("Door passage");
            if 
        }*/
        
        // Reached target. Success. Terminate episode.
        if (m_DistToTarget < 1.42f)
        {
            RecordData(RecorderCodes.Target);
            m_EndReason = EpEndReasons.TargetReached;
            AddReward(1f);
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

    /*void FixedUpdate()
    {
        // Get the distance to the target.
        CalculateDistanceToTarget();
        
        if (m_DistToTarget < 1.42f)
        {
            RecordData(RecorderCodes.Target);
            m_EndReason = EpEndReasons.TargetReached;
            AddReward(1f);
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
    }*/

    private Vector3 m_LastImplausiblePos = Vector3.zero;

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
    
    /// <summary>
    /// Calculate and return reward based on current distance to target.
    /// </summary>
    public float currentReward = 0f;

    //public float heightPenalty = 0f;
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
        currentReward = -1f / MaxStep;
        return currentReward;
    }

    /// <summary>
    /// Get the ID of the room the agent is currently in.
    /// </summary>
    /// <returns></returns>
    private int GetCurrentRoomId()
    {
        return floor.GetAgentRoomId(transform.position);
    }

    private int m_DoorPassageStartInRoom;
    private void OnTriggerEnter(Collider other)
    {
        m_DoorPassageStartInRoom = GetCurrentRoomId();
    }

    /// <summary>
    /// Called on trigger exit after collider contact finishes with the door.
    /// </summary>
    /// <remarks>Takes into account if agent and target are placed in the same room.
    /// Important change to the reward function</remarks>
    private void OnTriggerExit(Collider other)
    {
        // Make sure a proper door passage occured.
        if (m_DoorPassageStartInRoom != GetCurrentRoomId())
        {
            // Door passage can only be rewarded if target and agent are NOT in the same room.
            if (!floor.RoomsInEnv.AreAgentAndTargetInSameRoom())
            {
                Debug.Log("Trigger Exit.");
                if (m_DoorPassages % 2 == 0)
                {
                    AddReward(0.5f);
                    Debug.Log("Door passed +0.5f");
                    RecordData(RecorderCodes.GoodDoorPassage);
                }
                else
                {
                    AddReward(-0.6f);
                    Debug.Log("Door passed -0.8f");
                    RecordData(RecorderCodes.BadDoorPassage);
                }
            }
            else // Agent and target start in the same room.
            {
                // Door passage now in the wrong direction. Away from the target. Both started in the same room.
                if (m_DoorPassages % 2 == 0)
                {
                    AddReward(-0.6f);
                    RecordData(RecorderCodes.BadDoorPassage);
                }
                else // Now door passage back to the target room. Reward must be less to inhibit circular movement through the door.
                {
                    AddReward(0.5f);
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
        m_LastCollision = transform.position; // Used for Gizmos.
        AddReward(-0.5f);
        RecordData(RecorderCodes.Wall);
    }
    
    
    /// <summary>
    /// If the collision continues after the initial event reduced punishment.
    /// </summary>
    private void OnCollisionStay(Collision other)
    {
        m_LastCollision = transform.position;
        AddReward(-0.3f);
        RecordData(RecorderCodes.Wall);
    }

    /// <summary>
    /// Codes used for tensorboard stats recorder.
    /// </summary>
    private enum RecorderCodes
    {
        Wall,
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
        BadDoorPassage
    }
    
    /// <summary>
    /// Record data for tensorboard statistics.
    /// </summary>
    /// <remarks>Write data based on selected Recorder codes within the code.</remarks>
    /// <param name="msg">Recorder code</param>
    private void RecordData(RecorderCodes msg)
    {
        var statsRecorder = Academy.Instance.StatsRecorder;
        switch (msg)
        {
            case RecorderCodes.Wall:
                statsRecorder.Add("Wall hit", 1f);
                break;
            
            case RecorderCodes.MaxSteps:
                statsRecorder.Add("Max Steps reached", 1f);
                break;

            case RecorderCodes.Target:
                statsRecorder.Add("Target Reached", 1f);
                break;
            
            case RecorderCodes.RotationError:
                statsRecorder.Add("Rotation Error", 1f);
                break;
            
            case RecorderCodes.OutOfBounds:
                statsRecorder.Add("Out of bounds", 1f);
                break;
            
            case RecorderCodes.GoodDoorPassage:
                statsRecorder.Add("Good door passage", 1f);
                break;
            
            case RecorderCodes.BadDoorPassage:
                statsRecorder.Add("Bad door passage", 1f);
                break;
        }
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

        if (floor.CreateWall)
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
            if (Input.GetKey(KeyCode.D))
            {
                discreteActionsOut[0] = 1;
            }
            else if (Input.GetKey(KeyCode.A))
            {
                discreteActionsOut[0] = 2;
            }

            // Y. Up, down and no movement.
            /*else if (Input.GetKey(KeyCode.X))
            {
                discreteActionsOut[0] = 3;
            }
            else if (Input.GetKey(KeyCode.Y))
            {
                discreteActionsOut[0] = 4;
            }*/
            // Z. Forward, backwards and no movement.
            else if (Input.GetKey(KeyCode.W))
            {
                discreteActionsOut[0] = 3;
            }
            else if (Input.GetKey(KeyCode.S))
            {
                discreteActionsOut[0] = 4;
            }

            // Rotation. Right, left and no rotation.
            else if (Input.GetKey(KeyCode.E))
            {
                discreteActionsOut[0] = 5;
            }
            else if (Input.GetKey(KeyCode.Q))
            {
                discreteActionsOut[0] = 6;
            }
            else
            {
                discreteActionsOut[0] = 0;
            }
        }
    }
}
