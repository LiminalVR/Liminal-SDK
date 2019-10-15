using UnityEngine;

public class Racquet : MonoBehaviour
{
    public Transform Target;
    public Rigidbody RigidBody;

    public Vector3 RotationOffset;

    public void FixedUpdate()
    {
        RigidBody.MovePosition(Target.position);
        RigidBody.MoveRotation(Target.rotation * Quaternion.Euler(RotationOffset));
    }
}