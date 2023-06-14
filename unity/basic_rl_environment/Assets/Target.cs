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

        var minX = getMin(cornerCoords, 0);
        var maxX = getMax(cornerCoords, 0);
        
        var minZ = getMin(cornerCoords, 2);
        var maxZ = getMax(cornerCoords, 2);

        var distFromWall = 0.5f;
        var rangesX = new List<float>();
        rangesX.Add(minX);
        var rangesZ = new List<float>();
        rangesZ.Add(maxZ);

        foreach (var coords in wallCoords)
        {
            rangesX.Add(coords.Item1.x);
            rangesZ.Add(coords.Item1.z);
        }
        
        rangesX.Add(maxX);
        rangesZ.Add(maxZ);

        var randomX = new List<float>();
        var randomZ = new List<float>();

        var tmpX = rangesX[0];
        foreach (var x in rangesX)
        {
            randomX.Add(Random.Range(tmpX + distFromWall, x - distFromWall));
            tmpX = x;
        }
        
        var tmpZ = rangesZ[0];
        foreach (var z in rangesZ)
        {
            randomZ.Add(Random.Range(tmpZ + distFromWall, z - distFromWall));
            tmpZ = z;
        }
        
        transform.localPosition = transform.InverseTransformPoint(
            new Vector3(
                randomX[Random.Range(0, randomX.Count)],
                1f,
                randomZ[Random.Range(0, randomZ.Count)]
            ));
    }

    private float getMax(List<Vector3> vectorList, int index)
    {
        var maxValue = float.NegativeInfinity;
        foreach (var vec in vectorList)
        {
            if (vec[index] > maxValue)
            {
                maxValue = vec.x;
            }
        }
        return maxValue;
    }
    
    private float getMin(List<Vector3> vectorList, int index)
    {
        var minValue = float.PositiveInfinity;
        foreach (var vec in vectorList)
        {
            if (vec[index] < minValue)
            {
                minValue = vec[index];
            }
        }
        return minValue;
    }

    public Vector3 GetLocalPosition()
    {
        return transform.localPosition;
    }
}
