using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LanguageButton : InteractableButton
{
    [SerializeField]
    private Language language;

    protected override void Perform()
    {
        GameManager.instance.SetLanguage(language);
    }
}
