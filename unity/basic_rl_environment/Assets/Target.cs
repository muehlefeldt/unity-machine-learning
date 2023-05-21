using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Target : MonoBehaviour
{
    public void ResetPosition()
    {
        transform.localPosition = new Vector3(
            Random.Range(-3.5f, 3.5f),
            0.5f,
            Random.Range(-13f, 3f));
    }

    public Vector3 GetLocalPosition()
    {
        return transform.localPosition;
    }
}
