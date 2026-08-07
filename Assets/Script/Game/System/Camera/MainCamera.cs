using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

public class MainCamera : MonoBehaviour
{
    [System.Serializable]
    public struct MinMax
    {
        public float Min;
        public float Max;
    }

    [field: SerializeField]
    public Transform FocusObject { get; private set; }
    private Vector3 FocusPos;

    public Vector2 Angle;
    public Vector3 Offset;
    public float DefaultDistance;
    private float CurrentDistance;
    private Vector3 DistanceOffset;
    private Quaternion Rotation;
    private Vector3 CameraVelocity;

    public float CameraDragSensitivity;
    [SerializeField]
    private float CameraRadius;
    [SerializeField]
    private LayerMask CameraBlockMask;
    [SerializeField]
    private MinMax CameraXCap;
    [SerializeField]
    private float CameraZoomSensitivity;
    [SerializeField]
    private MinMax CameraZoomCap;
    private float CameraZoomLevel;

    public float GetRotationY() => Angle.y;

    public void SetFocusObject(Transform obj) => FocusObject = obj;

    private void OnDrawGizmos()
    {
        DrawCameraGizmos();
    }

    private void DrawCameraGizmos()
    {
        FocusPos = FocusObject.position + GetRotation(Angle) * Offset;

        Vector3 direction = GetDirection(Angle);

        Vector3 pos = GetDestination(direction, DefaultDistance);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(
            pos,
            FocusObject.position + GetRotation(Angle) * Offset
        );
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(pos, Vector3.one * 0.1f);
    }

    private void Start()
    {
        FocusPos = FocusObject.position + GetRotation(Angle) * Offset;
        CurrentDistance = DefaultDistance;
    }

    private void Update()
    {
        HandleCameraMoveInput();
        HandleCameraZoomInput();
    }

    private void HandleCameraMoveInput()
    {
        Vector2 look = InputManager.Instance.LookInput;
        Angle.y += look.x * CameraDragSensitivity;
        Angle.x -= look.y * CameraDragSensitivity;
        Angle.x = Mathf.Clamp(Angle.x, CameraXCap.Min, CameraXCap.Max);
    }

    private void HandleCameraZoomInput()
    {
        float scrollInput = InputManager.Instance.ScrollInput;

        if (scrollInput != 0)
        {
            CameraZoomLevel -= scrollInput;
            CurrentDistance = DefaultDistance * Mathf.Exp(CameraZoomLevel * CameraZoomSensitivity);
            if (CurrentDistance < CameraZoomCap.Min)
            {
                CurrentDistance = CameraZoomCap.Min;
                CameraZoomLevel += 1f;
            }
            else if (CurrentDistance > CameraZoomCap.Max)
            {
                CurrentDistance = CameraZoomCap.Max;
                CameraZoomLevel -= 1f;
            }
        }
    }

    private void LateUpdate()
    {
        SetPosition(Angle);
    }

    private void SetPosition(Vector2 angle)
    {
        SetFocusPosDamp();

        Vector3 direction = GetDirection(angle);

        float distance = GetObstructedDistance(FocusPos, direction);

        SetTransform(direction, distance, angle);
    }

    private void SetFocusPosDamp()
    {
        FocusPos = Vector3.SmoothDamp(FocusPos, FocusObject.position + GetRotation(Angle) * Offset, ref CameraVelocity, 0.2f);
    }

    private Quaternion GetRotation(Vector2 angle)
    {
        return Quaternion.Euler(angle.x, angle.y, 0f);
    }

    private Vector3 GetDirection(Vector2 angle)
    {
        return GetRotation(angle) * Vector3.back;
    }

    private float GetObstructedDistance(Vector3 focusPos, Vector3 dir)
    {
        float dist = CurrentDistance;
        
        RaycastHit[] hits = Physics.SphereCastAll(
            focusPos,
            CameraRadius,
            dir,
            CurrentDistance,
            CameraBlockMask
        );

        foreach (var hit in hits)
        {
            if (hit.collider.isTrigger == true)
                continue;
            
            if (hit.collider.transform == FocusObject.parent)
                continue;

            if (hit.collider.transform.IsChildOf(FocusObject))
                continue;

            dist = Mathf.Min(dist, hit.distance);
        }

        return dist;
    }

    private void SetTransform(Vector3 direction, float distance, Vector2 angle)
    {
        transform.position = GetDestination(direction, distance);
        transform.rotation = GetRotation(angle);
    }

    private Vector3 GetDestination(Vector3 direction, float distance)
    {
        return FocusPos + direction * distance;
    }
}
