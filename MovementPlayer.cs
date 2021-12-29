using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementPlayer : MonoBehaviour
{
    Rigidbody2D rb;

    float horizontal;
    float vertical;

    //direction player is looking
    int lookingDirection = 1;

    public float runSpeed = 20.0f;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        // a & left arrow = -1, d & right arrow = 1 
        horizontal = Input.GetAxisRaw("Horizontal");
        // s & down arrow = -1, w & up arrow = 1 
        vertical = Input.GetAxisRaw("Vertical");
    }

    private void FixedUpdate()
    {
        //sets velocity if key is pressed, key is either positive or negative
        rb.velocity = new Vector2(horizontal * runSpeed, vertical * runSpeed);
    }

    private void InitializePlayer()
    {
        Flip(KeyCode.W);
    }

    //Can be used in the future for player rotation if we have a fov cone for player
    private void Flip(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.W:
                lookingDirection = 1;
                break;
            case KeyCode.A:
                lookingDirection = 2;
                break;
            case KeyCode.S:
                lookingDirection = 3;
                break;
            case KeyCode.D:
                lookingDirection = 4;
                break;
        }
    }
    
}


