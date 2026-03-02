using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // Zorg dat dit er staat voor TMP!

public class GoToWaypoint : MonoBehaviour {

    public WPManager wpManager;   
    public TMP_Dropdown startDropdown; 
    public TMP_Dropdown endDropdown;   
    
    public float speed = 5.0f;
    public float rotationSpeed = 2.0f;
    public float accuracy = 1.5f; // Iets ruimer voor soepeler rijden

    private List<Node> path = new List<Node>();
    private int currentNodeIndex = 0;

    // Roep dit aan via je Button "OnClick"
    public void StartPathfinding() {
        if (wpManager == null) return;

        int startIndex = startDropdown.value;
        int endIndex = endDropdown.value;

        GameObject startNodeObj = wpManager.waypoints[startIndex];
        GameObject endNodeObj = wpManager.waypoints[endIndex];

        if (wpManager.graph.AStar(startNodeObj, endNodeObj)) {
            path = wpManager.graph.pathList;
            currentNodeIndex = 0;
            Debug.Log("Route berekend! Aantal waypoints: " + path.Count);
        } else {
            Debug.LogWarning("Geen route gevonden tussen deze twee bomen.");
        }
    }

    void LateUpdate() {
        if (path == null || path.Count == 0 || currentNodeIndex >= path.Count) return;

        // Bepaal positie van huidige doelwit
        Vector3 targetPosition = path[currentNodeIndex].getID().transform.position;
        targetPosition.y = transform.position.y; // Houd de tank op de grond

        Vector3 direction = targetPosition - transform.position;

        // Draaien
        if (direction.magnitude > 0.1f) {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }

        // Rijden
        transform.Translate(0, 0, speed * Time.deltaTime);

        // Check of we er zijn
        if (Vector3.Distance(transform.position, targetPosition) < accuracy) {
            currentNodeIndex++;
        }
    }
}