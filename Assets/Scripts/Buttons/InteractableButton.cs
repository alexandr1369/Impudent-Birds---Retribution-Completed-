using UnityEngine;
using UnityEngine.EventSystems;

public class InteractableButton : MonoBehaviour, IPointerUpHandler, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    // states toggles
    public bool isAnimated = true;
    public bool isInteractable = true;
    public bool isClickSoundOn = true;

    protected Animator animator; // animator

    // click sounds
    protected AudioClip btnDownAudioClip;
    protected AudioClip btnUpAudioClip;

    // animation utils
    private bool isReady;
    private bool isPointerUp = true;
    protected bool isPointerUpon;
    protected bool isToggleNeeded;

    // move utils
    protected Vector2 downPosition;
    protected Vector2 upPosition;

    void Start()
    {
        animator = GetComponent<Animator>();
        btnUpAudioClip = Resources.Load<AudioClip>("Sounds/Btn Up");
        btnDownAudioClip = Resources.Load<AudioClip>("Sounds/Btn Down");
    }

    void Update()
    {
        // buttons with update action utility
        if (!UpdateAction())
        {
            if (isAnimated)
                isAnimated = false;
        }
        else
        {
            if (!isAnimated)
                isAnimated = true;
        }

        // unpress action
        if (isToggleNeeded && isPointerUp)
        {
            isToggleNeeded = false;
            animator.SetTrigger("Unpressed");
        }

        // press action
        if (isReady && isPointerUp)
        {
            // check for interactivity
            if (!isInteractable)
                return;

            // override button logic by changing only this method
            Perform();

            // button releasing
            isReady = false;
        }
    }

    /// <summary>
    /// Main logic of button work.
    /// </summary>
    protected virtual void Perform() { }
    protected virtual bool UpdateAction() => true;

    #region OnPointerUtils

    public void OnPointerUp(PointerEventData eventData)
    {
        // get mouse up position
        upPosition = Input.mousePosition;

        // mouse release if it's still upon the button
        if (isPointerUpon)
            isPointerUp = true;

        // check whether button is animated or not
        if (isAnimated)
            animator.SetTrigger("PressedReverse");
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        // utils
        if (!UpdateAction() || GameManager.instance.IsTutorial && !GameManager.instance.IsStageNumberSeven()) return;

        // mouse click
        isPointerUp = false;

        // check whether button in animated or not
        if (isAnimated)
        {
            // begin animation
            animator.SetTrigger("Pressed");
        }
        else
        {
            // run without animation
            isReady = true;
        } 

        // get mouse down position
        downPosition = Input.mousePosition;
    }
    public void OnPointerEnter(PointerEventData eventData) => isPointerUpon = true;
    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerUpon = false;
        if(isPointerUp && isAnimated)
            animator.ResetTrigger("PressedReverse");
    }

    #endregion

    #region AnimatorUtils

    protected virtual void PressedStart() { }
    protected virtual void UnpressedStart() { }
    protected void Pressed()
    {
        if(btnDownAudioClip != null)
        {
            if(isClickSoundOn)
                SoundManager.instance.PlaySingle(btnDownAudioClip);
        }
        isToggleNeeded = true;
    }
    protected void Unpressed()
    {
        if (btnUpAudioClip != null)
        {
            if(isClickSoundOn)
                SoundManager.instance.PlaySingle(btnUpAudioClip);
        }
    }
    protected void SetPanel()
    {
        isReady = true;
    }

    #endregion
}
