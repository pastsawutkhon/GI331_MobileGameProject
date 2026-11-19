using UnityEngine;
using Unity.Services.LevelPlay;
using System;

public class AdsSample : MonoBehaviour
{
    private LevelPlayRewardedAd rewardedVideoAd;
    bool isAdsEnabled = false;

    private Action onRewardedCallback;
    private Action onClosedCallback;

    // ตัวแปรรับค่าข้าม Thread เพื่อสั่งอัปเดต UI บน Main Thread ได้ชัวร์
    private bool pendingReward = false;
    private bool pendingClose = false;

    public void Start()
    {
        LevelPlay.ValidateIntegration();

        LevelPlay.OnInitSuccess += SdkInitCompletedEvent;
        LevelPlay.OnInitFailed += SdkInitFailedEvent;

        LevelPlay.Init(AdsCofig.AppKey);
    }

    private void Update()
    {
        if (pendingReward)
        {
            pendingReward = false;
            onRewardedCallback?.Invoke();
        }

        if (pendingClose)
        {
            pendingClose = false;
            var closed = onClosedCallback;
            onRewardedCallback = null;
            onClosedCallback = null;
            closed?.Invoke();
            rewardedVideoAd?.LoadAd();
        }
    }

    private void SdkInitFailedEvent(LevelPlayInitError error)
    {
        Debug.Log($"Ads not enable : {error}");
    }

    private void SdkInitCompletedEvent(LevelPlayConfiguration configuration)
    {
        EnableAds();
        isAdsEnabled = true;
    }

    void EnableAds()
    {
        rewardedVideoAd = new LevelPlayRewardedAd(AdsCofig.RewardVedioUnityId);

        // ดูโฆษณาจบ -> ตั้งธงให้ Update() แจกรางวัล
        rewardedVideoAd.OnAdRewarded += (info, reward) => pendingReward = true;

        // ปิดโฆษณา หรือแสดงไม่สำเร็จ -> ตั้งธงให้ Update() รีเซ็ตและโหลดใหม่
        rewardedVideoAd.OnAdClosed += info => pendingClose = true;
        rewardedVideoAd.OnAdDisplayFailed += (info, error) => pendingClose = true;

        rewardedVideoAd.LoadAd();
    }

    public bool ShowRewarded(Action onRewarded, Action onClosed = null)
    {
        if (rewardedVideoAd == null || !rewardedVideoAd.IsAdReady())
            return false;

        onRewardedCallback = onRewarded;
        onClosedCallback = onClosed;

        rewardedVideoAd.ShowAd();

        return true;
    }

    public void LoadRewardedVideoAds()
    {
        rewardedVideoAd?.LoadAd();
    }

    public void ShowRewardedVideoAds()
    {
        if (rewardedVideoAd != null && rewardedVideoAd.IsAdReady())
        {
            rewardedVideoAd.ShowAd();
        }
        else
        {
            Debug.Log("[LevelPlaySample] LevelPlay Rewarded Video Ad is not ready");
        }
    }
}