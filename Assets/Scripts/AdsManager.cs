using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Advertisements;

public class AdsManager : MonoBehaviour
{
    #region Singleton

    public static AdsManager instance;
    private void Awake()
    {
        instance = this;
    }

    #endregion

    // test state
    private bool isTestMode = false;

    // game ID [android]
    private string gameId = "4011667";

    // placements IDs [android]
    private string interstitialPlacementId = "Interstitial_Android";
    private string rewardedPlacementId = "Rewarded_Android";
    private string bannerPlacementId = "Banner_Android";

    private IEnumerator Start()
    {
        // init
        Advertisement.Initialize(gameId, isTestMode);

        // wait until banner is ready
        if (!Advertisement.IsReady(bannerPlacementId))
            yield return null;

        // set banner position
        Advertisement.Banner.SetPosition(BannerPosition.TOP_RIGHT);

        // show banner
        //Advertisement.Banner.Show(bannerPlacementId);
    }

    // show interstitial video ad
    public void ShowInterstitialVideo()
    {
        if (Advertisement.IsReady(interstitialPlacementId))
        {
            Advertisement.Show(interstitialPlacementId);
        }
    }
}
