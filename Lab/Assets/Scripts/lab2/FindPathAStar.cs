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
    public GameObject playerPrefab;
    private GameObject activePlayer;

    PathMarker startNode;
    PathMarker goalNode;
    PathMarker lastPos;
    bool done = false;
    bool hasStarted = false;

    List<PathMarker> open = new List<PathMarker>();
    List<PathMarker> closed = new List<PathMarker>();
    List<PathMarker> path = new List<PathMarker>();

    void RemoveAllMarkers() {
        GameObject[] markers = GameObject.FindGameObjectsWithTag("marker");
        foreach (GameObject m in markers) Destroy(m);

        GameObject goal = GameObject.FindGameObjectWithTag("Finish");
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (goal != null) Destroy(goal);
        if (player != null) Destroy(player);
        if (activePlayer != null) Destroy(activePlayer);
    }

    void BeginSearch() {
        done = false;
        hasStarted = false;
        open.Clear();
        closed.Clear();
        path.Clear();
        RemoveAllMarkers();

        List<MapLocation> locations = new List<MapLocation>();
        for (int z = 1; z < maze.depth - 1; ++z) {
            for (int x = 1; x < maze.width - 1; ++x) {
                if (maze.map[x, z] != 1) {
                    locations.Add(new MapLocation(x, z));
                }
            }
        }
        locations.Shuffle();

        Vector3 startLocation = new Vector3(1 * maze.scale, 0.5f, 1 * maze.scale);
        startNode = new PathMarker(new MapLocation(1, 1), 0.0f, 0.0f, 0.0f, 
            Instantiate(start, startLocation, Quaternion.identity), null);

        int ex = Random.Range(5, maze.width - 1);
        int ez = Random.Range(5, maze.depth - 1);
        while(maze.map[ex, ez] == 1) { 
            ex = Random.Range(5, maze.width - 1);
            ez = Random.Range(5, maze.depth - 1);
        }

        Vector3 endLocation = new Vector3(ex * maze.scale, 0.5f, ez * maze.scale);
        goalNode = new PathMarker(new MapLocation(ex, ez), 0.0f, 0.0f, 0.0f, 
            Instantiate(end, endLocation, Quaternion.identity), null);

        open.Add(startNode);
        lastPos = startNode;
    }

    void Search(PathMarker thisNode) {
        if (thisNode.Equals(goalNode)) {
            done = true;
            return;
        }

        foreach (MapLocation dir in maze.directions) {
            MapLocation neighbour = dir + thisNode.location;

            if (neighbour.x < 1 || neighbour.x >= maze.width || neighbour.z < 1 || neighbour.z >= maze.depth) continue;
            if (maze.map[neighbour.x, neighbour.z] == 1) continue;
            if (IsClosed(neighbour)) continue;

            float g = Vector2.Distance(thisNode.location.ToVector(), neighbour.ToVector()) + thisNode.G;
            float h = Vector2.Distance(neighbour.ToVector(), goalNode.location.ToVector());
            float f = g + h;

            GameObject pathBlock = Instantiate(pathP, new Vector3(neighbour.x * maze.scale, 0.0f, neighbour.z * maze.scale), Quaternion.identity);
            
            if (!UpdateMarker(neighbour, g, h, f, thisNode)) {
                open.Add(new PathMarker(neighbour, g, h, f, pathBlock, thisNode));
            }
        }

        if (open.Count == 0) return;

        open = open.OrderBy(p => p.F).ToList<PathMarker>();
        PathMarker pm = open[0];
        closed.Add(pm);
        open.RemoveAt(0);

        if (pm.marker != null)
            pm.marker.GetComponent<Renderer>().material = closedMaterial;

        lastPos = pm;
    }

    bool UpdateMarker(MapLocation pos, float g, float h, float f, PathMarker prt) {
        foreach (PathMarker p in open) {
            if (p.location.Equals(pos)) {
                if (g < p.G) {
                    p.G = g;
                    p.F = f;
                    p.parent = prt;
                    return true;
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
            StopAllCoroutines();
            BeginSearch();
            hasStarted = true;
            StartCoroutine(Searching());
        }
    }

    IEnumerator Searching() {
        while (!done && open.Count > 0) {
            Search(lastPos);
            yield return new WaitForSeconds(0.01f);
        }

        if (done) {
            ReconstructPath();
            StartCoroutine(FollowPath()); // Start de player beweging
        }
    }

    void ReconstructPath() {
        PathMarker p = lastPos;
        while (p != null) {
            path.Insert(0, p);
            if (p.marker != null) {
                p.marker.GetComponent<Renderer>().material.color = Color.blue;
                p.marker.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
            }
            p = p.parent;
        }
    }

    IEnumerator FollowPath() {
        Vector3 startPos = new Vector3(startNode.location.x * maze.scale, 0.5f, startNode.location.z * maze.scale);
        activePlayer = Instantiate(playerPrefab, startPos, Quaternion.identity);

        foreach (PathMarker p in path) {
            Vector3 targetPos = new Vector3(p.location.x * maze.scale, 0.5f, p.location.z * maze.scale);
            
            while (Vector3.Distance(activePlayer.transform.position, targetPos) > 0.05f) {
                activePlayer.transform.position = Vector3.MoveTowards(
                    activePlayer.transform.position, 
                    targetPos, 
                    Time.deltaTime * 5f
                );
                yield return null; 
            }
        }
        Debug.Log("Player has reached the finish!");
    }
}

public static class ListExtensions {
    public static void Shuffle<T>(this IList<T> list) {
        System.Random rnd = new System.Random();
        int n = list.Count;
        while (n > 1) {
            n--;
            int k = rnd.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}