using UnityEngine;

// Simple player-side grab controller demonstrating use of IGrabbable only.
[DisallowMultipleComponent]
public class PlayerGrabController : MonoBehaviour
{
    [Header("Grab")]
    [SerializeField]
    private float GrabRange = 2f;

    [SerializeField]
    private LayerMask GrabMask = -1;

    [SerializeField]
    private Transform HoldPoint;

    private IGrabbable currentGrabbed;
    private Transform grabbedTransform;

    private void Update()
    {
        // Press G to attempt grab / release
        if (Input.GetKeyDown(KeyCode.G))
        {
            if (currentGrabbed == null)
            {
                TryGrab();
            }
            else
            {
                ReleaseGrab();
            }
        }

        // Press H to execute grab attack (deal damage)
        if (Input.GetKeyDown(KeyCode.H))
        {
            if (currentGrabbed != null)
            {
                currentGrabbed.ExecuteGrabAttack(20f);
            }
        }

        // If holding, update held transform position
        if (grabbedTransform != null && HoldPoint != null)
        {
            grabbedTransform.position = HoldPoint.position;
            grabbedTransform.rotation = HoldPoint.rotation;
        }
    }

    private void TryGrab()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, GrabRange, GrabMask);

        IGrabbable best = null;
        float bestDist = float.MaxValue;

        foreach (var h in hits)
        {
            // Use GetComponentInParent to find the interface on parents as well
            var g = h.GetComponentInParent<IGrabbable>();

            if (g == null) continue;

            // Check CanGrab via interface
            if (!g.CanGrab()) continue;

            var mb = g as MonoBehaviour;

            if (mb == null) continue;

            float d = Vector3.Distance(transform.position, mb.transform.position);

            if (d < bestDist)
            {
                best = g;
                bestDist = d;
            }
        }

        if (best != null)
        {
            var mb = best as MonoBehaviour;
            best.BeginGrab(transform);
            currentGrabbed = best;
            grabbedTransform = mb != null ? mb.transform : null;
            Debug.Log($"Grabbed: {mb?.name}");
        }
        else
        {
            Debug.Log("No grabbable found");
        }
    }

    private void ReleaseGrab()
    {
        if (currentGrabbed == null) return;

        currentGrabbed.EndGrab();
        currentGrabbed = null;
        grabbedTransform = null;
    }
}
