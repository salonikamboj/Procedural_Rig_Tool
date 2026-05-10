using UnityEngine;

public class FollowConstraint : MonoBehaviour
{
    public Transform target;

    [Range(0f, 1f)]
    public float positionWeight = 1f;

    [Range(0f, 1f)]
    public float rotationWeight = 1f;

    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;

    private void Awake()
    {
        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Blend between original pose and control pose using weights
        transform.position = Vector3.Lerp(
            transform.position,
            target.position,
            positionWeight
        );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            target.rotation,
            rotationWeight
        );
    }
}