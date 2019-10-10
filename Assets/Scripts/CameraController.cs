using UnityEngine;
using TouchScript.Gestures.TransformGestures;

public class CameraController : MonoBehaviour
{
    public ScreenTransformGesture TwoFingerMoveGesture;
    public ScreenTransformGesture ManipulationGesture;
    public float PanSpeed = 200f;
    public float RotationSpeed = 200f;
    public float ZoomSpeed = 10f;

    public Transform _pivot;
    public Camera _cam;

    private const int DefaultSize = 5;
    private const int DefaultMinSize = 3;
    private const int DefaultMaxSize = 8;

    private void OnEnable()
    {
        TwoFingerMoveGesture.Transformed += twoFingerTransformHandler;
        ManipulationGesture.Transformed += manipulationTransformedHandler;
    }

    private void OnDisable()
    {
        TwoFingerMoveGesture.Transformed -= twoFingerTransformHandler;
        ManipulationGesture.Transformed -= manipulationTransformedHandler;
    }

    private void manipulationTransformedHandler(object sender, System.EventArgs e)
    {
        float size = _cam.orthographicSize - DefaultSize * (ManipulationGesture.DeltaScale - 1f) * ZoomSpeed;
        _cam.orthographicSize = Mathf.Clamp(size, DefaultMinSize, DefaultMaxSize);
    }

    private void twoFingerTransformHandler(object sender, System.EventArgs e)
    {
        _pivot.localPosition += _pivot.rotation * (-TwoFingerMoveGesture.DeltaPosition) * PanSpeed;
    }
}