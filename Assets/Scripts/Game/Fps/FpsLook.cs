using UnityEngine;

public sealed class FpsLook : MonoBehaviour
{
    [SerializeField] Transform yawRoot;
    [SerializeField] Transform pitchPivot;
    [SerializeField] float yawSensitivity = 1.65f;
    [SerializeField] float pitchSensitivity = 1.65f;

    float _pitch;

    void LateUpdate()
    {
        if (yawRoot == null || pitchPivot == null)
            return;

        if (_pendingLook.sqrMagnitude < 1e-6f) return;
        var delta = _pendingLook;
        _pendingLook = Vector2.zero;

        yawRoot.Rotate(0f, delta.x * yawSensitivity, 0f);
        _pitch -= delta.y * pitchSensitivity;
        _pitch = Mathf.Clamp(_pitch, -89f, 89f);
        pitchPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    Vector2 _pendingLook;

    public void ConsumeLook(Vector2 frameDeltaPixels)
    {
        _pendingLook += frameDeltaPixels;
    }
}
