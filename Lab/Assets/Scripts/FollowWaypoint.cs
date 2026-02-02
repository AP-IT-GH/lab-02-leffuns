using UnityEngine;

public sealed class FollowWaypoint : MonoBehaviour
{
    [Header("Movement Settings")]
    public GameObject[] waypoints;
    public float speed = 5.0f;
    public float rotSpeed = 2.0f;
    public float waypointThreshold = 2.0f; // Distance to trigger next waypoint

    private int currentWaypoint = 0;

    void Update()
    {
        // Safety check to ensure waypoints exist
        if (waypoints.Length == 0) return;

        MoveTowardsWaypoint();
    }

    void MoveTowardsWaypoint()
    {
        // 1. Calculate direction to the target waypoint
        Vector3 direction = waypoints[currentWaypoint].transform.position - transform.position;
        
        // 2. Smooth Rotation logic (Quaternion.Slerp)
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotSpeed);
        }

        // 3. Forward Movement
        transform.Translate(0, 0, speed * Time.deltaTime);

        // 4. Distance Check: Switch to next waypoint if close enough
        if (direction.magnitude < waypointThreshold)
        {
            currentWaypoint++;

            // Loop back to the first waypoint if the end is reached
            if (currentWaypoint >= waypoints.Length)
            {
                currentWaypoint = 0;
            }
        }
    }
}