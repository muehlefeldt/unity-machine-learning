using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Decoy
{
    private GameObject m_Object;
    public Decoy(UnityEngine.Transform parent)
    {
        m_Object = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        m_Object.transform.localScale = new Vector3(2, 1, 2);
        m_Object.tag = "decoys";
        m_Object.GetComponent<Renderer>().material = Resources.Load<Material>("DecoyMaterial");
        m_Object.transform.parent = parent;
    }
    public void ResetPosition(Vector3 pos)
    {
        m_Object.transform.position = pos;
    }
}
