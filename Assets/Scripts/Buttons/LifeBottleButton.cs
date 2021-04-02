using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LifeBottleButton : InteractableButton
{
    [SerializeField]
    private int price; // booster price

    [SerializeField]
    private int lifesReward; // lifes reward (+N lifes)

    [SerializeField]
    private float cooldown; // init cooldown (between purchasing)
    private float currentCooldown = 0; // current cooldown

    [SerializeField]
    private GameObject defIcon; // default icon (getting width and height and image filling)

    protected override bool UpdateAction()
    {
        // check for cooldown
        if (currentCooldown > 0)
        {
            currentCooldown -= Time.deltaTime;
            defIcon.GetComponent<Image>().fillAmount = 1 - (currentCooldown / cooldown);
            return false;
        }

        return true;
    }
    protected override void Perform()
    {
        if(GameManager.instance.LifesAmount < GameManager.instance.StartLifesAmount)
        {
            if (GameManager.instance.RemoveCoins(price))
            {
                // set cooldown
                currentCooldown = cooldown;

                // add lifes
                GameManager.instance.GetDamage(-lifesReward);

                // play life gathering sound
                AudioClip lifeGatheringClip = Resources.Load<AudioClip>("Sounds/Heart");
                SoundManager.instance.PlaySingle(lifeGatheringClip);

                // toggle right action
                if (GameManager.instance.IsStageNumberSeven())
                    GameManager.instance.RightAction();
            }
        }
    }
}
