using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // Verplicht voor TextMeshPro Dropdowns

[System.Serializable]
public struct Link {

    public enum direction {
        UNI,
        BI
    }

    public GameObject node1, node2;
    public direction dir;
}

public class WPManager : MonoBehaviour {

    public GameObject[] waypoints;
    public Link[] links;
    public Graph graph = new Graph();

    // UI Referenties (Nu met TMP_Dropdown)
    public TMP_Dropdown startDropdown;
    public TMP_Dropdown endDropdown;

    void Start() {
        // 1. Controleer of er waypoints zijn
        if (waypoints.Length > 0) {
            
            // 2. Voeg alle waypoints toe als Nodes in de graaf
            foreach (GameObject wp in waypoints) {
                graph.AddNode(wp);
            }

            // 3. Maak de verbindingen (Links) tussen de nodes
            foreach (Link l in links) {
                if (l.node1 != null && l.node2 != null) {
                    graph.AddEdge(l.node1, l.node2);
                    
                    // Bij BI (Bidirectioneel) voegen we ook de weg terug toe
                    if (l.dir == Link.direction.BI) {
                        graph.AddEdge(l.node2, l.node1);
                    }
                }
            }
        }

        // 4. Vul de Dropdowns automatisch met de namen van de palmbomen
        PopulateDropdowns();
    }

    void PopulateDropdowns() {
        if (startDropdown == null || endDropdown == null) {
            Debug.LogWarning("Dropdowns nog niet toegewezen in de Inspector op het WPManager object!");
            return;
        }

        // Maak de lijstjes eerst leeg
        startDropdown.ClearOptions();
        endDropdown.ClearOptions();

        List<string> options = new List<string>();

        // Loop door de waypoints en pak de namen van de GameObjects
        foreach (GameObject wp in waypoints) {
            if (wp != null) {
                options.Add(wp.name);
            }
        }

        // Voeg de namen toe aan de TMP Dropdowns
        startDropdown.AddOptions(options);
        endDropdown.AddOptions(options);
    }

    // Tekent lijnen in de Scene View zodat je de verbindingen kunt zien
    void OnDrawGizmos() {
        if (links == null || links.Length == 0) return;

        Gizmos.color = Color.cyan;
        foreach (Link l in links) {
            if (l.node1 != null && l.node2 != null) {
                Gizmos.DrawLine(l.node1.transform.position, l.node2.transform.position);
            }
        }
    }
}