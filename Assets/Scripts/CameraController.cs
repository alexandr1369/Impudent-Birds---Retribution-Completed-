using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Pixelplacement;
using Pixelplacement.TweenSystem;
using TMPro;

[RequireComponent(typeof(Animator))]
public class CameraController : MonoBehaviour
{
    #region Singleton

    public static CameraController instance;
    private void Awake()
    {
        instance = this;
    }

    #endregion

    [SerializeField]
    private Camera mainCamera; // main camera
    [SerializeField]
    private Animator mainCameraAnimator;
    [SerializeField]
    private Camera gameFieldCamera; // game field camera (UI)
    [SerializeField]
    private Animator gameFieldCameraAnimator;

    [SerializeField]
    private TextMeshProUGUI score; // score amount

    #region Zooming

    // zoom availability states
    private bool canZoom = false; 
    public bool CanOnlyZoomIn { get; set; }
    public bool CanOnlyZoomOut { get; set; }

    public float lastDeltaDistance = 0; // last delta distance between touches

    private bool hasZoomedFar = false; // birds' culling mask toggle
    private bool hasZoomedFarBeyond = false; // 2d game field toggle 

    [SerializeField]
    private Vector3 defaultRewardBarLocalScale; // reward bar utility

    // current zoom [0; 1]
    private float currentZoom = 0f;
    public float CurrentZoom { get => currentZoom; }

    private TweenBase timerTween; // timer tween utility

    // zooming utility
    private float maxZoomSwipesDistance = Screen.width * 2 / 3; // max distance between 2 swipes's positions for currentZoom == 1

    #endregion

    private void Update()
    {
        if (Input.touchSupported)
        {
            // check for zooming
            if (canZoom && Input.touchCount == 2)
            {
                Touch touch1, touch2;
                Vector2 oldTouchPosition1, oldTouchPosition2;
                float currentTouchesDistance, oldTouchesDistance, deltaDistance;

                // get current touches
                touch1 = Input.GetTouch(0);
                touch2 = Input.GetTouch(1);

                // get touches's positions from previous frame
                oldTouchPosition1 = touch1.position - touch1.deltaPosition;
                oldTouchPosition2 = touch2.position - touch2.deltaPosition;

                // get distance between old and current touches
                oldTouchesDistance = Vector2.Distance(oldTouchPosition1, oldTouchPosition2);
                currentTouchesDistance = Vector2.Distance(touch1.position, touch2.position);

                // zoom offset value between old and current touches
                lastDeltaDistance = deltaDistance = (currentTouchesDistance - oldTouchesDistance) / maxZoomSwipesDistance;

                // check for only zoom in
                if(deltaDistance >= 0)
                {
                    if (CanOnlyZoomOut && !CanOnlyZoomIn)
                        return;
                }
                // check for only zoom out
                else
                {
                    if (CanOnlyZoomIn && !CanOnlyZoomOut)
                        return;
                }

                // zoom
                Zoom(deltaDistance);
            }
        }

        // check for 2d game field swapping
        if (currentZoom != 1 && hasZoomedFarBeyond)
        {
            // toggle zoom
            hasZoomedFarBeyond = false;

            // show 3d elements
            mainCamera.cullingMask ^= 1 << LayerMask.NameToLayer("Default");
            mainCamera.cullingMask ^= 1 << LayerMask.NameToLayer("Enemy");
            mainCamera.cullingMask ^= 1 << LayerMask.NameToLayer("Dropped Enemy");

            // toggle 2d game field canvas
            gameFieldCamera.cullingMask ^= 1 << LayerMask.NameToLayer("Game Field (2D)");
        }
        else if (currentZoom == 1 && !hasZoomedFarBeyond)
        {
            // toggle zoom
            hasZoomedFarBeyond = true;

            // hide 3d elements
            mainCamera.cullingMask ^= 1 << LayerMask.NameToLayer("Default");
            mainCamera.cullingMask ^= 1 << LayerMask.NameToLayer("Enemy");
            mainCamera.cullingMask ^= 1 << LayerMask.NameToLayer("Dropped Enemy");

            // toggle 2d game field canvas
            gameFieldCamera.cullingMask ^= 1 << LayerMask.NameToLayer("Game Field (2D)");
        }

        // check for birds visibility toggle
        if (currentZoom >= .5f && !hasZoomedFar)
        {
            // make timer transparent
            if (timerTween != null && timerTween.Status == Tween.TweenStatus.Running)
                timerTween.Stop();
            timerTween = Tween.Value(score.color.a, .35f, (t) => { score.color = new Color(score.color.r, score.color.g, score.color.b, t); }, .1f * score.color.a, 0);

            // toggle zoom
            hasZoomedFar = true;

            // hide birds (in moving state)
            mainCamera.cullingMask ^= 1 << LayerMask.NameToLayer("Bird");
        }
        else if (currentZoom < .5f && hasZoomedFar)
        {
            // make timer transparent
            if (timerTween != null && timerTween.Status == Tween.TweenStatus.Running)
                timerTween.Stop();
            timerTween = Tween.Value(score.color.a, 1f, (t) => { score.color = new Color(score.color.r, score.color.g, score.color.b, t); }, .1f * (.35f / score.color.a), 0);

            // toggle zoom
            hasZoomedFar = false;

            // show birds (in moving state)
            mainCamera.cullingMask ^= 1 << LayerMask.NameToLayer("Bird");
        }

        // change reward bar scale with camera zooming
        foreach (GameObject rewardBar in GameObject.FindGameObjectsWithTag("RewardBar"))
            rewardBar.transform.localScale = defaultRewardBarLocalScale * (1 - currentZoom / 3);
    }

    // camera zooming toggle
    public void AllowCameraZooming() => canZoom = true;
    // zoom (in/out)
    private void Zoom(float deltaTouchDistance)
    {
        currentZoom = Mathf.Clamp(currentZoom + deltaTouchDistance, 0, 1f);
        mainCameraAnimator.Play("CameraZoom", 0, currentZoom);
        gameFieldCameraAnimator.Play("CameraZoom", 0, currentZoom);
    }

    // DEMO -> changed to swipes
    private void OnValueChanged(Slider slider)
    {
        // get last delta distance
        lastDeltaDistance = currentZoom < slider.value ? 1 : (float)-1;

        // zooming
        currentZoom = slider.value;
        mainCameraAnimator.Play("CameraZoom", 0, currentZoom);
        gameFieldCameraAnimator.Play("CameraZoom", 0, currentZoom);
    }
}
public enum SwipeDirection
{
    Left = 0,
    Right = 1
}
