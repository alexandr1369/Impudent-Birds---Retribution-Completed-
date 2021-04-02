using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayButton : InteractableButton
{
    protected override void Perform()
    {
        // move to 'Battle'
        GameManager.instance.LoadScene(SceneType.Battle);
    }
}
