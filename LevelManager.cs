using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    [SerializeField]
    private Transform respawnPoint; //respawn point when player dies

    [SerializeField]
    private GameObject levelGoal; //goal point for the player to  reach in order to win

    [SerializeField]
    private GameObject player; //the player

    // Start is called before the first frame update
    void Start()
    {
        StartLevel();   
    }

    private void Update()
    {
        //if player gets to the goal we change screen to WinScreen
        if(levelGoal.GetComponentInChildren<Collider2D>().IsTouching(player.GetComponent<Collider2D>()))
        {
            LoadScene("WinScreen");
        }
    }

    //will set the players position to the exact position of the respawnPoint
    private void StartLevel()
    {
        player.transform.position = respawnPoint.position;
    }

    //respawns the object by resettign the rotation and position
    public void Respawn(GameObject go)
    {
        go.transform.position = respawnPoint.position;
        go.transform.rotation = respawnPoint.rotation;
    }

    //loads the provided scene
    private void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
