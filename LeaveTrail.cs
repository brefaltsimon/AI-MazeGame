using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeaveTrail : MonoBehaviour
{
    [SerializeField]
    private float leaveAmount, leaveIntervall = 1f; //how much trail is left and how often the trail is left/refreshed. Is set in the editor

    [SerializeField]
    private TrailManager tm; //object that handels the trail

    private void Awake()
    {
        tm = FindObjectOfType<TrailManager>();
    }

    private void Start()
    {
        StartCoroutine(LeaveRoutine());
    }

    private IEnumerator LeaveRoutine()
    {
        // the guards will constantly leave trail at their position with the interval decided
        while (true)
        {
            tm.AddTrail(transform.position, leaveAmount);
            yield return new WaitForSeconds(leaveIntervall);
        }
    }

}
