using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Z : MonoBehaviour
{
    private GameObject rotatingObject;

    void Start()
    {
        // Create a cube GameObject
        rotatingObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        // Position it in front
        rotatingObject.transform.position = new Vector3(0, 0, 5);
    }

    void Update()
    {
        // Rotate the cube around the Y axis
        if (rotatingObject != null)
        {
            rotatingObject.transform.Rotate(0, 45 * Time.deltaTime, 0);
        }
    }
}
