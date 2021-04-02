using UnityEngine;
using TMPro;

public class RewardBar : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI value; // value text

    private void Update()
    {
        transform.LookAt(transform.position + Camera.main.transform.forward);
    }

    // show reward info
    public void ShowReward(int rewardAmount) => value.text = $"+{rewardAmount}";
    // destroy reward
    private void Destoy() => Destroy(gameObject);
}
