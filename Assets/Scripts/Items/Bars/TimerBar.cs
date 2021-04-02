using UnityEngine;
using Pixelplacement;
using System;
using UnityEngine.UI;

public class TimerBar : MonoBehaviour
{
    [SerializeField]
    private bool isNegative = false; // check for pos/neg type

    // ending state
    public bool IsEnding { get => isEnding; }
    private bool isEnding;

    public Action onTimerEnded; // delegate for 'timer ended' event

    private float duration; // total duration

    [SerializeField]
    private Image background; // bg image
    private Image backgroundUI; // bg image (UI)

    [SerializeField]
    private Image foreground; // fg image
    private Image foregroundUI; // fg image (UI)
    private Material foregroundMaterial; // fg material

    private void Update()
    {
        transform.LookAt(transform.position + Camera.main.transform.forward);
    }

    // start timer with fade in animation
    public void StartTimer(float duration)
    {
        // set duration
        this.duration = duration;

        // start init
        string timerMaterialName = isNegative ? "Timer 2 Material" : "Timer 1 Material";
        foregroundMaterial = Instantiate<Material>(Resources.Load<Material>($"Materials/" + timerMaterialName));
        foreground.material = foregroundUI.material = foregroundMaterial;

        // timer animation
        Tween.Value(1f, 0, (float t) => { if (t <= .15f && !isEnding) StopTimer(); foreground.fillAmount = foregroundUI.fillAmount = t; }, this.duration, 0);

        // show timer
        background.color = backgroundUI.color = new Color32(0, 0, 0, 200);
        if (foregroundMaterial.HasProperty("TimerAlpha"))
            foregroundMaterial.SetFloat("TimerAlpha", 1f);
    }
    // stop timer with fade out animation
    public void StopTimer()
    {
        if (isEnding) return;

        // toggle ending state
        isEnding = true;

        // hide timer
        background.color = backgroundUI.color = new Color32(0, 0, 0, 0);
        if (foregroundMaterial.HasProperty("TimerAlpha"))
            foregroundMaterial.SetFloat("TimerAlpha", 0);

        // inform enemy about timer ending
        onTimerEnded();
    }
    // set current 2D version of enemy's timer bar
    public void SetReferenceToUI(Image backgroundUI, Image foregroundUI)
    {
        this.foregroundUI = foregroundUI;
        this.backgroundUI = backgroundUI;
    }
}
