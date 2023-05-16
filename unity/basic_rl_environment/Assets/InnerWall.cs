using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InnerWall : MonoBehaviour
{
    private Rigidbody m_RBody;
    
    // Start is called before the first frame update
    void Start()
    {
        m_RBody = GetComponent<Rigidbody>();
    }

    public void SetRandomPosition()
    {
        var loc = Random.Range(-1.5f, 1.5f);
        transform.localPosition = new Vector3(loc, 1f, -3f);
        transform.localScale = new Vector3(Random.Range(5f, 7f), 2f, 0.1f);
    }
    
    // Update is called once per frame
    void Update()
    {
        
    }
}
