using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{

    void Update()
    {
        transform.rotation = Quaternion.Euler(0f, InputManager.Instance.yRotation, 0f);
    }
}
