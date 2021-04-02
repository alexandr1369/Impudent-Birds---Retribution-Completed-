using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleButton : InteractableButton
{
    [SerializeField]
    private GameObject panel; // panel to toggle

    [SerializeField]
    private bool hasPauseToggle = false; // has permission to toggle game pause

    protected override void Perform()
    {
        // toggle game pause
        if (hasPauseToggle)
            GameManager.instance.TogglePause();

        // toggle panel
        panel.SetActive(!panel.activeSelf);
    }
}
