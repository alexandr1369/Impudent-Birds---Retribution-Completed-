using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgainButton : InteractableButton
{
    protected override void Perform()
    {
        // restart current scene
        GameManager.instance.ReloadCurrentScene();
    }
}
