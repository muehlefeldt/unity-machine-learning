using System.Collections.Generic;
using UnityEngine;

public class InnerWallCreator
{
    //public Floor floor;
    private List<GameObject> m_GameObjects = new List<GameObject>();
    private Material m_WallMaterial;
    private Material m_FrameMaterial;
    private Material m_CheckpointMaterial;

    private Transform m_FloorTransform;
    
    public InnerWallCreator(Transform floorTransform)
    {
        m_WallMaterial = Resources.Load<Material>("WallMaterial");
        m_FrameMaterial = Resources.Load<Material>("FrameMaterial");
        m_CheckpointMaterial = Resources.Load<Material>("CheckpointMaterial");

        m_FloorTransform = floorTransform;
    }
    
    /// <summary>
    /// Create part of the wall. Creates one single cube.
    /// </summary>
    /// <param name="coordStartWall"></param>
    /// <param name="coordEndWall"></param>
    private void CreateWall(Vector3 coordStartWall, Vector3 coordEndWall)
    {
        //coordStartWall.x -= m_DoorFrameWidth;
        //coordEndWall.x -= m_DoorFrameWidth;
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
    private float m_DoorFrameWidth = 0.5f;

    private void CreateDoor((Vector3, Vector3) doorCoords)
    {
        /*var coordStart = coordDoor;
        var coordEnd = coordDoor;
        coordEnd.x -= m_DoorWidth;*/

        var between = doorCoords.Item1 - doorCoords.Item2;
        var dist = Vector3.Distance(doorCoords.Item1, doorCoords.Item2);

        var pos = doorCoords.Item1;
        pos.x = pos.x + (m_DoorFrameWidth / 2);
        pos.y = 1f;
        //pos.x += 1f;
        var locScale = new Vector3(m_DoorFrameWidth, 2, 0.1f);
        CreateFrameObject(m_FloorTransform, pos, locScale);
        
        pos = doorCoords.Item2;
        pos.x = pos.x - (m_DoorFrameWidth / 2);
        pos.y = 1f;
        //pos.x += 1f;
        locScale = new Vector3(m_DoorFrameWidth, 2, 0.1f);
        CreateFrameObject(m_FloorTransform, pos, locScale);


        var passageCoordStart = doorCoords.Item1;
        passageCoordStart.x += m_DoorFrameWidth;
        var passageCoordEnd = doorCoords.Item2;
        passageCoordEnd.x -= m_DoorFrameWidth;

        dist = Vector3.Distance(passageCoordStart, passageCoordEnd);
        
        //var newCube = CreateNewCube();
        locScale = new Vector3(dist, m_DoorFrameWidth, 0.1f);
        pos = doorCoords.Item1 - (between / 2);
        pos.y = 1.75f;
        //newCube.transform.position = position;
        //m_GameObjects.Add(newCube);
        CreateFrameObject(m_FloorTransform, pos, locScale);

        var checkpoint = CreateNewCheckpoint();
        checkpoint.transform.localScale = new Vector3(dist, 2 - m_DoorFrameWidth, 0.1f);
        pos.y = 0.75f;
        checkpoint.transform.position = pos;
        m_GameObjects.Add(checkpoint);
    }

    /// <summary>
    /// Create a new primitive cube.
    /// </summary>
    /// <returns>Created cube.</returns>
    private GameObject CreateNewCube()
    {
        var newCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        newCube.transform.parent = m_FloorTransform;
        newCube.tag = "innerWall";
        SetMaterial(newCube);
        SetCollider(newCube);
        return newCube;
    }
    
    private void CreateFrameObject(UnityEngine.Transform parent, Vector3 position, Vector3 localScale)
    {
        var newCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        newCube.transform.position = position;
        newCube.transform.localScale = localScale;
        newCube.transform.parent = parent;
        newCube.tag = "door";
        newCube.name = "Frame";
        newCube.GetComponent<Renderer>().material = m_FrameMaterial;
        //SetCollider(newCube);
        m_GameObjects.Add(newCube);
    }
    
    private GameObject CreateNewCheckpoint()
    {
        var newCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        newCube.transform.parent = m_FloorTransform;
        newCube.GetComponent<Collider>().isTrigger = true;
        newCube.layer = 2;
        newCube.GetComponent<Renderer>().material = m_CheckpointMaterial;
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
            UnityEngine.Object.Destroy(element);
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
