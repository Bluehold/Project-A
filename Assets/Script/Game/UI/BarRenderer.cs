using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class BarRenderer : MonoBehaviour
{
    public Image BarImage;
    public float CurrentRatio = 1f;
    public Image BarDeltaImage;
    private float DeltaRatio;
    private float DeltaAlpha;
    public float DeltaFadeTime;
    public float DeltaKeepTime;
    private float DeltaRemainingTime;

    private void Awake()
    {
        ResetValue(0f);
    }

    private void LateUpdate()
    {
        BarImage.fillAmount = CurrentRatio;
        BarDeltaImage.fillAmount = DeltaRatio;

        // 바 깎였을 시 상태 컨트롤
        if (DeltaRemainingTime > 0f)
        {
            if (DeltaRemainingTime < DeltaFadeTime)
            {
                DeltaAlpha = DeltaRemainingTime / DeltaFadeTime;
                SetDeltaAlpha();
            }

            DeltaRemainingTime -= Time.deltaTime;
            if (DeltaRemainingTime < 0f)
                DeltaRemainingTime = 0f;
        }
    }

    public void SetValue(float value)
    {
        CurrentRatio = value;
    }

    public void ResetValue(float value)
    {
        CurrentRatio = value;
        DeltaRatio = value;
        DeltaAlpha = 1f;
        SetDeltaAlpha();
        DeltaRemainingTime = 0f;
    }

    public void AddBar(float value)
    {
        CurrentRatio += value;
        if (CurrentRatio > 1f)
            CurrentRatio = 1f;

        if (CurrentRatio > DeltaRatio)
            DeltaRatio = CurrentRatio;
    }

    public void DrainBar(float value)
    {
        //잘못된 값 시 정지
        if (value < 0f)
        {
            Debug.LogWarning("[DecreaseBar] Wrong Input Value (< 0f)");
            return;
        }

        //Overflow 방지
        if (value > CurrentRatio)
            value = CurrentRatio;

        // 게이지 깎기 동작
        if (DeltaRemainingTime == 0f) //새로 동작할 때 재설정
        {
            DeltaRatio = CurrentRatio;
        }

        CurrentRatio -= value;
        DeltaRemainingTime = DeltaKeepTime + DeltaFadeTime;
        DeltaAlpha = 1f;
        SetDeltaAlpha();
    }

    private void SetDeltaAlpha()
    {
        Color c = BarDeltaImage.color;
        c.a = DeltaAlpha;
        BarDeltaImage.color = c;
    }
}
