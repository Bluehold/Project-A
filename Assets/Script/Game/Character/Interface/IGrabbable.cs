using UnityEngine;

public interface IGrabbable
{
    // 현재 앞잡기 공격이 가능한 상태인지 확인
    bool CanGrab();

    // 앞잡기 시작
    void BeginGrab(Transform player);

    // 앞잡기 위치, 방향, 애니메이션 정보를 반환
    FrontGrabData GetGrabData();

    // 앞잡기 공격 실행
    void ExecuteGrabAttack(float damage);

    // 앞잡기 종료
    void EndGrab();

    // 현재 앞잡기 무적 상태인지 확인
    bool IsGrabInvincible();

    // 플레이어가 이동할 앞잡기 고정 위치 반환
    Transform GetGrabPoint();
}