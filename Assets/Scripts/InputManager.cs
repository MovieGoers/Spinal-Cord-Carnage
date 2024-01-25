using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    private static InputManager instance;

    public float mouseZ;
    public static InputManager Instance
    {
        get { return instance; }
    }
    private void Awake()
    {
        if (instance)
        {
            Destroy(instance);
            return;
        }

        instance = this;
        DontDestroyOnLoad(this.gameObject);
    }
}
