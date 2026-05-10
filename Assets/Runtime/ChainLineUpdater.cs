using UnityEngine;
using System.Collections.Generic;

public class ChainLineUpdater : MonoBehaviour
{
    public List<Transform> joints = new List<Transform>();
    public LineRenderer lr;

    private void LateUpdate()
    {
        if (lr == null || joints.Count == 0) return;

        lr.positionCount = joints.Count;

        for (int i = 0; i < joints.Count; i++)
        {
            if (joints[i] != null)
                lr.SetPosition(i, joints[i].position);
        }
    }
}