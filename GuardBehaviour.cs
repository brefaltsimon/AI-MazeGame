using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GuardBehaviour : MonoBehaviour
{
    private const int numDirections = 4;

    private bool reachedNewTile = true;
    private Vector2 size;
    private walkingDirection currentWalkingDirection;
    private Vector3Int previousTile;
    private Color randomMovingColor;

    private Stack<Vector3Int> walkingStack = new Stack<Vector3Int>();
    private Vector3Int goal;
    private bool stackUpdated = false;

    //SerializeFields are set in the editor
    [SerializeField]
    private State currentState;
    
    [SerializeField]
    private Tilemap walls;

    [SerializeField]
    private Tilemap walkingPath;

    [SerializeField]
    private TrailManager tm;
    
    private float walkSpeed;
    private float step;

    // Just an enumerator to easily know which direction is which
    // public needed for other scripts to access directions
    public enum walkingDirection
    {
        up = 0,
        right = 1,
        down = 2,
        left = 3
    }

    // The avaliable states for the guards
    private enum State
    {
        randomMoving, //just explore the maze
        pathFinding, //another guard has seen the player, find the best path there
        lookForPlayer //search area where the player was last seen
    }

    private Rigidbody2D rb;

    private void Awake()
    {
        tm = FindObjectOfType<TrailManager>(); //get the trail manager of the scene
    }

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        size = GetComponent<BoxCollider2D>().size;
        walkSpeed = 2f;
        ChooseNewWalkingDirection(); //we need a starting direction
        currentState = State.randomMoving;
        goal = new Vector3Int(0, 0, 15); //failsafe when it has no goal and are in path finding.
        randomMovingColor = GetComponent<SpriteRenderer>().color;
    }

    // Update is called once per frame
    void Update()
    {
        step = Time.deltaTime*walkSpeed;
        switch (currentState)
        {
            case State.randomMoving:
                GetComponent<SpriteRenderer>().color = randomMovingColor;
                WalkUsingMoveTowards();
                if (!reachedNewTile)
                {
                    reachedNewTile = CheckIfReachedNewTile();
                }
                if (FoundAnIntersectionTileMap() && ReachedTileCenter() && reachedNewTile)
                {
                    reachedNewTile = false;
                    previousTile = walls.WorldToCell(transform.position);
                    ChooseNewWalkingDirection();
                } 
                break;
            case var n when (n == State.pathFinding || n == State.lookForPlayer):
                
                if (stackUpdated && walkingStack.Count != 0)
                {
                    FollowStack(walkingStack);
                    stackUpdated = false;
                    previousTile = walls.WorldToCell(transform.position);
                }

                if (ReachedGoal(goal) && walkingStack.Count != 0)
                {
                    stackUpdated = false;
                    previousTile = walls.WorldToCell(transform.position);
                    FollowStack(walkingStack);
                }
                else if (goal.z != 15 && !ReachedGoal(goal))
                {
                    MoveToNextInStack(goal);
                }
                else if(ReachedGoal(goal) && walkingStack.Count == 0)
                {
                    ChangeState(State.randomMoving);
                }
                break;
            case State.lookForPlayer:
                // Nothing here right now
                break;
        }
    }

    // Debugger function, prints the stack sent in without emptying it
    private void PrintStack(Stack<Vector3Int> stack)
    {
        foreach(var item in stack)
        {
            Debug.Log("Stack trace" + item);
        }       
    }

    //will set goal to next in stack
    private void FollowStack(Stack<Vector3Int> walkingStack)
    {
        goal = walkingStack.Pop();
        transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.Normalize(goal - previousTile));
    }

    //moves to next in walking stack
    private void MoveToNextInStack(Vector3Int tile)
    {
        transform.position = Vector2.MoveTowards(transform.position, walls.GetCellCenterWorld(tile), step);
    }

    //checks if we have reached next tile in the walking stack
    private bool ReachedGoal(Vector3Int goal)
    {
        return (Vector2.Distance(walls.GetCellCenterWorld(goal), transform.position) < 0.05f);
    }

    //On body collision with the player, not cone, we kill the player and return to random movement
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.GetComponent<PlayerBehaviour>().Die();
            walkingStack.Clear();
            goal = new Vector3Int(0, 0, 15);
            ReturnToRandomMovement();
            
            GameObject[] AllGuards = GameObject.FindGameObjectsWithTag("Guard");
            foreach (GameObject guard in AllGuards)
            {
                //only for the other guards than myself
                if (gameObject != guard)
                {
                    guard.SendMessage("ReturnToRandomMovement");
                }
            }
        }

        //not used for the moment
        if (collision.gameObject.CompareTag("Guard"))
        { 
            if(currentState == State.randomMoving)
            {
                currentWalkingDirection = FindOppositeDirection(currentWalkingDirection);
                ChangeLookOrientation();
            }
        }
    }

    //returns the guard to state random movement
    public void ReturnToRandomMovement() {
        if(currentState != State.randomMoving)
        {
            ChangeState(State.randomMoving);
            ChooseNewWalkingDirection();
        }
        walkingStack.Clear();
        goal = new Vector3Int(0, 0, 15);
    }

    //If something is inside the cone object that represents the vision of the guard
    private void OnTriggerStay2D(Collider2D collision)
    {
        //if it is a player
        if (collision.gameObject.CompareTag("Player") && reachedNewTile)
        {
            Vector3 playerPos = collision.gameObject.transform.position;
            if (currentState == State.pathFinding )
            {
                walkingStack.Clear();
                walkingStack = PathFinder(walls.WorldToCell(playerPos));
                stackUpdated = true;
                //debugging purpose
                //PrintStack(walkingStack);
            }

            if (currentState == State.randomMoving)
            {
                ChangeState(State.pathFinding);
                walkingStack = PathFinder(walls.WorldToCell(playerPos));
            }

            if (reachedNewTile) 
            {
                GameObject[] AllGuards = GameObject.FindGameObjectsWithTag("Guard");
                foreach (GameObject guard in AllGuards)
                {

                    //only for the other guards than myself
                    if (gameObject != guard)
                    {
                        guard.SendMessage("AnotherGuardFoundPlayer", walls.WorldToCell(playerPos));
                    }

                }

                Stack<walkingDirection> directions = new Stack<walkingDirection>();
                foreach(walkingDirection direction in Enum.GetValues(typeof(walkingDirection)))
                {
                    directions.Push(direction);
                }

                // Calls each guard that is not this object
                foreach(GameObject guard in AllGuards)
                {
                    if (gameObject != guard)
                    {
                        if (directions.Count != 0)
                        {
                            walkingDirection currentDir = directions.Pop();
                            if (CanMoveForward(walls.WorldToCell(playerPos), currentDir))
                            {
                                Tuple<Vector3Int, walkingDirection> informationToGuard = new Tuple<Vector3Int, walkingDirection>(walls.WorldToCell(playerPos), currentDir);
                                guard.SendMessage("AnotherGuardFoundPlayer", informationToGuard);
                                continue;
                            }
                        }
                        else
                        {
                            guard.SendMessage("AnotherGuardFoundPlayer", walls.WorldToCell(playerPos));
                        }
                    }
                }
            }
        }
    }

    // Called with a goal next to player and player position, used to tell that player is to be seen as a wall
    public void AnotherGuardFoundPlayer(Tuple<Vector3Int, walkingDirection> information)
    {
        Vector3Int playerPosition = information.Item1;
        walkingDirection directionGiven = information.Item2;

        if (currentState != State.pathFinding)
        {
            Vector3Int goalNextToPlayer = GetNextCell(playerPosition, directionGiven);
            if (currentState != State.lookForPlayer)
                ChangeState(State.lookForPlayer);

            walkingStack.Clear();
            walkingStack = PathFinder(goalNextToPlayer, playerPosition);
            stackUpdated = true;
        }
    }

    // Will set the goal next to the player that is closest to the guard in euclidean distance
    public void AnotherGuardFoundPlayer(Vector3Int playerPosition)
    {
        //Vector3Int goalNextToPlayer = playerPosition;

        //float closestDistance = float.MaxValue;
        if (currentState != State.pathFinding)
        {
            if (currentState != State.lookForPlayer)
                ChangeState(State.lookForPlayer);

            // Code to set goal to a position next to the player that is closest to this guard
           /* foreach (walkingDirection direction in Enum.GetValues(typeof(walkingDirection)))
            {
                Vector3Int neighbor = GetNextCell(playerPosition, direction);
                if (walls.GetTile(neighbor) == null)
                {
                    if (Vector3.Distance(transform.position, neighbor) < closestDistance)
                    {
                        goalNextToPlayer = neighbor;
                        closestDistance = Vector3.Distance(transform.position, neighbor);
                    }
                }
            }*/
            walkingStack.Clear();
            //walkingStack = PathFinder(playerPosition, playerPosition);
            walkingStack = PathFinder(playerPosition);
            stackUpdated = true;
        }
    }

    // *** tilemap function usages ***

    // Will check if this gameobject has reached a new tile by comparing current and previous tile in tilemap coordinates
    private bool CheckIfReachedNewTile()
    {
        if (!walls.WorldToCell(transform.position).Equals(previousTile))
            return true;
        return false;
    }

    // Gets the coordinates of the next cell in cell coordinates of the tilemap
    private Vector3Int GetNextCell(Vector3 currentPos, walkingDirection direction)
    {
        Vector3 nextPosition = currentPos + (Vector3)ReturnDirectionAsVector(direction) * walls.cellSize.x; //Blir nextPosition verkligen rätt? Kan vi råka hamna i samma tile som vi står i?
        return walls.WorldToCell(nextPosition);
    }

    // Makes a call to trail manager to get the trail level in the tile at tilePos
    private float GetTrailInTile(Vector3Int tilePos)
    {
        return tm.GetTrailLevel(tilePos);
    }

    // Check if there is a wall in the current walking direction from current position.
    private bool CanMoveForward()
    {
        if (walls.GetTile(GetNextCell(transform.position, currentWalkingDirection)) != null)
        {
            return false;
        }
        return true;
    }

    // Overloaded which takes a position and a direction (used for pathfinding)
    // Check if there is a wall in the next tile in the passed direction by comparing with the walls tilemap
    private bool CanMoveForward(Vector3Int position, walkingDirection direction)
    {
        if (walls.GetTile(GetNextCell(walls.GetCellCenterWorld(position), direction)) != null) //We check the walls if there is a tile in the position
        {
            return false; // Can't move forward because there is a tile in walls
        }
        return true;
    }

    // Will get a new direction based on how much trail is in the tile in each direction and most likely choose one with the lowest trail
    private walkingDirection GetRandomIndexBasedOnWeighting(List<walkingDirection> directions)
    {
        List<Tuple<walkingDirection, float>> chance = new List<Tuple<walkingDirection, float>>(); //will store the directions to choose from
        float totalWeight = 0;
        foreach (var dir in directions)
        {
            float tileTrail = GetTrailInTile(GetNextCell(transform.position, dir));
            float weightTile = CalculateWeight(tileTrail);

            //All the neighboring tiles but the latest walked on (with weight = 0)
            if (Mathf.Abs(weightTile) > float.Epsilon)
            {
                //Add to possible directions to walk
                chance.Add(new Tuple<walkingDirection, float>(dir, weightTile));
                totalWeight += weightTile;
            }
        }

        // When only one of the possible directions are walkable
        if (chance.Count == 1)
        {
            // Return that direction
            return chance[0].Item1;
        }
        // If there are no possible directions we can always walk back
        if (chance.Count == 0)
        {
            return FindOppositeDirection(currentWalkingDirection); 
        }

        float lowestWeight = 0f;
        List<walkingDirection> test = new List<walkingDirection>(); //only stores the directions that has the absolutely lowest trail
        foreach (var entry in chance)
        { 
            if (entry.Item2 > lowestWeight) {
                lowestWeight = entry.Item2;
                test.Clear();
                test.Add(entry.Item1); 
            }
            else if (entry.Item2 == lowestWeight) 
                test.Add(entry.Item1);
        }
        int select = UnityEngine.Random.Range(0, test.Count); //if counter = 0 -> 0 <= select < 1 -> 0
        return test[select];
    }

    // Returns the weight in a tile, 1 is no weight, 0 is max weight
    private float CalculateWeight(float trailLevel)
    {
        return 1 - (trailLevel / tm.GetMaxTrail());
    }

    // Checks possible directions and calls the random indexing function to decide which direction to choose
    private void ChooseNewWalkingDirection()
    {
        List<walkingDirection> directionsToChoseFrom = GetPossibleDirections(transform.position);
        if (directionsToChoseFrom.Count > 0)
            currentWalkingDirection = GetRandomIndexBasedOnWeighting(directionsToChoseFrom);

        transform.rotation = Quaternion.LookRotation(Vector3.forward, new Vector3(ReturnDirectionAsVector(currentWalkingDirection).x, ReturnDirectionAsVector(currentWalkingDirection).y, 1f));
    }

    // Compares the number of possible directions between last tile and current tile
    //the character stands on, if current has more we have found an interseciton.
    // Will also check if the number of directions are the same but different directions -> also an intersection.
    private bool FoundAnIntersectionTileMap()
    {
        List<walkingDirection> thisTileDir = GetPossibleDirections(transform.position);
        List<walkingDirection> previousTileDir = GetPossibleDirections(GetNextCell(transform.position, FindOppositeDirection(currentWalkingDirection)));

        if (thisTileDir.Count != previousTileDir.Count && previousTileDir.Count > 1)
        {
            return true;
        }
        else if (thisTileDir.Count == previousTileDir.Count && !thisTileDir.SequenceEqual(previousTileDir))
        {
            return true;
        }

        return false;
    }

    // Will return wether we are close enough to the center of a tile or not
    // This is to prevent running into corners when changing direction
    private bool ReachedTileCenter()
    {
        if (Vector2.Distance(walls.GetCellCenterWorld(walls.WorldToCell(transform.position)), transform.position) < size.x * 0.05)
        {
            return true;
        }
        return false;
    }

    // Will check next tile if it exists, otherwise when it has reached close enough to the cells center
    // we check for a new direction
    private void WalkUsingMoveTowards()
    {
        if (!ReachedTileCenter() || CanMoveForward())
        {
            transform.position = Vector2.MoveTowards(transform.position, walls.GetCellCenterWorld(GetNextCell(transform.position, currentWalkingDirection)), step);
        }
        else if (ReachedTileCenter() && reachedNewTile)
        {
            reachedNewTile = false;
            walls.WorldToCell(transform.position);
            ChooseNewWalkingDirection();
        }
    }

    // Check all possible directions to walk in
    // Returns a list with all directions
    private List<walkingDirection> GetPossibleDirections(Vector3 position)
    {
        List<walkingDirection> result = new List<walkingDirection>();
        for (int i = 0; i < numDirections; ++i)
        {
            if (walls.GetTile(GetNextCell(position, (walkingDirection)i)) == null)
            {
                result.Add((walkingDirection)i);
            }
        }
        return result;
    }

    // We use A* pathfinding, playerPos is optional and if it is provided it means that the player should be accounted for as a wall
    private Stack<Vector3Int> PathFinder(Vector3Int targetTile, Vector3Int? playerPos = null)
    {
        //dictionaries for fast look-up
        Dictionary<Vector3Int, Tuple<int, Vector3Int, int>> open = new Dictionary<Vector3Int, Tuple<int, Vector3Int, int>>(); //list of tiles (cell pos) to explore. Key=cellens pos. Value= Tuple(total manhattandistance value, parent, manhattan distance to target)
        Dictionary<Vector3Int, Tuple<int, Vector3Int, int>> closed = new Dictionary<Vector3Int, Tuple<int, Vector3Int, int>>(); //list of tiles (cell pos) already explored

        Vector3Int startTile = walls.WorldToCell(transform.position); //startTile in cellcoord.
        open.Add(startTile, new Tuple<int, Vector3Int, int>(ManhattanDistance(startTile, targetTile, startTile), Vector3Int.zero, ManhattanDistance(startTile, targetTile, startTile)));

        Vector3Int current = startTile;
        while (!current.Equals(targetTile))
        {
            //stores the KeyValuePair with the lowest f-cost in open
            KeyValuePair<Vector3Int, Tuple<int, Vector3Int, int>> lowest = new KeyValuePair<Vector3Int, Tuple<int, Vector3Int, int>>(Vector3Int.zero, new Tuple<int, Vector3Int, int>(int.MaxValue, Vector3Int.zero, int.MaxValue));
            foreach (KeyValuePair<Vector3Int, Tuple<int, Vector3Int, int>> kvp in open)
            {
                if (kvp.Value.Item1 < lowest.Value.Item1)
                {
                    lowest = kvp;
                }
                else if (kvp.Value.Item1 == lowest.Value.Item1)
                {
                    if (kvp.Value.Item3 < lowest.Value.Item3)
                    {
                        lowest = kvp;
                    } 
                }
            }
            current = lowest.Key;
            Vector3Int parentTMP = Vector3Int.zero;
            //if open contain current there is a path
            if (open.ContainsKey(current))
            {
                parentTMP = open[current].Item2;
            }
            else
            {
                // If open does not contain current then ther is no path, try without letting player be a wall.
                if(playerPos != null)
                    return PathFinder(playerPos.Value);
            }
            open.Remove(current); //current is visited, we remove it from open
            
            //if current is not in closed we add it to closed
            if (!closed.ContainsKey(current))
                closed.Add(current, new Tuple<int, Vector3Int, int>(ManhattanDistance(current, targetTile, startTile), parentTMP, ManhattanDistance(current, targetTile, current)));

            //if we have found the target we have found the path
            if (current.Equals(targetTile))
                break;

            for (int i = 0; i < numDirections; i++)
            {
                Vector3Int neighbor = GetNextCell(current, (walkingDirection)i);

                //if neighbor is not traversable OR in closed we skip this tile
                if (!CanMoveForward(current, (walkingDirection)i) || closed.ContainsKey(neighbor))
                {
                    continue;
                }
                //if playerpos is accounted for as a wall we skip that tile
                if(currentState == State.lookForPlayer && playerPos != null && current.Equals(playerPos))
                {
                    continue;
                }

                int distanceNeighbor = ManhattanDistance(neighbor, targetTile, startTile); //to only make one call
                int distanceNeighborToTarget = ManhattanDistance(neighbor, targetTile, neighbor); //to only make one call

                if (!open.ContainsKey(neighbor))
                {
                    open.Add(neighbor, new Tuple<int, Vector3Int, int>(distanceNeighbor, current, distanceNeighborToTarget));
                }
                else if (distanceNeighbor < open[neighbor].Item1)
                {
                    open[neighbor] = new Tuple<int, Vector3Int, int>(distanceNeighbor, current, distanceNeighborToTarget);
                }
            }
        }
        Vector3Int temp = current;
        Stack<Vector3Int> path = new Stack<Vector3Int>();

        while (temp != startTile)
        {
            if (!path.Contains(temp))
            { 
                path.Push(temp);
                // Draw the intended path
                /*if(currentState == State.lookForPlayer)
                {
                    walkingPath.SetTileFlags(temp, TileFlags.None);
                    walkingPath.SetColor(temp, Color.cyan);
                    walkingPath.SetTileFlags(temp, TileFlags.LockColor);
                }*/
            }
            temp = closed[temp].Item2; //Steps in the path by going to parent of temp tile
        }
        return path;
    }

    // Sums the manhattan distance currentPos->targetPos and currentPos->startPos in the tilemap and returns
    // If currentPos and startPos is the same the manhattan distance returned is currentPos->targetPos
    private int ManhattanDistance(Vector3Int currentPos, Vector3Int targetPos, Vector3Int startPos)
    {
        int currentToTarget = Mathf.Abs(currentPos.x - targetPos.x) + Mathf.Abs(currentPos.y - targetPos.y);
        int currentToStart = Mathf.Abs(currentPos.x - startPos.x) + Mathf.Abs(currentPos.y - startPos.y);
        return currentToStart + currentToTarget;
    }



    //not so advanced -------------------------------------------------------------------------------------------------
    
    // Changes the state to passed state and could be used for extra state specific functionality
    private void ChangeState(State nxtState)
    {
        currentState = nxtState;
        switch (nxtState)
        {
            case State.lookForPlayer:
                GetComponent<SpriteRenderer>().color = Color.yellow;
                break;
            case State.pathFinding:
                GetComponent<SpriteRenderer>().color = Color.red;
                break;
            case State.randomMoving:
                break;
            default:
                break;
        }
    }
    
    // Returns the direction that is opposite of the passed direction
    private walkingDirection FindOppositeDirection(walkingDirection current)
    {
        // a really cool switch expression in c#, works for enum among others
        return current switch
        {
            walkingDirection.up => walkingDirection.down,
            walkingDirection.right => walkingDirection.left,
            walkingDirection.down => walkingDirection.up,
            walkingDirection.left => walkingDirection.right,
            _ => walkingDirection.up, //default
        };
    }

    // Converts passed direction current to it representing Vector2 format and returns it
    private Vector2 ReturnDirectionAsVector(walkingDirection current)
    {
        // a really cool switch expression in c#
        return current switch
        {
            walkingDirection.up => new Vector2(0f, 1f),
            walkingDirection.right => new Vector2(1f, 0f),
            walkingDirection.down => new Vector2(0f, -1f),
            walkingDirection.left => new Vector2(-1f, 0f),
            _ => Vector2.zero, //default
        };
    }

    // Will rotate the guard to look towards its goal tile
    private void ChangeLookOrientation()
    {
        transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.Normalize(goal - previousTile));
    }
}