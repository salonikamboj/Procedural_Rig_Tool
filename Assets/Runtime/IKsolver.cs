using UnityEngine;
using System.Collections.Generic;

public class IKSolver : MonoBehaviour
{
    [Header("Chain")]
    public List<Transform> joints = new List<Transform>();

    [Header("IK Target")]
    public Transform ikTarget;

    [Header("Settings")]
    [Range(0f, 1f)]
    public float ikWeight = 1f;
    public int iterations = 10;
    public float tolerance = 0.01f;

    // Stores the original FK positions and rotations
    private Vector3[] fkPositions;
    private Quaternion[] fkRotations;

    // Stores bone lengths between each joint
    private float[] boneLengths;
    private float totalLength;

    private void Start()
    {
        Initialise();
    }

    public void Initialise()
    {
        if (joints.Count < 2) return;

        boneLengths = new float[joints.Count - 1];
        totalLength = 0f;

        for (int i = 0; i < joints.Count - 1; i++)
        {
            float len = Vector3.Distance(
                joints[i].position,
                joints[i + 1].position
            );

            // If two joints are at the same spot, give a default length
            if (len < 0.001f)
            {
                Debug.LogWarning("Joint " + joints[i].name +
                    " and " + joints[i + 1].name +
                    " are too close together. Setting default length of 1.");
                len = 1f;
            }

            boneLengths[i] = len;
            totalLength += len;
        }

        fkPositions = new Vector3[joints.Count];
        fkRotations = new Quaternion[joints.Count];
        StoreFKPose();
    }

    private void LateUpdate()
    {
        if (ikTarget == null || joints.Count == 0) return;

        StoreFKPose();

        if (ikWeight > 0f)
            SolveIK();
    }

    private void StoreFKPose()
    {
        for (int i = 0; i < joints.Count; i++)
        {
            fkPositions[i] = joints[i].position;
            fkRotations[i] = joints[i].rotation;
        }
    }

    private void SolveIK()
    {

        // If target is further than total chain length
        // just stretch the chain straight toward the target
        float distToTarget = Vector3.Distance(
            joints[0].position,
            ikTarget.position
        );

        // Working array — we solve into this, not directly into joints
        Vector3[] positions = new Vector3[joints.Count];
        for (int i = 0; i < joints.Count; i++)
            positions[i] = fkPositions[i];

        if (distToTarget >= totalLength)
        {
            // Stretch straight toward target
            Vector3 dir = (ikTarget.position - positions[0]).normalized;
            for (int i = 1; i < joints.Count; i++)
                positions[i] = positions[i - 1] + dir * boneLengths[i - 1];
        }
        else
        {
            // FABRIK algorithm
            // Run for a set number of iterations until close enough
            for (int iter = 0; iter < iterations; iter++)
            {
                // --- BACKWARD PASS ---
                // Pull end joint to target
                positions[positions.Length - 1] = ikTarget.position;

                // Walk backward up the chain
                for (int i = positions.Length - 2; i >= 0; i--)
                {
                    Vector3 dir = (positions[i] - positions[i + 1]).normalized;
                    positions[i] = positions[i + 1] + dir * boneLengths[i];
                }

                // --- FORWARD PASS ---
                // Put root back where it started
                positions[0] = fkPositions[0];

                // Walk forward down the chain
                for (int i = 1; i < positions.Length; i++)
                {
                    Vector3 dir = (positions[i] - positions[i - 1]).normalized;
                    positions[i] = positions[i - 1] + dir * boneLengths[i - 1];
                }

                // Stop early if end is close enough to target
                if (Vector3.Distance(positions[positions.Length - 1],
                    ikTarget.position) < tolerance)
                    break;
            }
        }

        // Apply positions — blend between FK and IK using ikWeight
        for (int i = 0; i < joints.Count; i++)
        {
            joints[i].position = Vector3.Lerp(
                fkPositions[i],
                positions[i],
                ikWeight
            );

            // Point each joint toward the next one
            if (i < joints.Count - 1)
            {
                Vector3 dir = joints[i + 1].position - joints[i].position;
                if (dir != Vector3.zero)
                {
                    Quaternion ikRot = Quaternion.LookRotation(dir);
                    joints[i].rotation = Quaternion.Slerp(
                        fkRotations[i],
                        ikRot,
                        ikWeight
                    );
                }
            }
        }
    }

    // Called from the editor tool to re-initialise after setup
    public void ForceInitialise()
    {
        Initialise();
    }
}