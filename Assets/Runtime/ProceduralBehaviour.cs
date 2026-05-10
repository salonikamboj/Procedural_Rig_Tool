using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ProceduralBehavior : MonoBehaviour
{
    [Header("Chain")]
    public List<Transform> joints = new List<Transform>();

    [Header("Wave Motion")]
    public bool enableWave = false;
    public float waveAmplitude = 0.5f;
    public float waveFrequency = 1f;
    public float wavePhaseOffset = 0.5f;
    public Vector3 waveAxis = Vector3.right;

    [Header("Smooth Follow / Lag")]
    public bool enableLag = false;
    [Range(0f, 1f)]
    public float lagAmount = 0.1f;

    [Header("Secondary Motion / Spring")]
    public bool enableSpring = false;
    public float springStiffness = 10f;
    public float springDamping = 0.8f;

    private Vector3[] restPositions;
    private Vector3[] currentVelocities;
    private Vector3[] lagPositions;

    // This flag means we havent captured rest positions yet
    private bool initialised = false;

    private void LateUpdate()
    {
        if (joints.Count == 0) return;

        if (!initialised)
        {
            CaptureRestPositions();
            initialised = true;
            return;
        }

        // Wave runs first — sets base position
        if (enableWave)
            ApplyWave();

        // Lag runs on top of wave
        if (enableLag)
            ApplyLag();

        // Spring runs last — adds bounce on top of everything
        if (enableSpring)
            ApplySpring();
    }

    private void CaptureRestPositions()
    {
        Debug.Log("Capturing rest positions for " + joints.Count + " joints.");

        restPositions = new Vector3[joints.Count];
        currentVelocities = new Vector3[joints.Count];
        lagPositions = new Vector3[joints.Count];

        for (int i = 0; i < joints.Count; i++)
        {
            restPositions[i] = joints[i].position;
            lagPositions[i] = joints[i].position;
            currentVelocities[i] = Vector3.zero;
        }

    }

    private void ApplyWave()
    {
        for (int i = 0; i < joints.Count; i++)
        {
            float phase = i * wavePhaseOffset;
            float sineValue = Mathf.Sin(Time.time * waveFrequency + phase);
            Vector3 waveOffset = waveAxis.normalized * sineValue * waveAmplitude;

            joints[i].position = restPositions[i] + waveOffset;

        }
    }
    private void ApplyLag()
    {
        lagPositions[0] = joints[0].position;

        for (int i = 1; i < joints.Count; i++)
        {
            lagPositions[i] = Vector3.Lerp(
                lagPositions[i],
                lagPositions[i - 1],
                1f - lagAmount
            );

            Vector3 dir = (lagPositions[i] - lagPositions[i - 1]).normalized;
            float boneLength = Vector3.Distance(
                restPositions[i],
                restPositions[i - 1]
            );

            lagPositions[i] = lagPositions[i - 1] + dir * boneLength;
            joints[i].position = lagPositions[i];
        }
    }

    private void ApplySpring()
    {
        for (int i = 1; i < joints.Count; i++)
        {
            Vector3 displacement = joints[i].position - restPositions[i];
            Vector3 springForce = -displacement * springStiffness;

            currentVelocities[i] += springForce * Time.deltaTime;

            float safeDamping = Mathf.Clamp(springDamping, 0.01f, 0.999f);
            currentVelocities[i] *= Mathf.Pow(safeDamping, Time.deltaTime * 60f);

            // Safety cap
            if (currentVelocities[i].magnitude > 100f)
                currentVelocities[i] = Vector3.zero;

            joints[i].position += currentVelocities[i] * Time.deltaTime;
        }
    }

    public void UpdateRestPositions()
    {
        initialised = false;
    }
}