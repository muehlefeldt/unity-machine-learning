using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InnerWallCreator : MonoBehaviour
{
    public Floor floor;
    private List<GameObject> m_GameObjects = new List<GameObject>();

    private void CreateWall(Vector3 coordStartWall, Vector3 coordEndWall)
    {
        var between = coordEndWall - coordStartWall;
        var dist = Vector3.Distance(coordStartWall, coordEndWall);
        
        GameObject newCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        newCube.transform.parent = floor.transform; 
        
        newCube.transform.localScale = new Vector3(dist, 2f, 0.1f);
        var position = coordStartWall + (between / 2);
        position.y = 1f;
        newCube.transform.localPosition = position;
        SetCollider(newCube);
        m_GameObjects.Add(newCube);
    }

    private float m_DoorWidth = 1.5f;
    private float m_DoorFrameHeight = 0.5f;
    private Vector3 CreateDoor(Vector3 coordDoor)
    {
        var coordStart = coordDoor;
        var coordEnd = coordDoor;
        coordEnd.x -= m_DoorWidth;
        
        var between = coordEnd - coordStart;
        var dist = Vector3.Distance(coordStart, coordEnd);
        
        GameObject newCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        newCube.transform.parent = floor.transform;
        
        newCube.transform.localScale = new Vector3(dist, m_DoorFrameHeight, 0.1f);
        
        var position = coordStart + (between / 2);
        position.y = 1.75f;
        newCube.transform.localPosition = position;
        SetCollider(newCube);
        m_GameObjects.Add(newCube);

        return coordEnd;
    }

    public void CreateWallWithDoor(Vector3 coordDoor, Vector3 coordStart, Vector3 coordEnd)
    {
        CreateWall(coordStart, coordDoor);
        var coordEndDoor = CreateDoor(coordDoor);
        CreateWall(coordEndDoor, coordEnd);
    }
    
    /// <summary>
    /// Destroy all created game objects.
    /// </summary>
    public void DestroyAll()
    {
        foreach (var element in m_GameObjects)
        {
            Destroy(element);
        }
    }
    
    /// <summary>
    /// Set the Collider of a game object as trigger.
    /// </summary>
    /// <param name="obj">Game object with collider</param>
    private void SetCollider(GameObject obj)
    {
        obj.GetComponent<Collider>().isTrigger = true;
    }
}
