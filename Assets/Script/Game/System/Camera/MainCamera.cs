using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

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
    public float DefaultDistance;
    private float FinalDistance;
    public Vector3 Offset;
    private Quaternion rot;
    private Vector3 pos;
    private Vector3 vel;

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
        rot = Quaternion.Euler(Angle.x, Angle.y, 0f);
        Offset = rot * new Vector3(0f, 0f, -FinalDistance);
        pos = FocusObject.TransformPoint(Offset);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(pos, FocusObject.position);
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(pos, Vector3.one * 0.1f);
    }

    private void Start()
    {
        FocusPos = FocusObject.position;
        FinalDistance = DefaultDistance;
    }

    private void Update()
    {
        if (InputManager.Inputs.Player.CameraDrag.IsPressed())
        {
            Vector2 look = InputManager.Inputs.Player.Look.ReadValue<Vector2>();
            Angle.y += look.x * CameraDragSensitivity;
            Angle.x -= look.y * CameraDragSensitivity;
            Angle.x = Mathf.Clamp(Angle.x, CameraXCap.Min, CameraXCap.Max);
        }

        float scrollInput = InputManager.Inputs.Player.CameraZoom.ReadValue<float>();

        if (scrollInput != 0)
        {
            CameraZoomLevel -= scrollInput;
            FinalDistance = DefaultDistance * Mathf.Exp(CameraZoomLevel * CameraZoomSensitivity);
            if (FinalDistance < CameraZoomCap.Min)
            {
                FinalDistance = CameraZoomCap.Min;
                CameraZoomLevel += 1f;
            }
            else if (FinalDistance > CameraZoomCap.Max)
            {
                FinalDistance = CameraZoomCap.Max;
                CameraZoomLevel -= 1f;
            }
        }
    }

    private void LateUpdate()
    {
        FocusPos = Vector3.SmoothDamp(FocusPos, FocusObject.position, ref vel, 0.2f);

        rot = Quaternion.Euler(Angle.x, Angle.y, 0f);
        Offset = rot * new Vector3(0f, 0f, -FinalDistance);

        float dist = FinalDistance;
        Vector3 dir = Offset.normalized;
        RaycastHit[] hits = Physics.SphereCastAll(
            FocusPos,
            CameraRadius,
            dir,
            FinalDistance,
            CameraBlockMask
        );

        foreach (var hit in hits)
        {
            if (hit.transform.root == FocusObject.root)
                continue;

            dist = Mathf.Min(dist, hit.distance);
        }

        transform.position = FocusPos + dir * dist;
        transform.rotation = rot;
    }
}
