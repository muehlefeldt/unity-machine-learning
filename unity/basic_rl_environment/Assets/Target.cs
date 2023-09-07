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
        /*// Get global coordinates of the corners of the training area.
        var cornerCoords = floor.globalCornerCoord;
        var zeroCorner = cornerCoords[^1];
        var xCorner = cornerCoords[^2];
        var zCorner = cornerCoords[1];

        Vector3 zPosition;
        if (floor.CreateWall)
        {
            if (floor.targetAlwaysInOtherRoomFromAgent)
            {
                /*foreach (var singleRoom in floor.rooms)
                {
                    if (!singleRoom.ContainsAgent())
                    {
                        singleRoom.
                    }
                }#1#
            }
            else
            {
                // If dynamic wall: Get position of the wall.
                var wallCoords = floor.CreatedWallsCoord;
                var zeroWallPosition = wallCoords[0].Item1;

                // Two z values are possible due to two possible rooms for the target. Position is chosen at random
                // from the two calculated options.
                var zOptions = new List<Vector3>();
                zOptions.Add(Vector3.Lerp(zeroCorner, zeroWallPosition, GetRandom()));
                zOptions.Add(Vector3.Lerp(zeroWallPosition, zCorner, GetRandom()));
                zPosition = zOptions[Random.Range(0, zOptions.Count)];
            }
        }
        else
        {
            // If no dynamic wall is present: One random position for z coordinates to be chosen. Only one room.
            zPosition = Vector3.Lerp(zeroCorner, zCorner, GetRandom());
        }
        
        // x Position is in every case the same.
        var xPosition = Vector3.Lerp(zeroCorner, xCorner, GetRandom());
        
        // Ensure correct height in y for the target and set the position. Use x and z from the corresponding vectors.
        var pos = Vector3.zero;
        pos.x = xPosition.x;
        pos.y = 0.5f;
        pos.z = zPosition.z;*/
        transform.position = pos;
    }
    
    
    
    void OnDrawGizmos()
    {
        if (floor.globalCornerCoord != null)
        {
            Gizmos.DrawWireSphere(floor.globalCornerCoord[1], 2f);
            //Gizmos.DrawWireSphere(floor.globalCornerCoord[0], 2f);
        }
    }
}
