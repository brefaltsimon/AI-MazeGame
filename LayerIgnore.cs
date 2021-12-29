using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LayerIgnore : MonoBehaviour
{ 
    void Start()
    {
        // Will ensure that the guards can move through each other
        Physics2D.IgnoreLayerCollision(10, 10);
    }
}
