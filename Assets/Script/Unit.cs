using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour {

    const float minPathUpdateTime = 0.2f;
    const float patUpdateMoveThreshold = 0.5f;

    public Transform target;

    public float speed = 0.1f;
    public float turnDst = 5;
    public float TurnSpeed = 3;
    public float stoppingDst = 10;

    Path path;
    //Vector3[] path;
    //int targetIndex;

    private void Start()
    {
        StartCoroutine(UpdatePath());
        //PathRequestManager.RequestPath(transform.position, target.position, OnPathFound);
    }

    public void OnPathFound(Vector3[] wayPoints, bool pathSuccessful)
    {
        if(pathSuccessful)
        {
            path = new Path(wayPoints, transform.position, turnDst, stoppingDst);
            StopCoroutine("FollowPath");
            StartCoroutine("FollowPath");
        }
    }

    IEnumerator UpdatePath()
    {
        if(Time.timeSinceLevelLoad < 0.3f)
        {
            yield return new WaitForSeconds(0.3f);
        }

        //PathRequestManager.RequestPath(transform.position, target.position, OnPathFound);
        PathRequestManager.RequestPath(new PathRequest(transform.position, target.position, OnPathFound));

        float sqrMovethreshold = patUpdateMoveThreshold * patUpdateMoveThreshold;
        Vector3 targetPosOld = target.position;

        while (true)
        {
            yield return new WaitForSeconds(minPathUpdateTime);

            if ((target.position - targetPosOld).sqrMagnitude > sqrMovethreshold)
            {
                //PathRequestManager.RequestPath(transform.position, target.position, OnPathFound);
                PathRequestManager.RequestPath(new PathRequest(transform.position, target.position, OnPathFound));
                targetPosOld = target.position;
            }
            
            
        }
    }

    IEnumerator FollowPath()
    {
        //Vector3 currentWaypoint = path[0];

        //while(true)
        //{
        //    if(transform.position == currentWaypoint)
        //    {
        //        targetIndex++;

        //        if(targetIndex >= path.Length)
        //        {
        //            yield break;
        //        }

        //        currentWaypoint = path[targetIndex];
        //    }

        //    transform.position = Vector3.MoveTowards(transform.position, currentWaypoint, speed);
        //    yield return null;
        //}
        bool followingPath = true;
        int pathIndex = 0;

        transform.LookAt(path.lookPoints[0]);

        float speedPercent = 1;

        while (true)
        {

            Vector2 pos2D = new Vector2(transform.position.x, transform.position.z);

            while (path.turnBoundaries[pathIndex].HasCrossedLine(pos2D))
            {
                if(pathIndex == path.finishLineIndex)
                {
                    followingPath = false;
                    break;
                }
                else
                {
                    pathIndex++;
                }
            }

            if(followingPath)
            {
                if(pathIndex >= path.slowDownIndex && stoppingDst > 0)
                {
                    speedPercent = Mathf.Clamp01(path.turnBoundaries[path.finishLineIndex].DistanceFromPoint(pos2D) / stoppingDst);

                    if(speedPercent < 0.01f)
                    {
                        followingPath = false;
                    }
                }
                

                Quaternion targetRotation = Quaternion.LookRotation(path.lookPoints[pathIndex] - transform.position);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * TurnSpeed);
                transform.Translate(Vector3.forward * Time.deltaTime * speed * speedPercent, Space.Self);
            }
            yield return null;
        }
    }

    public void OnDrawGizmos()
    {
        if(path != null)
        {
            //for(int i = targetIndex; i < path.Length; i++)
            //{
            //    Gizmos.color = Color.black;
            //    Gizmos.DrawCube(path[i], Vector3.one);

            //    if(i == targetIndex)
            //    {
            //        Gizmos.DrawLine(transform.position, path[i]);
            //    }
            //    else
            //    {
            //        Gizmos.DrawLine(path[i - 1], path[i]);
            //    }
            //}
            path.DrawWithGizmos();
        }
    }
}
