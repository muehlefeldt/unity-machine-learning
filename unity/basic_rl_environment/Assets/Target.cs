using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Target : MonoBehaviour
{
    // public InnerWallCreator innerWallCreator;
    public Floor floor;
    public void ResetPosition()
    {
        var cornerCoords = floor.globalCornerCoord;
        var wallCoords = floor.CreatedWallsCoord;

        var minX = getMinX(cornerCoords);
        var maxX = getMaxX(cornerCoords);
        
        transform.localPosition = new Vector3(
            Random.Range(-3.5f, 3.5f),
            0.5f,
            Random.Range(-13f, 3f));
    }

    private float getMaxX(List<Vector3> vectorList)
    {
        var maxValue = float.NegativeInfinity;
        foreach (var vec in vectorList)
        {
            if (vec.x > maxValue)
            {
                maxValue = vec.x;
            }
        }
        return maxValue;
    }
    
    private float getMinX(List<Vector3> vectorList)
    {
        var minValue = float.NegativeInfinity;
        foreach (var vec in vectorList)
        {
            if (vec.x > minValue)
            {
                minValue = vec.x;
            }
        }
        return minValue;
    }

    public Vector3 GetLocalPosition()
    {
        return transform.localPosition;
    }

    /*private void OnTriggerEnter(Collider other)
    {
        var compareObj = innerWallCreator.wallParent;
        
        ResetPosition();
    }*/
    
    /*void Update()
    {
        if (innerWallCreator.wallParent != null)
        {
            // Check for overlap between this object's collider and the target object's collider
            if (GetComponent<Collider>().bounds.Intersects(innerWallCreator.wallParent.GetComponent<Collider>().bounds))
            {
                // Overlap detected
                Debug.Log("Overlap detected!");
            }
        }
    }*/
}
