using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.InputSystem;

public class PathMarker {
    public MapLocation location;
    public float G, H, F;
    public GameObject marker;
    public PathMarker parent;

    public PathMarker(MapLocation l, float g, float h, float f, GameObject m, PathMarker p) {
        location = l;
        G = g;
        H = h;
        F = f;
        marker = m;
        parent = p;
    }

    public override bool Equals(object obj) {
        if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            return false;
        else
            return location.Equals(((PathMarker)obj).location);
    }
}

public class FindPathAStar : MonoBehaviour {
    public Maze maze;
    public Material closedMaterial;
    public Material openMaterial;
    public GameObject start;
    public GameObject end;
    public GameObject pathP;

    PathMarker startNode;
    PathMarker goalNode;
    PathMarker lastPos;
    bool done = false;
    bool hasStarted = false;

    List<PathMarker> open = new List<PathMarker>();
    List<PathMarker> closed = new List<PathMarker>();

    void RemoveAllMarkers() {
        GameObject[] markers = GameObject.FindGameObjectsWithTag("marker");
        foreach (GameObject m in markers) Destroy(m);
        // Clean up any goal/player objects if they exist
        GameObject goal = GameObject.FindGameObjectWithTag("Finish");
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) Destroy(player);
    }

    void BeginSearch() {
        done = false;
        RemoveAllMarkers();

        List<MapLocation> locations = new List<MapLocation>();
        for (int z = 1; z < maze.depth - 1; ++z) {
            for (int x = 1; x < maze.width - 1; ++x) {
                if (maze.map[x, z] != 1) {
                    locations.Add(new MapLocation(x, z));
                }
            }
        }

        // Minimal Shuffle Fix
        for (int i = 0; i < locations.Count; i++) {
            MapLocation temp = locations[i];
            int randomIndex = Random.Range(i, locations.Count);
            locations[i] = locations[randomIndex];
            locations[randomIndex] = temp;
        }

        // Apply scale so they line up with the maze
        Vector3 startLocation = new Vector3(1 * maze.scale, 0.5f, 1 * maze.scale);
        startNode = new PathMarker(new MapLocation(1, 1),
            0.0f, 0.0f, 0.0f, Instantiate(start, startLocation, Quaternion.identity), null);

        MapLocation goalLoc = locations[0];
        Vector3 endLocation = new Vector3(goalLoc.x * maze.scale, 0.5f, goalLoc.z * maze.scale);
        goalNode = new PathMarker(goalLoc, 0.0f, 0.0f, 0.0f, Instantiate(end, endLocation, Quaternion.identity), null);

        open.Clear();
        closed.Clear();
        open.Add(startNode);
        lastPos = startNode;
    }

    void Search(PathMarker thisNode) {
        if (thisNode.location.Equals(goalNode.location)) {
            done = true;
            ReconstructPath();
            return;
        }

        foreach (MapLocation dir in maze.directions) {
            MapLocation neighbour = dir + thisNode.location;

            if (neighbour.x < 0 || neighbour.x >= maze.width || neighbour.z < 0 || neighbour.z >= maze.depth) continue;
            if (maze.map[neighbour.x, neighbour.z] == 1) continue;
            if (IsClosed(neighbour)) continue;

            float g = Vector2.Distance(thisNode.location.ToVector(), neighbour.ToVector()) + thisNode.G;
            float h = Vector2.Distance(neighbour.ToVector(), goalNode.location.ToVector());
            float f = g + h;

            if (!UpdateMarker(neighbour, g, h, f, thisNode)) {
                GameObject pathBlock = Instantiate(pathP, new Vector3(neighbour.x * maze.scale, 0.0f, neighbour.z * maze.scale), Quaternion.identity);
                open.Add(new PathMarker(neighbour, g, h, f, pathBlock, thisNode));
            }
        }

        if (open.Count > 0) {
            open = open.OrderBy(p => p.F).ToList<PathMarker>();
            PathMarker pm = open.ElementAt(0);
            closed.Add(pm);
            open.RemoveAt(0);
            lastPos = pm;
        }
    }

    bool UpdateMarker(MapLocation pos, float g, float h, float f, PathMarker prt) {
        foreach (PathMarker p in open) {
            if (p.location.Equals(pos)) {
                if (g < p.G) {
                    p.G = g; p.H = h; p.F = f; p.parent = prt;
                }
                return true;
            }
        }
        return false;
    }

    bool IsClosed(MapLocation marker) {
        foreach (PathMarker p in closed) {
            if (p.location.Equals(marker)) return true;
        }
        return false;
    }

    void Update() {
        if (Keyboard.current.pKey.wasPressedThisFrame) {
            BeginSearch();
            hasStarted = true;          
        }
        if (hasStarted && !done) {
            if (Keyboard.current.cKey.wasPressedThisFrame) Search(lastPos);
        }
    }

    void ReconstructPath() {
        PathMarker begin = lastPos;
        while (begin != null) {
            if (begin.marker != null)
                begin.marker.GetComponent<Renderer>().material.color = Color.blue;
            begin = begin.parent;
        }
    }
}