using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleBirdDanceButton : InteractableButton
{
    [SerializeField]
    private Animator birdAnimator;

    private string currentDanceNumber;

    protected override void Perform()
    {
        // get all dance trigger names
        List<string> danceNames = new List<string> { "Dance1", "Dance2", "Dance3" };

        // get new dance trigger name
        danceNames.Remove(currentDanceNumber);
        currentDanceNumber = danceNames[Random.Range(0, danceNames.Count)];

        // toggle dance
        if (birdAnimator)
            birdAnimator.SetTrigger(currentDanceNumber);
    }

    public void SetCurrentDanceName(string danceName) => currentDanceNumber = danceName;
}
