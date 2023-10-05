using System.Collections.Generic;
using UnityEngine;

public class InnerWallCreator : MonoBehaviour
{
    public Floor floor;
    private List<GameObject> m_GameObjects = new List<GameObject>();
    private Material m_WallMaterial;
    
    private void Start()
    {
        m_WallMaterial = Resources.Load<Material>("WallMaterial");
    }
    
    /// <summary>
    /// Create part of the wall. Creates one single cube.
    /// </summary>
    /// <param name="coordStartWall"></param>
    /// <param name="coordEndWall"></param>
    private void CreateWall(Vector3 coordStartWall, Vector3 coordEndWall)
    {
        var between = coordEndWall - coordStartWall;
        var dist = Vector3.Distance(coordStartWall, coordEndWall);

        var newCube = CreateNewCube();

        newCube.transform.localScale = new Vector3(dist, 2f, 0.1f);
        var position = coordStartWall + (between / 2);
        position.y = 1f;
        newCube.transform.position = position;
        //SetCollider(newCube);
        m_GameObjects.Add(newCube);
    }

    //public float m_DoorWidth = 3.0f;
    private float m_DoorFrameHeight = 0.5f;

    private void CreateDoor((Vector3, Vector3) doorCoords)
    {
        /*var coordStart = coordDoor;
        var coordEnd = coordDoor;
        coordEnd.x -= m_DoorWidth;*/

        var between = doorCoords.Item1 - doorCoords.Item2;
        var dist = Vector3.Distance(doorCoords.Item1, doorCoords.Item2);

        var newCube = CreateNewCube();

        newCube.transform.localScale = new Vector3(dist, m_DoorFrameHeight, 0.1f);

        var position = doorCoords.Item1 - (between / 2);
        position.y = 1.75f;
        newCube.transform.position = position;
        m_GameObjects.Add(newCube);
    }

    /// <summary>
    /// Create a new primitive cube.
    /// </summary>
    /// <returns>Created cube.</returns>
    private GameObject CreateNewCube()
    {
        var newCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        newCube.transform.parent = transform;
        SetMaterial(newCube);
        SetCollider(newCube);
        return newCube;
    }
    
    public void CreateWallWithDoor((Vector3, Vector3) wallCoords, (Vector3, Vector3) doorCoords)
    {
        //wallParent.transform.InverseTransformPoint()
        // coordDoor = wallParent.transform.InverseTransformPoint(coordDoor);
        // coordStart = wallParent.transform.InverseTransformPoint(coordStart);
        // coordEnd = wallParent.transform.InverseTransformPoint(coordEnd);
        
        //var coordEndDoor = CreateDoor(doorCoords);
        
        CreateWall(wallCoords.Item1, doorCoords.Item1);
        CreateDoor(doorCoords);
        CreateWall(doorCoords.Item2, wallCoords.Item2);
        //CreateWall(doorCoords.Item2, wallCoords.Item2);
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
        //obj.GetComponent<Collider>().isTrigger = true;
    }
}
