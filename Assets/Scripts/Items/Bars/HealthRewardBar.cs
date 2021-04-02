using UnityEngine;
using TMPro;

public class HealthRewardBar : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI value; // value text

    private void Update()
    {
        transform.LookAt(transform.position + Camera.main.transform.forward);
    }

    // show reward info
    public void ShowDamage(int damageAmount) => value.text = $"{(damageAmount >= 0? "-" : "+")}{Mathf.Abs(damageAmount)}";
    // destroy reward
    private void Destoy() => Destroy(gameObject);
}
