﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class PathRequestManager : MonoBehaviour {

    //Queue<PathRequest> pathRequestQueue = new Queue<PathRequest>();
    //PathRequest currentPathRequest;

    Queue<PathResult> results = new Queue<PathResult>();

    static PathRequestManager instance;
    Pathfiding pathfinding;

    //bool isProcessingPath;

    private void Awake()
    {
        instance = this;
        pathfinding = GetComponent<Pathfiding>();
    }

    private void Update()
    {
        if(results.Count > 0)
        {
            int itemsInQueue = results.Count;

            lock(results)
            {
                for(int i =0; i<itemsInQueue; i++)
                {
                    PathResult result = results.Dequeue();

                    result.callback(result.path, result.success);
                }
            }
        }
    }

    public static void RequestPath(PathRequest request)
    {
        ThreadStart threadeStart = delegate
        {
            instance.pathfinding.FindPath(request, instance.FinishedProcessingPath);
        };

        threadeStart.Invoke();
        //PathRequest newRequest = new PathRequest(pathStart, pathEnd, callback);

        //instance.pathRequestQueue.Enqueue(newRequest);
        //instance.TryProcessNext();
    }

    //void TryProcessNext()
    //{
    //    if(!isProcessingPath && pathRequestQueue.Count > 0)
    //    {
    //        currentPathRequest = pathRequestQueue.Dequeue();
    //        isProcessingPath = true;
    //        pathfinding.StartFindPath(currentPathRequest.pathStart, currentPathRequest.pathEnd);
    //    }
    //}

    //public void FinishedProcessingPath(Vector3[] path, bool success, PathRequest originalRequest)
    public void FinishedProcessingPath(PathResult result)
    {
        //currentPathRequest.callback(path, success);
        //isProcessingPath = false;
        //TryProcessNext();
        
        //PathResult result = new PathResult(path, success, originalRequest.callback);
        //originalRequest.callback(path, success);

        lock(results)
        {
            results.Enqueue(result);
        }
        
    }



    //struct PathRequest
    //{
    //    public Vector3 pathStart;
    //    public Vector3 pathEnd;
    //    public Action<Vector3[], bool> callback;

    //    public PathRequest(Vector3 _start, Vector3 _end, Action<Vector3[], bool> _callback)
    //    {
    //        pathStart = _start;
    //        pathEnd = _end;
    //        callback = _callback;
    //    }
    //}
}

public struct PathResult
{
    public Vector3[] path;
    public bool success;
    public Action<Vector3[], bool> callback;

    public PathResult(Vector3[] path, bool success, Action<Vector3[], bool> _callback)
    {
        this.path = path;
        this.success = success;
        this.callback = _callback;
    }
}

public struct PathRequest
{
    public Vector3 pathStart;
    public Vector3 pathEnd;
    public Action<Vector3[], bool> callback;

    public PathRequest(Vector3 _start, Vector3 _end, Action<Vector3[], bool> _callback)
    {
        pathStart = _start;
        pathEnd = _end;
        callback = _callback;
    }
}