using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid : MonoBehaviour {

    //public Transform player;
    public bool displayGridGizmo;
    public LayerMask unwalkableMask;
    public Vector2 gridWorldSize;
    public float nodeRadius;
    public TerrianType[] walkableRegions;
    public int obastacleProximityPenalty = 10;
    LayerMask walkeableMask;
    Dictionary<int, int> walkableRegionsDictionary = new Dictionary<int, int>();

    Node[,] grid;

    float nodeDiameter;
    int gridSizeX, gridSizeY;

    int penaltyMin = int.MaxValue;
    int penaltyMax = int.MinValue;

    void Awake()
    {
        nodeDiameter = nodeRadius * 2;
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);

        foreach(TerrianType region in walkableRegions)
        {
            walkeableMask.value |= region.terrainMask.value;
            walkableRegionsDictionary.Add((int)Mathf.Log(region.terrainMask.value, 2), region.terrainPenalty);
        }

        CreateGrid();
    }

    public int MaxSize
    {
        get
        {
            return gridSizeX * gridSizeY;
        }
    }

    void CreateGrid()
    {
        grid = new Node[gridSizeX, gridSizeY];
        Vector3 WorldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.forward * gridWorldSize.y / 2;

        for(int x = 0; x<gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector3 worldPoint = WorldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (y * nodeDiameter + nodeRadius);
                bool walkable = !(Physics.CheckSphere(worldPoint, nodeRadius, unwalkableMask));

                int movementPanalty = 0;

                // raycast
                //if(walkable)
                //{
                //    Ray ray = new Ray(worldPoint + Vector3.up * 50.0f, Vector3.down);
                //    RaycastHit hit;

                //    if(Physics.Raycast(ray, out hit, 100, walkeableMask))
                //    {
                //        walkableRegionsDictionary.TryGetValue(hit.collider.gameObject.layer, out movementPanalty);
                //    }
                //}

                Ray ray = new Ray(worldPoint + Vector3.up * 50.0f, Vector3.down);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, 100, walkeableMask))
                {
                    walkableRegionsDictionary.TryGetValue(hit.collider.gameObject.layer, out movementPanalty);
                }

                if (!walkable)
                {
                    movementPanalty += obastacleProximityPenalty;
                }

                grid[x, y] = new Node(walkable, worldPoint, x, y, movementPanalty);
            }
        }

        BlurPenaltyMap(3);
    }

    void BlurPenaltyMap(int blurSize)
    {
        int kernelSize = blurSize * 2 + 1;
        int kernelExtents = (kernelSize - 1) / 2;

        int[,] penaltiesHorizontalpass = new int[gridSizeX, gridSizeY];
        int[,] penaltiesVerticalpass = new int[gridSizeX, gridSizeY];

        for(int y = 0; y< gridSizeY; y++)
        {
            for(int x = -kernelExtents; x<= kernelExtents; x++)
            {
                int sampleX = Mathf.Clamp(x, 0, kernelExtents);
                penaltiesHorizontalpass[0, y] += grid[sampleX, y].movementPanalty;

            }

            for(int x = 1; x< gridSizeX; x++)
            {
                int removeIndex = Mathf.Clamp(x - kernelExtents - 1, 0, gridSizeX);
                int addIndex = Mathf.Clamp(x + kernelExtents, 0, gridSizeX - 1);

                penaltiesHorizontalpass[x, y] = penaltiesHorizontalpass[x - 1, y] - grid[removeIndex, y].movementPanalty + grid[addIndex, y].movementPanalty;
            }
        }

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = -kernelExtents; y <= kernelExtents; y++)
            {
                int sampleY = Mathf.Clamp(y, 0, kernelExtents);
                penaltiesVerticalpass[x, 0] += penaltiesHorizontalpass[x, sampleY];

            }

            int blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalpass[x, 0] / (kernelSize * kernelSize));

            grid[x, 0].movementPanalty = blurredPenalty;

            for (int y = 1; y < gridSizeY; y++)
            {
                int removeIndex = Mathf.Clamp(y - kernelExtents - 1, 0, gridSizeY);
                int addIndex = Mathf.Clamp(y + kernelExtents, 0, gridSizeY - 1);

                penaltiesVerticalpass[x, y] = penaltiesVerticalpass[x, y-1] - penaltiesHorizontalpass[x, removeIndex] + penaltiesHorizontalpass[x, addIndex];

                blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalpass[x, y] / (kernelSize * kernelSize));

                grid[x, y].movementPanalty = blurredPenalty;

                if(blurredPenalty > penaltyMax)
                {
                    penaltyMax = blurredPenalty;
                }
                
                if(blurredPenalty < penaltyMin)
                {
                    penaltyMin = blurredPenalty;
                }
            }
        }
    }

    public List<Node> GetNeighbours(Node node)
    {
        List<Node> neighbours = new List<Node>();

        for(int x = -1; x<= 1; x++)
        {
            for(int y = -1; y<= 1; y++)
            {
                if(x == 0 && y == 0)
                {
                    continue;
                }

                int checkX = node.gridX + x;
                int checkY = node.gridY + y;

                if(checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
                {
                    neighbours.Add(grid[checkX, checkY]);
                }


            }
        }

        return neighbours;
    }

    public Node NodeFromWorldPoint(Vector3 worldPosition)
    {
        float percentX = (worldPosition.x + gridWorldSize.x / 2) / gridWorldSize.x;
        float percentY = (worldPosition.z + gridWorldSize.y / 2) / gridWorldSize.y;

        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);

        return grid[x, y];
    }

    //public List<Node> path;

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1, gridWorldSize.y));

        //if(onlyDisplayPathGizmo)
        //{
        //    if(path != null)
        //    {
        //        foreach(Node n in path)
        //        {
        //            Gizmos.color = Color.black;
        //            Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter - .1f));
        //        }
        //    }
        //}

        if (grid != null && displayGridGizmo)
        {
            //Node playerNode = NodeFromWorldPoint(player.position);

            foreach(Node n in grid)
            {
                Gizmos.color = Color.Lerp(Color.white, Color.black, Mathf.InverseLerp(penaltyMin, penaltyMax, n.movementPanalty));
                Gizmos.color = (n.walkabls) ? Gizmos.color : Color.red;

                //if(path != null)
                //{
                //    if(path.Contains(n))
                //    {
                //        Gizmos.color = Color.black;
                //    }
                //}
                //if(playerNode == n)
                //{
                //    Gizmos.color = Color.green;
                //}

                Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter));
            }
        }
    }

    [System.Serializable]
    public class TerrianType
    {
        public LayerMask terrainMask;
        public int terrainPenalty;
    }
}
