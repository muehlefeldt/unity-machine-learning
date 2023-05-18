using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Schema;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

//using Random = Unity.Mathematics.Random;

public class Doorway : MonoBehaviour
{
    // Elements of the door frame.
    public Transform topFrame;
    public Transform leftFrame;
    public Transform rightFrame;

    /// <summary>
    /// Returns minimum and maximum x values of the doorway components.
    /// </summary>
    /// <returns></returns>
    public Tuple<float, float> GetMinMaxX()
    {
        var leftPosition = leftFrame.localPosition;
        var leftScale = leftFrame.localScale;
        var leftX = leftPosition.x + leftScale.x / 2f;
        
        var rightPosition = rightFrame.localPosition;
        var rightScale = rightFrame.localScale;
        var rightX = rightPosition.x + rightScale.x / 2f;
        
        return new Tuple<float, float>(Mathf.Min(leftX, rightX), Mathf.Max(leftX, rightX));
    }
    
    /// <summary>
    /// Reposition the doorway along the x axis.
    /// </summary>
    public void RandomReposition(float minX, float maxX, Vector3 parentPosition)
    {
       var randomX = Random.Range(minX, maxX);
       transform.Tr
    }
}
