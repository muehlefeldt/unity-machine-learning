using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Unity.Mathematics;
//using DefaultNamespace;
using UnityEngine;

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
    //public Target target;
    public Floor floor;
    public float m_MaxDist;
    
    // Distances.
    public float m_DistToTarget;
    public float m_DistToTargetNormal;
    private float m_LastDistToTarget;
    
    // Set how much force is applied to the rigidbody.
    public float forceMultiplier = 10f;
    
    // Select sensor count of the agent. Has no influence on sensors along y axis, i.e. height sensors remain constant.
    public int sensorCount = 4;
    
    /// <summary>
    /// Select reward function. 
    /// </summary>
    public RewardFunction rewardFunctionSelect = RewardFunction.Basic;
    
    private Vector3 m_LastCollision = Vector3.zero;

    // Is called before the first frame update
    //public override void Initialize() {
    public void Start() {
        m_RBody = GetComponent<Rigidbody>();
        m_SensorDirections = GetSensorDirections();
        
        // Set the observation size to the requested sensor count + 2 sensors up and down.
        GetComponent<BehaviorParameters>().BrainParameters.VectorObservationSize = 2 + sensorCount + 4;
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
            transform.position = floor.GetRandomAgentPosition();
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
        
        // If the Agent fell, zero its momentum
        // if (transform.localPosition.y < 0 || m_CollisionDetected || m_ImplausiblePosition)
        // if (transform.localPosition.y < 0 || m_ImplausiblePosition)
        // {
        //     //m_CollisionDetected = false;
        //     m_ImplausiblePosition = false;
        //     //m_TargetReached = false;
        //     ResetAgentPosition();
        // }

        // Move the target to a new spot
        // if (floor.targetFixedPosition)
        // {
        //     ResetAgentPosition();
        // }
        
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
        m_RayDistances.Add(PerformRaycastGetDistance(Vector3.up));
        m_RayDistances.Add(PerformRaycastGetDistance(Vector3.down));
        
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
        
        // Draw gizmo lines to help with debugging.
        if (dir == Vector3.forward)
        {
            Debug.DrawRay(transform.position, dirTransform * len, Color.red);
        }
        else
        {
            Debug.DrawRay(transform.position, dirTransform * len, Color.gray);
        }
        

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
                case 3:
                    controlSignal.y = 1f;
                    break;
                case 4:
                    controlSignal.y = -1f;
                    break;
                case 5:
                    controlSignal.z = 1f;
                    break;
                case 6:
                    controlSignal.z = -1f;
                    break;
                case 7:
                    rotate = 1f;
                    break;
                case 8:
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

        // Reached target. Success. Terminate episode.
        if (m_DistToTarget < 1.42f)
        {
            RecordData(RecorderCodes.Target);
            m_EndReason = EpEndReasons.TargetReached;
            SetReward(1f);
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
    /// Types of reward function available.
    /// </summary>
    public enum RewardFunction
    {
        /// <summary>
        /// Most basic reward function.
        /// </summary>
        Basic,
        /// <summary>
        /// Based on the basic reward function. But with changed values.
        /// </summary>
        SimpleDist,
        /// <summary>
        /// Complex reward function based on distance. Based on the Matignon et al. paper.
        /// </summary>
        ComplexDist,
        /// <summary>
        /// Reward contact with correct target.
        /// </summary>
        CollisionCheckpoint,
        /// <summary>
        /// Sparse reward. Punish each step and reward target found. Punish all other contacts.
        /// </summary>
        Sparse,
        /// <summary>
        /// Experiment reward function. Used to test different approaches.
        /// Combines now: Collision penalty. Reduced penalty for door collisions.
        /// Every step receives distance based reward based on Matignon et al.
        /// </summary>
        Experiment
        
    }

    /// <summary>
    /// Calculate and return reward based on current distance to target.
    /// </summary>
    public float currentReward = 0f;
    private float GetReward()
    {
        if (rewardFunctionSelect == RewardFunction.Basic)
        {
            var reward = 0f;
            if (m_LastDistToTarget > m_DistToTarget)
            {
                reward = 0.1f;
            }
            //if (m_LastDistToTarget > m_DistToTarget) reward = 0.1f;
            else reward = -0.15f;
            currentReward = reward + (-1f / MaxStep);
            return currentReward;
        }
        if (rewardFunctionSelect == RewardFunction.SimpleDist)
        {
            var reward = 0f;
            if (m_LastDistToTarget > m_DistToTarget)
            {
                reward = 0.01f;
            }
            //else if (action > 6) reward = 0.01f;
            
            else reward = -0.02f;

            return reward; //+ (-1f / MaxStep);
        }

        if (rewardFunctionSelect == RewardFunction.ComplexDist)
        {
            var beta = 1f;
            var omega = 0.3f;
            var x = m_DistToTargetNormal;
            currentReward = beta * math.exp(-1 * (math.pow(x, 2) / (2 * math.pow(omega, 2))));
            return currentReward;
        }

        if (rewardFunctionSelect == RewardFunction.CollisionCheckpoint)
        {
            return -1f / MaxStep;
        }
        
        if (rewardFunctionSelect == RewardFunction.Sparse)
        {
            return -1f / MaxStep;
        }

        if (rewardFunctionSelect == RewardFunction.Experiment)
        {
            var beta = 0.4f;
            var omega = 0.4f;
            var x = m_DistToTargetNormal;
            currentReward = beta * math.exp(-1 * (math.pow(x, 2) / (2 * math.pow(omega, 2))));
            return currentReward;
        }
        
        return 0f;
    }
    
    
    private int m_DoorPassages;
    /// <summary>
    /// Trigger is given by contact of the agent with the door.
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
        if (rewardFunctionSelect is RewardFunction.CollisionCheckpoint or RewardFunction.Sparse or RewardFunction.Experiment)
        {
            if (m_DoorPassages < 10)
            {
                if (m_DoorPassages % 2 == 0)
                {
                    AddReward(0.5f);
                    Debug.Log("Door passed +0.5f");
                }
                else
                {
                    AddReward(-0.5f);
                    Debug.Log("Door passed -0.5f");
                }
                m_DoorPassages++;
            }
            else
            {
                AddReward(-1f);
                Debug.Log("Door passed -1f");
            }
        }
    }

    /// <summary>
    /// Initial collision event.
    /// </summary>
    private void OnCollisionEnter(Collision other)
    {
        m_LastCollision = transform.position; // Used for Gizmos.
        
        if (rewardFunctionSelect is RewardFunction.CollisionCheckpoint)
        {
            if (other.gameObject.CompareTag("innerWall"))
            {
                AddReward(-0.1f);
            }

            else if (other.gameObject.CompareTag("outerWalls"))
            {
                AddReward(-0.5f);
            }

            else if (other.gameObject.CompareTag("decoys"))
            {
                AddReward(-1f);
                Debug.Log("Decoy -1");
            }
            
        }
        else if (rewardFunctionSelect is RewardFunction.Sparse or RewardFunction.Experiment)
        {
            // On collision with door give separate reward.
            if (other.gameObject.CompareTag("door"))
            {
                AddReward(-0.2f);
            }
            // All other collisions: Ceiling, floor, walls.
            else
            {
                AddReward(-0.5f);
            }
        }
        else if (rewardFunctionSelect == RewardFunction.SimpleDist)
        {
            AddReward(-0.5f);
        }
        else
        {
            AddReward(-0.8f);
        }
        RecordData(RecorderCodes.Wall);
        
    }
    
    /// <summary>
    /// If the collision continues after the initial event reduced punishment.
    /// </summary>
    private void OnCollisionStay(Collision other)
    {
        if (rewardFunctionSelect == RewardFunction.CollisionCheckpoint)
        {
            if (other.gameObject.CompareTag("innerWall"))
            {
                AddReward(-0.05f);
            }

            if (other.gameObject.CompareTag("outerWalls"))
            {
                AddReward(-0.2f);
            }

            if (other.gameObject.CompareTag("decoys"))
            {
                AddReward(-0.8f);
            }
            
        }
        else if (rewardFunctionSelect is RewardFunction.Sparse or RewardFunction.Experiment)
        {
            if (other.gameObject.CompareTag("door"))
            {
                AddReward(-0.05f);
            }
            else
            {
                AddReward(-0.1f);
            }
        }
        else if (rewardFunctionSelect == RewardFunction.SimpleDist)
        {
            AddReward(-0.1f);
        }
        else
        {
            AddReward(-0.5f);
        }
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
        OutOfBounds
    }
    
    /// <summary>
    /// Record data for tensorboard statistics.
    /// </summary>
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
        m_DistToTargetNormal = m_DistToTarget / m_MaxDist;
        
        // If first call during episode. Set the last and current distance to target equal.
        if (float.IsPositiveInfinity(m_LastDistToTarget))
        {
            m_LastDistToTarget = m_DistToTarget;
        }
    }
    
    /// <summary>
    /// Reset the distances to default values. Called at the beginning of the episode.
    /// </summary>
    private void ResetDist()
    {
        m_LastDistToTarget = float.PositiveInfinity;
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
    
    void OnDrawGizmos()
    {
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

        if (m_LastCollision != Vector3.zero)
        {
            Gizmos.DrawCube(m_LastCollision, new Vector3(0.3f, 0.3f, 0.3f));
        }
        if (m_LastImplausiblePos != Vector3.zero)
        {
            //Gizmos.DrawCube(m_LastImplausiblePos, new Vector3(0.3f, 0.3f, 0.3f));
            Debug.DrawRay(m_LastImplausiblePos, Vector3.up * 100f, Color.red);
        }

        if (rewardFunctionSelect == RewardFunction.ComplexDist)
        {
            Gizmos.color = Color.black;
            //Gizmos.DrawIcon();
        }
    }

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
            else if (Input.GetKey(KeyCode.X))
            {
                discreteActionsOut[0] = 3;
            }
            else if (Input.GetKey(KeyCode.Y))
            {
                discreteActionsOut[0] = 4;
            }
            // Z. Forward, backwards and no movement.
            else if (Input.GetKey(KeyCode.W))
            {
                discreteActionsOut[0] = 5;
            }
            else if (Input.GetKey(KeyCode.S))
            {
                discreteActionsOut[0] = 6;
            }

            // Rotation. Right, left and no rotation.
            else if (Input.GetKey(KeyCode.E))
            {
                discreteActionsOut[0] = 7;
            }
            else if (Input.GetKey(KeyCode.Q))
            {
                discreteActionsOut[0] = 8;
            }
            else
            {
                discreteActionsOut[0] = 0;
            }
        }
    }
}
