using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.Serialization;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

public class RollerAgent : Agent
{
    private Rigidbody m_RBody;
    public Transform target;
    public Floor floor;
    private float m_MaxDist;
    
    // Start is called before the first frame update
    void Start () {
        m_RBody = GetComponent<Rigidbody>();
    }
    
    public override void OnEpisodeBegin()
    {
        // If the Agent fell, zero its momentum
        if (this.transform.localPosition.y < 0)
        {
            this.m_RBody.angularVelocity = Vector3.zero;
            this.m_RBody.velocity = Vector3.zero;
            
            this.transform.localPosition = new Vector3( 0, 0.25f, 0);
            transform.rotation = Quaternion.identity;
        }

        // Move the target to a new spot
        target.localPosition = new Vector3(Random.value * 8 - 4,
            0.5f,
            Random.value * 8 - 4);

        //m_MaxDist = floor.GetMaxPossibleDist();
    }

    private float m_RayForwardDist;
    private float m_RayBackDist;
    private float m_RayLeftDist;
    private float m_RayRightDist;
    void FixedUpdate()
    {
        /*var currentRay = new Ray(transform.localPosition, transform.TransformDirection(Vector3.forward));
        RaycastHit currentHit;
        Physics.Raycast(currentRay, out currentHit, maxDistance: Mathf.Infinity);*/
        m_RayForwardDist = PerformRaycastGetDistance(Vector3.forward);
        m_RayBackDist = PerformRaycastGetDistance(Vector3.back);
        m_RayLeftDist = PerformRaycastGetDistance(Vector3.left);
        m_RayRightDist = PerformRaycastGetDistance(Vector3.right);
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

        // Agent velocity.
        sensor.AddObservation(m_RBody.velocity.x);
        sensor.AddObservation(m_RBody.velocity.z);
        
        // Agent rotation.
        Quaternion rotation = m_RBody.rotation;
        sensor.AddObservation(rotation.eulerAngles / 360.0f);  // [0,1]
    }
    
    public float forceMultiplier = 10f;
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Actions, size = 2
        Vector3 controlSignal = Vector3.zero;
        controlSignal.x = actionBuffers.ContinuousActions[0];
        controlSignal.z = actionBuffers.ContinuousActions[1];
        var rotate = Mathf.Clamp(actionBuffers.ContinuousActions[2], -1f, 1f);
        
        // Rotate
        var turnSpeed = 200;
        var rotateDir = transform.up * rotate;
        transform.Rotate(rotateDir, Time.fixedDeltaTime * turnSpeed);

        controlSignal = Vector3.Scale(controlSignal, transform.forward);
        //m_RBody.AddForce(controlSignal * forceMultiplier);
        //transform.localPosition = transform.localPosition + controlSignal * forceMultiplier;
        //m_RBody.MovePosition(transform.localPosition + controlSignal * forceMultiplier);
        
        var direction = m_RBody.rotation * controlSignal;
        m_RBody.MovePosition(m_RBody.position + direction * (Time.deltaTime * forceMultiplier));
        
        // Move forward
        //var dirToGo = Vector3.Scale(transform.forward, controlSignal);// * m_Forward;
        //m_RBody.MovePosition(transform.localPosition + dirToGo * (Time.fixedDeltaTime * forceMultiplier));

        // Rewards
        float distanceToTarget = Vector3.Distance(this.transform.localPosition, target.localPosition);

        // Reached target
        if (distanceToTarget < 1.42f)
        {
            AddReward(1.0f);
            EndEpisode();
        }

        // Fell off platform
        else if (this.transform.localPosition.y < 0)
        {
            EndEpisode();
        }
        AddReward(-0.001f);
    }
    
    /// <summary>
    /// Collision handling. Reset if the agent collides with other game objects.
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
        SetReward(-1f);
        EndEpisode();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
        continuousActionsOut[2] = Input.GetAxis("Mouse X");
    }
}
