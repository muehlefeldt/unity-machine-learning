using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
//using DefaultNamespace;
using UnityEngine;

using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;


public class RollerAgent : Agent
{
    private Rigidbody m_RBody;
    public Target target;
    public Floor floor;
    public float m_MaxDist;

    // Start is called before the first frame update
    void Start () {
        m_RBody = GetComponent<Rigidbody>();
        ResetAgentPosition();
    }
    
    /// <summary>
    /// Reset the position of the agent to a default position.
    /// </summary>
    private void ResetAgentPosition()
    {
        m_RBody.angularVelocity = Vector3.zero;
        m_RBody.velocity = Vector3.zero;
            
        transform.localPosition = new Vector3( 0, 1f, 3f);
        transform.rotation = Quaternion.identity;
    }

    private int m_MaxSteps;
    private int m_CurrentStep;
    public override void OnEpisodeBegin()
    {
        floor.Prepare();
        floor.CreateInnerWall();
        ResetDist();

        // How many steps are allowed.
        m_MaxSteps = 1000;
        m_CurrentStep = 0;
        
        // If the Agent fell, zero its momentum
        if (this.transform.localPosition.y < 0 || m_CollisionDetected || m_ImplausiblePosition)
        {
            m_CollisionDetected = false;
            m_ImplausiblePosition = false;
            ResetAgentPosition();
        }

        // Move the target to a new spot
        target.ResetPosition();

        m_MaxDist = floor.GetMaxPossibleDist();
        CalculateDistanceToTarget();
    }

    private float m_RayForwardDist;
    private float m_RayBackDist;
    private float m_RayLeftDist;
    private float m_RayRightDist;
    private float m_RayUpDist;
    private float m_RayDownDist;
    void FixedUpdate()
    {
        /*var currentRay = new Ray(transform.localPosition, transform.TransformDirection(Vector3.forward));
        RaycastHit currentHit;
        Physics.Raycast(currentRay, out currentHit, maxDistance: Mathf.Infinity);*/
        m_RayForwardDist = PerformRaycastGetDistance(Vector3.forward);
        m_RayBackDist = PerformRaycastGetDistance(Vector3.back);
        m_RayLeftDist = PerformRaycastGetDistance(Vector3.left);
        m_RayRightDist = PerformRaycastGetDistance(Vector3.right);
        m_RayUpDist = PerformRaycastGetDistance(Vector3.up);
        m_RayDownDist = PerformRaycastGetDistance(Vector3.down);
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
        if (dir == Vector3.forward)
        {
            Debug.DrawRay(transform.position, dirTransform * floor.GetMaxPossibleDist(), Color.red);
        }

        return currentHit.distance / floor.GetMaxPossibleDist();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Target and Agent positions
        //sensor.AddObservation(target.localPosition);
        //sensor.AddObservation(this.transform.localPosition);
        
        sensor.AddObservation(m_RayForwardDist);
        sensor.AddObservation(m_RayBackDist);
        sensor.AddObservation(m_RayLeftDist);
        sensor.AddObservation(m_RayRightDist);
        sensor.AddObservation(m_RayUpDist);
        sensor.AddObservation(m_RayDownDist);

        // Agent velocity.
        sensor.AddObservation(m_RBody.velocity.x);
        sensor.AddObservation(m_RBody.velocity.z);
        
        // Agent rotation.
        Quaternion rotation = m_RBody.rotation;
        sensor.AddObservation(rotation.eulerAngles / 360.0f);  // [0,1]
    }

    /// <summary>
    /// Check if agent position and rotation is plausible. Used to detect positions outside of training area.
    /// </summary>
    /// <returns></returns>
    private bool IsAgentStatePlausible()
    {
        // Fell off platform or is beyond roof.
        var y = transform.localPosition.y;
        if (y is < 0f or > 2f)
        {
            return false;
        }
        // Check rotation. In case of implausible values terminate episode.
        var rotation = transform.rotation;
        if (!Mathf.Approximately(rotation.x, 0f) || !Mathf.Approximately(rotation.z, 0f))
        {
            return false;
        }
        return true;

    }
    
    private bool m_ImplausiblePosition = false;
    public float forceMultiplier = 10f;
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        Vector3 controlSignal = Vector3.zero;
        
        var rightLeft = actionBuffers.DiscreteActions[0];
        var upDown = actionBuffers.DiscreteActions[1];
        var forwardBackwards = actionBuffers.DiscreteActions[2];
        var r = actionBuffers.DiscreteActions[3];
        
        switch (rightLeft)
        {
            case 2:
                controlSignal.x = 1f;
                break;
            case 3:
                controlSignal.x = -1f;
                break;
        }
        
        switch (upDown)
        {
            case 2:
                controlSignal.y = 1f;
                break;
            case 3:
                controlSignal.y = -1f;
                break;
        }
        
        switch (forwardBackwards)
        {
            case 2:
                controlSignal.z = 1f;
                break;
            case 3:
                controlSignal.z = -1f;
                break;
        }

        var rotate = 0f;
        switch (r)
        {
            case 2:
                rotate = 1f;
                break;
            case 3:
                rotate = -1f;
                break;
        }
        
        // Rotate
        var turnSpeed = 200;
        var rotateDir = transform.up * rotate;
        transform.Rotate(rotateDir, Time.fixedDeltaTime * turnSpeed);

