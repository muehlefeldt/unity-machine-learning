using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
//using DefaultNamespace;
using UnityEngine;

using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
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
    
    // Select sensor count of the agent.
    public int sensorCount = 4;

    // Start is called before the first frame update
    void Awake () {
        m_RBody = GetComponent<Rigidbody>();
        ResetAgentPosition();
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
    
    /// <summary>
    /// Reset the position of the agent to a fixed default position.
    /// </summary>
    private void ResetAgentPosition()
    {
        m_RBody.angularVelocity = Vector3.zero;
        m_RBody.velocity = Vector3.zero;
            
        transform.localPosition = new Vector3( 0, 1f, 3f);
        transform.rotation = Quaternion.identity;
    }
    
    /// <summary>
    /// Fix implausible agent rotation. Maintains Y rotation but sets x and z to 0.
    /// </summary>
    private void FixAgentRotation()
    {
        var currentRotationY = transform.eulerAngles.y;
        transform.eulerAngles = new Vector3(0f, currentRotationY, 0f);
    }
    
    private bool m_TargetReached = false;
    public override void OnEpisodeBegin()
    {
        floor.Prepare();
        floor.CreateInnerWall();
        
        ResetDist();

        // If the Agent fell, zero its momentum
        if (transform.localPosition.y < 0 || m_CollisionDetected || m_ImplausiblePosition)
        {
            m_CollisionDetected = false;
            m_ImplausiblePosition = false;
            m_TargetReached = false;
            ResetAgentPosition();
        }

        // Move the target to a new spot
        if (floor.targetFixedPosition)
        {
            ResetAgentPosition();
        }
        floor.ResetTargetPosition();

        m_MaxDist = floor.GetMaxPossibleDist();
        CalculateDistanceToTarget();
    }
    
    private List<float> m_RayDistances = new List<float>();
    
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
        
        // Draw gizmo lines to help with debugging.
        if (dir == Vector3.forward)
        {
            Debug.DrawRay(transform.position, dirTransform * floor.GetMaxPossibleDist(), Color.red);
        }
        else
        {
            Debug.DrawRay(transform.position, dirTransform * floor.GetMaxPossibleDist(), Color.gray);
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
    
    private bool m_ImplausiblePosition = false;
    public float forceMultiplier = 10f;
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        Vector3 controlSignal = Vector3.zero;
        var actions = actionBuffers.DiscreteActions[0];
        
        var rotate = 0f;
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

        // Rotate the agent.
        var turnSpeed = 200;
        var rotateDir = transform.up * rotate;
        transform.Rotate(rotateDir, Time.fixedDeltaTime * turnSpeed);
        
        // Move the agent.
        var direction = m_RBody.rotation * controlSignal;
        m_RBody.MovePosition(m_RBody.position + direction * (Time.deltaTime * forceMultiplier));
        
        // Get the distance to the target.
        CalculateDistanceToTarget();
        
        // On collision with wall terminate the episode.
        if (m_CollisionDetected)
        {
            RecordData(RecorderCodes.Wall);
            SetReward(-1f);
            //Debug.Log("Collision");
            EndEpisode();
        }
        
        // Reached target. Success.
        if (m_DistToTarget < 1.42f)
        {
            RecordData(RecorderCodes.Target);
            SetReward(1f);
            m_TargetReached = true;
            EndEpisode();
        }
        
        // Verify agent state (position) is plausible. Terminate episode if agent is beyond limits of the area.
        if (!IsAgentPositionPlausible())
        {
            m_ImplausiblePosition = true;
            EndEpisode();
        }
    
        // Fix the rotation of the agent. Does not warant the termination of the episode.
        if (!IsAgentRotationPlausible())
        {
            //m_ImplausiblePosition = true;
            FixAgentRotation();
            //EndEpisode();
        }

        //if (currentStep > maxSteps)
        //{
        //    RecordData(RecorderCodes.MaxSteps);
        //    SetReward(-1f);
        //    EndEpisode();
        //}
        
        //CalculateReward();
        SetReward(-1f / MaxStep);
    }
    
    /// <summary>
    /// Calculate and return reward based on current distance to target.
    /// </summary>
    /// <remarks>
    /// ToDo: Wenn du die Entfernung zum Ziel nicht verringerst, dann gibt es eine Bestrafung.
    /// </remarks>
    public float reward = 0f;
    private void CalculateReward()
    {
        
        var beta = 0.35f;
        var omega = 0.3f;
        var fGui = Mathf.Exp(-1f * Mathf.Pow(m_DistToTargetNormal, 2f) / (2 * Mathf.Pow(omega, 2) ));
        
        GetPath();
        
        var angleToTarget = Vector3.Angle(transform.forward, m_Path[1]-transform.position) / 180f;
        print(angleToTarget);
        var fAng = Mathf.Exp(-1f * Mathf.Pow(angleToTarget, 2f)/ (2 * Mathf.Pow(omega, 2) ));
        
        //var dirTransform = transform.TransformDirection(transform.forward);
        //var currentRay = new Ray(transform.position, dirTransform);

        //var stepPunishment = currentStep / maxSteps;
        //reward = ((1f - beta) * fGui + beta * fAng) * (1f - stepPunishment);

    }
    
    /// <summary>
    /// Collision handling. Reset of the agent position if agent collides with other game objects.
    /// </summary>
    /// <param name="other"></param>
    private bool m_CollisionDetected = false;
    private void OnTriggerEnter(Collider other)
    {
        m_CollisionDetected = true;
    }
    
    /// <summary>
    /// Codes used for tensorboard stats recorder.
    /// </summary>
    private enum RecorderCodes
    {
        None = 0,
        Wall = 1,
        MaxSteps = 2,
        Implausible = 3,
        Target = 4,
        RotationError = 5,
        OutOfBounds = 6
    }
    
    /// <summary>
    /// Record data for tensorboard.
    /// </summary>
    /// <param name="msg">Recorder code</param>
    private void RecordData(RecorderCodes msg)
    {
        var statsRecorder = Academy.Instance.StatsRecorder;
        switch (msg)
        {
            case RecorderCodes.Wall:
                statsRecorder.Add("Wall hit", 1f);
                statsRecorder.Add("Max Steps reached", 0f);
                statsRecorder.Add("Target Reached", 0f);
                statsRecorder.Add("Rotation Error", 0f);
                statsRecorder.Add("Out of bounds", 0f);
                break;
            
            case RecorderCodes.MaxSteps:
                statsRecorder.Add("Wall hit", 0f);
                statsRecorder.Add("Max Steps reached", 1f);
                statsRecorder.Add("Target Reached", 0f);
                statsRecorder.Add("Rotation Error", 0f);
                statsRecorder.Add("Out of bounds", 0f);
                break;

            case  RecorderCodes.Target:
                statsRecorder.Add("Wall hit", 0f);
                statsRecorder.Add("Max Steps reached", 0f);
                statsRecorder.Add("Target Reached", 1f);
                statsRecorder.Add("Rotation Error", 0f);
                statsRecorder.Add("Out of bounds", 0f);
                break;
            
            case RecorderCodes.RotationError:
                //statsRecorder.Add("Wall hit", 0f);
                //statsRecorder.Add("Max Steps reached", 0f);
                //statsRecorder.Add("Target Reached", 0f);
                statsRecorder.Add("Rotation Error", 1f);
                //statsRecorder.Add("Out of bounds", 0f);
                break;
            
            case RecorderCodes.OutOfBounds:
                statsRecorder.Add("Wall hit", 0f);
                statsRecorder.Add("Max Steps reached", 0f);
                statsRecorder.Add("Target Reached", 0f);
                statsRecorder.Add("Rotation Error", 0f);
                statsRecorder.Add("Out of bounds", 1f);
                break;
        }
    }
    
    /// <summary>
    /// Calculate the distance from the agent to the target. Taking doors into account.
    /// </summary>
    /// <remarks>Distance is basis for reward function.</remarks>
    public float m_DistToTarget = 0f;
    public float m_DistToTargetNormal = 0f;
    
    /*private float m_LastDistToTarget = 0f;
    private float m_BestDistToTarget = float.PositiveInfinity;
    private bool m_DistImproved = false;*/
    private void CalculateDistanceToTarget()
    {
        ResetDist();
        
        // Get the path from agent to target.
        GetPath();
        
        // Calculate the distance of the path.
        for (int i = 1; i < m_Path.Count; i++)
        {
            m_DistToTarget += Vector3.Distance(m_Path[i - 1], m_Path[i]);
        }
        
        // Calculate normalised distance to target.
        m_DistToTargetNormal = m_DistToTarget / m_MaxDist;
        
        /*if (m_DistToTarget < m_BestDistToTarget)
        {
            m_BestDistToTarget = m_DistToTarget;
            m_DistImproved = true;
        }*/
    }

    private void ResetDist()
    {
        /*m_LastDistToTarget = m_DistToTarget;
        m_DistToTarget = 0f;
        m_DistImproved = false;*/
        m_DistToTarget = 0f;
    }
    
    /// <summary>
    /// Get the path from the agent to the target.
    /// </summary>
    private List<Vector3> m_Path = new List<Vector3>();
    private void GetPath()
    {
        m_Path.Clear();
        
        var agentPosition = transform.position;
        var doorPositions = floor.GetDoorPosition();
        var targetPosition = floor.target.transform.position;
        
        // First point of the path is the agent position.
        m_Path.Add(agentPosition);
        
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
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        //var continuousActionsOut = actionsOut.ContinuousActions;
        //continuousActionsOut[0] = Input.GetAxis("Horizontal");
        //continuousActionsOut[1] = Input.GetAxis("Vertical");
        //continuousActionsOut[2] = Input.GetAxis("Mouse X");
        // Height
        //continuousActionsOut[3] = Input.GetAxis("Mouse Y");
        var discreteActionsOut = actionsOut.DiscreteActions;
        //var x = discreteActionsOut[0];
        //var y = discreteActionsOut[1];
        //var z = discreteActionsOut[2];
        //var rotation = discreteActionsOut[3];
        
        // X. Right, left and no movement.
        /*if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[0] = 2;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[0] = 3;
        }
        else
        {
            discreteActionsOut[0] = 1;
        }
        // Y. Up, down and no movement.
        if (Input.GetKey(KeyCode.X))
        {
            discreteActionsOut[1] = 2;
        }
        else if (Input.GetKey(KeyCode.Y))
        {
            discreteActionsOut[1] = 3;
        }
        else
        {
            discreteActionsOut[1] = 1;
        }
        // Z. Forward, backwards and no movement.
        if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[2] = 2;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[2] = 3;
        }
        else
        {
            discreteActionsOut[2] = 1;
        }
        // Rotation. Right, left and no rotation.
        if (Input.GetKey(KeyCode.E))
        {
            discreteActionsOut[3] = 2;
        }
        else if (Input.GetKey(KeyCode.Q))
        {
            discreteActionsOut[3] = 3;
        }
        else
        {
            discreteActionsOut[3] = 1;
        }*/
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
