using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InnerWallDoor : MonoBehaviour
{
    public Doorway doorway;
    public Transform leftInnerWall;
    public Transform rightInnerWall;

    public float minX = 1.5f;
    public float maxX = 3.5f;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void RandomReposition()
    {
        RepositionDoorway();
        //Resizewalls();
    }
    
    private void RepositionDoorway()
    {
        var position = transform.localPosition;
        doorway.RandomReposition(minX, maxX, position);
    }
}
