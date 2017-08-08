using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node {
    public bool walkabls;
    public Vector3 worldPosition;

    public Node(bool _walkable, Vector3 _worldPos)
    {
        walkabls = _walkable;
        worldPosition = _worldPos;
    }
}
