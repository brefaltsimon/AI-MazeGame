using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TrailManager : MonoBehaviour
{
    //All SerializeFields are set in the editor

    [SerializeField]
    private Tilemap trailMap; //where the trail is left

    [SerializeField]
    private float maxTrail; 

    [SerializeField]
    private Color maxTrailColor, minTrailColor, clearColor; //Color variables for different levels of trail

    //like a hash table, Vector3Int is a position in world coords, float is the trail value in the position
    private Dictionary<Vector3Int, float> trailTiles = new Dictionary<Vector3Int, float>();
    
    [SerializeField]
    private float testTrailAmount;

    [SerializeField]
    private float reduceAmount, reduceIntervall = 1f;

    private void Start()
    {
        StartCoroutine(ReduceTrailRoutine());
    }

    //for the guards to call, returns trail value in position key
    public float GetTrailLevel(Vector3Int key)
    {
        if (trailTiles.ContainsKey(key))
            return trailTiles[key];

        return 0f;
    }

    //self explanatory, returns the maximum possible trail
    public float GetMaxTrail()
    {
        return maxTrail;
    }

    //adds trailAmount to the trail at worldPos in the trailMap
    public void AddTrail(Vector2 worldPos, float trailAmount)
    {
        Vector3Int gridPos = trailMap.WorldToCell(worldPos);
        ChangeTrail(gridPos, trailAmount);
        VisualizeTrail();
    }

    // changes the trail by the changeBy variable at trailMap position gridPos, used to reduce trail and increase
    private void ChangeTrail(Vector3Int gridPos, float changeBy)
    {

        if (!trailTiles.ContainsKey(gridPos))
            trailTiles.Add(gridPos, 0f);

        float newValue = trailTiles[gridPos] + changeBy;

        if(newValue <= 0f)
        {
            trailTiles.Remove(gridPos);
            SetColor(gridPos, clearColor);
        }
        else
        {
            trailTiles[gridPos] = Mathf.Clamp(newValue, 0f, maxTrail); //ensure that the trail left isnt larger than max trail
        }
    }

    // constantly reduces trail at each tile in trailMap
    private IEnumerator ReduceTrailRoutine()
    {
        while(true)
        { 
            Dictionary<Vector3Int, float> trailTileCopy = new Dictionary<Vector3Int, float>(trailTiles);

            foreach(var entry in trailTileCopy)
            {
                ChangeTrail(entry.Key, reduceAmount);
            }
            VisualizeTrail();
            yield return new WaitForSeconds(reduceIntervall);
        }
    }

    //Visualizes the trail by setting the color in trailMap
    private void VisualizeTrail()
    {
        foreach(var entry in trailTiles)
        {
            float trailPercent = entry.Value / maxTrail;
            Color newTrailColor = maxTrailColor * trailPercent + minTrailColor * (1f - trailPercent);
            SetColor(entry.Key, newTrailColor);
        }
    }

    //actually sets the color in trailMap
    private void SetColor(Vector3Int key, Color clr)
    {
        trailMap.SetTileFlags(key, TileFlags.None);
        trailMap.SetColor(key, clr);
        trailMap.SetTileFlags(key, TileFlags.LockColor);
    }

    private void Update()
    {
        //used to debug, click and there will appear trail
/*        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            AddTrail(mousePos, testTrailAmount);
        }*/
    }
}
