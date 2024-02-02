using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Target : MonoBehaviour
{
    // public InnerWallCreator innerWallCreator;
    public Floor floor;
    
    /// <summary>
    /// Reset the position of the target to a valid position within in the training area.
    /// </summary>
    public void ResetPosition(Vector3 pos)
    {
        transform.position = pos;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Draw Gizmo to help visualise the training.
    /// </summary>
    void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, 1.42f);
    }
#endif
}
