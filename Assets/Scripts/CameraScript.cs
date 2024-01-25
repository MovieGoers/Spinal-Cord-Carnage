using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraScript : MonoBehaviour
{
    public GameObject Eyes;

    // Update is called once per frame
    void Update()
    {
        transform.position = Eyes.gameObject.transform.position;
        transform.rotation = Quaternion.Euler(InputManager.Instance.xRotation, InputManager.Instance.yRotation, 0);
    }
}
