using UnityEngine;

public interface IGrabbable
{
    bool CanGrab();

    void BeginGrab(Transform player);

    void ExecuteGrabAttack(float damage);

    void EndGrab();

    bool IsGrabInvincible();

    Transform GetGrabPoint();
}