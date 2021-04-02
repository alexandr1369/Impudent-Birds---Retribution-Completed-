using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuButton : InteractableButton
{
    protected override void Perform()
    {
        // move to 'Menu'
        GameManager.instance.LoadScene(SceneType.Menu);
    }
}
