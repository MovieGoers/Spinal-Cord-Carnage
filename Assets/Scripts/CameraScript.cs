using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraScript : MonoBehaviour
{
    public GameObject Player;

    public float cameraHeight;
    // Update is called once per frame
    void Update()
    {
        transform.position = Player.gameObject.transform.position + new Vector3(0, cameraHeight, 0);
        transform.rotation = Quaternion.Euler(InputManager.Instance.xRotation, InputManager.Instance.yRotation, 0);
    }
}
