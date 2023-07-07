using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Target : MonoBehaviour
{
    // public InnerWallCreator innerWallCreator;
    public Floor floor;
    private Vector3 final = Vector3.zero;
    private float m_distFromWall = 0.2f;
    private List<Vector3> options;
    public void ResetPosition()
    {
        var cornerCoords = floor.globalCornerCoord;
        var wallCoords = floor.CreatedWallsCoord;

        var a = wallCoords[0].Item1;
        var b = wallCoords[0].Item2;
        var randomWall = Vector3.Lerp(a, b, getRandom());
        
        var c = cornerCoords[2];
        var second = Vector3.Lerp(a, c, getRandom());

        var d = cornerCoords[0];
        var third = Vector3.Lerp(a, d, getRandom());

        options = new List<Vector3>();
        options.Add(Vector3.Lerp(randomWall, second, getRandom()));
        options.Add(Vector3.Lerp(randomWall, third, getRandom()));

        final = options[Random.Range(0, options.Count)];

        final.y = 0.5f;
        transform.position = final;
    }

    private float getRandom()
    {
        var t = Random.Range(0f + m_distFromWall, 1f - m_distFromWall);
        return t;
    }
    
    void OnDrawGizmos()
    {
        if (final != Vector3.zero)
        {
            Gizmos.DrawSphere(final, 0.8f);
        }

        if (options != null)
        {
            foreach (var coord in options)
            {
                Gizmos.DrawWireSphere(coord, 0.3f);
            }
        }
    }

    private float getMax(List<Vector3> vectorList, int index)
    {
        var maxValue = float.NegativeInfinity;
        foreach (var vec in vectorList)
        {
            if (vec[index] > maxValue)
            {
                maxValue = vec[index];
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
