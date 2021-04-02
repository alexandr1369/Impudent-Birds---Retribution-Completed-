using UnityEngine;
using Pixelplacement;
using TMPro;

public class EndGameRoundManager : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI scoreText; // current score text

    // new record
    [SerializeField]
    private GameObject newRecordPanel;
    [SerializeField]
    private TextMeshProUGUI newRecordText;
    [SerializeField]
    private AnimationCurve newRecordCurve;

    [SerializeField]
    private Camera cameraUI; // ui camera (bird visibility toggling)

    [SerializeField]
    private Animator tauntingBirdAnimator; // taunting bird

    // set current score
    public void SetScore(int score)
    {
        // get current score
        int lastScoreRecord = 0;
        if (PlayerPrefs.HasKey("ScoreRecord"))
            lastScoreRecord = PlayerPrefs.GetInt("ScoreRecord");

        // set current score text
        scoreText.text = score.ToString();

        // check current score for 'New Record'
        if (lastScoreRecord < score)
        {
            // save new record
            PlayerPrefs.SetInt("ScoreRecord", score);

            // show 'New Record' panel
            newRecordPanel.SetActive(true);

            // hide bird (UI)
            cameraUI.cullingMask ^= 1 << LayerMask.NameToLayer("Bird (UI)");

            // animate record amount [0-N]
            Tween.Value(0, score, (t) => newRecordText.text = t.ToString(), 5f, 0, newRecordCurve, obeyTimescale: false);
            Tween.LocalScale(newRecordText.transform, new Vector3(.5f, .5f, .5f), Vector3.one, 5f, 0, newRecordCurve, obeyTimescale: false);
        }
        else
        {
            // show advertisement
            AdsManager.instance.ShowInterstitialVideo();

            // start bird's animation
            StartTaunting();
        }
    }
    // button 'Continue' action (New Record)
    public void Continue()
    {
        // show advertisement
        AdsManager.instance.ShowInterstitialVideo();

        // hide 'New Record' panel
        newRecordPanel.SetActive(false);

        // show bird (UI)
        cameraUI.cullingMask ^= 1 << LayerMask.NameToLayer("Bird (UI)");

        // start bird's animation
        StartTaunting();
    }
    // 'Taunting Bird' utility
    private void StartTaunting()
    {
        string triggerName = Random.Range(0, 2) == 0 ? "Taunting1" : "Taunting2";
        tauntingBirdAnimator.SetTrigger(triggerName);
    }
}
