using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class InnerWallCreator : MonoBehaviour
{
    public Floor floor;
    private List<GameObject> m_GameObjects = new List<GameObject>();
    public GameObject wallParent;
    private Material m_WallMaterial;

    private void Start()
    {
        wallParent = new GameObject();
        wallParent.transform.SetParent(floor.transform);
        wallParent.transform.localPosition = Vector3.zero;
        //newCube.transform.parent = emptyGameObject.transform;
        m_WallMaterial = Resources.Load<Material>("WallMaterial");
    }

    private void CreateWall(Vector3 coordStartWall, Vector3 coordEndWall)
    {
        var between = coordEndWall - coordStartWall;
        var dist = Vector3.Distance(coordStartWall, coordEndWall);

        var newCube = CreateNewCube();

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

        var newCube = CreateNewCube();

        newCube.transform.localScale = new Vector3(dist, m_DoorFrameHeight, 0.1f);

        var position = coordStart + (between / 2);
        position.y = 1.75f;
        newCube.transform.localPosition = position;
        m_GameObjects.Add(newCube);

        return coordEnd;
    }

    /// <summary>
    /// Create a new primitive cube.
    /// </summary>
    /// <returns>Created cube.</returns>
    private GameObject CreateNewCube()
    {
        var newCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        newCube.transform.parent = wallParent.transform;
        SetMaterial(newCube);
        return newCube;
    }

    public void CreateWallWithDoor(Vector3 coordDoor, Vector3 coordStart, Vector3 coordEnd)
    {
        //wallParent.transform.InverseTransformPoint()
        coordDoor = wallParent.transform.InverseTransformPoint(coordDoor);
        coordStart = wallParent.transform.InverseTransformPoint(coordStart);
        coordEnd = wallParent.transform.InverseTransformPoint(coordEnd);

        CreateWall(coordStart, coordDoor);
        var coordEndDoor = CreateDoor(coordDoor);
        CreateWall(coordEndDoor, coordEnd);
    }

    /// <summary>
    /// Destroy all created game objects. Delete existing navmesh.
    /// </summary>
    public void DestroyAll()
    {
        foreach (var element in m_GameObjects)
        {
            Destroy(element);
        }
        
    }

    /// <summary>
    /// Set the material of the wall elements. Visual effect only.
    /// </summary>
    /// <param name="obj">Game object to be modified.</param>
    private void SetMaterial(GameObject obj)
    {
        obj.GetComponent<Renderer>().material = m_WallMaterial;
    }
    
    /// <summary>
    /// Set the Collider of a game object as trigger.
    /// </summary>
    /// <param name="obj">Game object with collider</param>
    private void SetCollider(GameObject obj)
    {
        //obj.transform.parent = wallParent.transform;
        obj.GetComponent<Collider>().isTrigger = true;
    }
}