        //controlSignal = Vector3.Scale(controlSignal, transform.forward);
        //m_RBody.AddForce(controlSignal * forceMultiplier);
        //transform.localPosition = transform.localPosition + controlSignal * forceMultiplier;
        //m_RBody.MovePosition(transform.localPosition + controlSignal * forceMultiplier);
        
        var direction = m_RBody.rotation * controlSignal;
        m_RBody.MovePosition(m_RBody.position + direction * (Time.deltaTime * forceMultiplier));
        
        // Move forward
        //var dirToGo = Vector3.Scale(transform.forward, controlSignal);// * m_Forward;
        //m_RBody.MovePosition(transform.localPosition + dirToGo * (Time.fixedDeltaTime * forceMultiplier));

        // Rewards
        //var currentDistToTarget = Vector3.Distance(transform.position, target.transform.position);
        CalculateDistanceToTarget();
        
        
        
        // Reached target
        if (m_DistToTarget < 1.42f)
        {
            RecordData(RecorderCodes.Target);
            SetReward(1f);
            EndEpisode();
        }
        
        // Verify agent state (position) is plausible.
        if (!IsAgentStatePlausible())
        {
            RecordData(RecorderCodes.Implausible);
            m_ImplausiblePosition = true;
            EndEpisode();
        }
        
        // Punish each step.
        //AddReward(-0.001f);
        m_CurrentStep += 1;

        if (m_CurrentStep > m_MaxSteps)
        {
            RecordData(RecorderCodes.MaxSteps);
            SetReward(-1f);
            EndEpisode();
        }
        
        CalculateReward();
        SetReward(reward);
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
        /*// If distance increases to target no reward issued.
        if (m_DistToTarget >= m_LastDistToTarget)
        {
            reward = 0f;
            return reward;
        }
        var beta = 0.5f;
        var omega = 0.3f;
        var x = m_DistToTarget / m_MaxDist;

        reward = beta * Mathf.Exp(-1 * Mathf.Pow(x, 2f) / (2f * Mathf.Pow(omega, 2f)));
        return reward;*/
        if (m_DistToTarget < m_LastDistToTarget)
        {
            reward = 0.1f;
        }
        else if (m_DistToTarget > m_LastDistToTarget)
        {
            reward = -0.1f;
        }
        else
        {
            reward = 0f;
        }
    }
    
    /// <summary>
    /// Collision handling. Reset of the agent position if agent collides with other game objects.
    /// </summary>
    /// <param name="other"></param>
    private bool m_CollisionDetected = false;
    private void OnTriggerEnter(Collider other)
    {
        RecordData(RecorderCodes.Wall);
        m_CollisionDetected = true;
        SetReward(-1f);
        EndEpisode();
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
        Target = 4
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
                statsRecorder.Add("Implausible agent position", 0f);
                statsRecorder.Add("Target Reached", 0f);
                break;
            
            case RecorderCodes.MaxSteps:
                statsRecorder.Add("Wall hit", 0f);
                statsRecorder.Add("Max Steps reached", 1f);
                statsRecorder.Add("Implausible agent position", 0f);
                statsRecorder.Add("Target Reached", 0f);
                break;

            case RecorderCodes.Implausible:
                statsRecorder.Add("Wall hit", 0f);
                statsRecorder.Add("Max Steps reached", 0f);
                statsRecorder.Add("Implausible agent position", 1f);
                statsRecorder.Add("Target Reached", 0f);
                break;
            
            case  RecorderCodes.Target:
                statsRecorder.Add("Wall hit", 0f);
                statsRecorder.Add("Max Steps reached", 0f);
                statsRecorder.Add("Implausible agent position", 0f);
                statsRecorder.Add("Target Reached", 1f);
                break;
        }
    }
    
    /// <summary>
    /// Calculate the distance from the agent to the target. Taking doors into account.
    /// </summary>
    /// <remarks>Distance is basis for reward function.</remarks>
    public float m_DistToTarget = 0f;
    private float m_LastDistToTarget = 0f;
    private float m_BestDistToTarget = float.PositiveInfinity;
    private bool m_DistImproved = false;
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

        if (m_DistToTarget < m_BestDistToTarget)
        {
            m_BestDistToTarget = m_DistToTarget;
            m_DistImproved = true;
        }
    }

    private void ResetDist()
    {
        m_LastDistToTarget = m_DistToTarget;
        m_DistToTarget = 0f;
        m_DistImproved = false;
    }
    
    /// <summary>
    /// Get the path from the agent to the target.
    /// </summary>
    /// <remarks>
    /// ToDo
    /// </remarks>
    private List<Vector3> m_Path = new List<Vector3>();
    private void GetPath()
    {
        m_Path.Clear();
        
        var agentPosition = transform.position;
        var doorPositions = floor.CreatedDoorsCoord;
        var targetPosition = target.transform.position;
        
        // First point of the path is the agent position.
        m_Path.Add(agentPosition);

        foreach (var coord in doorPositions)
        {
            if (agentPosition.z > targetPosition.z)
            {
                if ((agentPosition.z > coord.z) && (coord.z > targetPosition.z))
                {
                    m_Path.Add(coord);
                }
            }
            else
            {
                if ((agentPosition.z < coord.z) && (coord.z < targetPosition.z))
                {
                    m_Path.Add(coord);
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
        if (Input.GetKey(KeyCode.D))
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
        }
    }
}
