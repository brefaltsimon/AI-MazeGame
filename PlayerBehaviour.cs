using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBehaviour : MonoBehaviour
{
    //player dies by changing its position, done by calling respawn() in LevelManager
    public void Die()
    {
        FindObjectOfType<LevelManager>().Respawn(gameObject);
    }
}
