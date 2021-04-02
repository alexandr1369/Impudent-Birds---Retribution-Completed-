using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// square defence + unique action
public class Booster : Item
{
    [SerializeField]
    private float healthBarOffset; // health bar Y offset

    [SerializeField]
    private BoosterType boosterType; // booster type
    public BoosterType BoosterType { get => boosterType; }

    [SerializeField]
    private int health = 5; // health of current booster

    private List<Square> squares = new List<Square>(); // list of squares booster is on

    private GameObject boosterUI; // reference to UI version of current booster

    private HealthBar healthBar; // current health bar
    public HealthBar HealthBar { get => healthBar; }

    [SerializeField]
    private ParticleSystem ps; // particle system

    #region Idle Animation

    [SerializeField]
    private Animator animator; // main animator

    private float delay; // delay between 'idle' animations
    private float idleDuration; // 'idle' animation duration

    // 'mini mine' utility
    private float timeBeforeUniqueAction = 2f;
    private float timeLeftForUniqueAction = 0f; 

    private bool hasBeenPlaced = false; // placing state

    private void Start()
    {
        // check for 'toggle idle' boosters only
        if (boosterType == BoosterType.Barrier || boosterType == BoosterType.Fan ||
            boosterType == BoosterType.MiniMine || boosterType == BoosterType.Minefield) return;

        // init
        delay = Random.Range(2f, 5f);
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            if (clip.name.EndsWith("Idle"))
                idleDuration = clip.length;
    }
    private void Update()
    {
        // 'mini mine' unique action (getting coins)
        if(hasBeenPlaced && boosterType == BoosterType.MiniMine)
        {
            // get reward every 'timeBeforeUniqueAction' seconds
            if (timeLeftForUniqueAction <= 0)
            {
                GetCoins();
                timeLeftForUniqueAction = timeBeforeUniqueAction;
            }
            else
                timeLeftForUniqueAction -= Time.deltaTime;
        }

        // check for 'toggle idle' boosters only
        if (!hasBeenPlaced || boosterType == BoosterType.Barrier || boosterType == BoosterType.Fan ||
            boosterType == BoosterType.MiniMine || boosterType == BoosterType.Minefield) return;

        // check for toggling
        if (delay <= 0)
        {
            ToggleIdle();
            delay = idleDuration + Random.Range(4f, 7f);
        }
        // continue waiting
        else
        {
            delay -= Time.deltaTime;
        }
    }

    // toggle placing
    public void Place()
    {
        hasBeenPlaced = true;
        if (animator)
            animator.SetBool("Appearing", false);

        // unique utility actions
        if (boosterType == BoosterType.Fan)
        {
            // toggle 'Idle' animation
            ToggleIdle();

            // toggle air flows
            ToggleAirFlows(true);
        }
    }
    // toggle 'idle' animation
    private void ToggleIdle() => animator.SetTrigger("Idle");

    #endregion

    #region Utils

    public void OnTriggerEnter(Collider other)
    {
        // prevent bag with placing Booster with Enemy's landing at the same time
        if (!HealthBar) return;

        Enemy enemy = other.gameObject.GetComponent<Enemy>();
        if (enemy && !enemy.IsSelected())
            enemy.FreeSquare(true);
    }
    public void OnTriggerStay(Collider other)
    {
        // prevent bag with placing Booster with Enemy's landing at the same time
        if (!HealthBar) return;

        Enemy enemy = other.gameObject.GetComponent<Enemy>();
        if (enemy && !enemy.IsSelected())
            enemy.FreeSquare(true);
    }

    #endregion

    #region Health

    public void LoadHealthBar()
    {
        // add parent
        Transform enemyParentTransform = new GameObject().transform;
        enemyParentTransform.position = transform.position;
        enemyParentTransform.name = GetBoosterName(); // can add spaces between (???)
        transform.parent = enemyParentTransform;

        // load health bar
        GameObject healthBarPrefab = Resources.Load<GameObject>("Prefabs/Health Bar");
        healthBar = Instantiate(healthBarPrefab, transform.parent).GetComponent<HealthBar>();
        healthBar.transform.position = new Vector3(transform.position.x, transform.position.y + healthBarOffset, transform.position.z);
        healthBar.name = "Health Bar";

        // set health bar
        healthBar.SetHealth(health);
        healthBar.onHealthEnded += OnHealthEnded;
    }
    public void GetDamage(int damage) => healthBar.GetDamage(damage);
    // get booster name with spaces between words
    public string GetBoosterName()
    {
        string boosterName = string.Empty;
        switch (boosterType)
        {
            case BoosterType.Scarecrow: boosterName = "Scarecrow"; break;
            case BoosterType.HorizontalThorns: boosterName = "Horizontal Thorns"; break;
            case BoosterType.VerticalThorns: boosterName = "Vertical Thorns"; break;
            case BoosterType.PotatoThrower: boosterName = "Potato Thrower"; break;
            case BoosterType.Fan: boosterName = "Fan"; break;
            case BoosterType.MiniMine: boosterName = "Mini Mine"; break;
            case BoosterType.Minefield: boosterName = "Minefield"; break;
            case BoosterType.Barrier: boosterName = "Barrier"; break;
        }

        return boosterName;
    }

    #endregion

    // set current square(s)
    public void SetSquares(Square square) => squares.Add(square);
    public void SetSquares(List<Square> squares) => squares.ForEach(t => this.squares.Add(t));

    // set reference to booster (UI)
    public void SetBoosterUI(GameObject boosterUI) => this.boosterUI = boosterUI;

    // fade out when timer is ended
    private void OnHealthEnded()
    {
        // reset squares
        squares.ForEach(t => t.ResetSquare());

        // unique utility actions
        if (boosterType == BoosterType.Fan)
            ToggleAirFlows(false);

        // clear squares
        GridManager.instance.GetSquares(this);

        // destroy object(make fade out animation in future)
        if(transform.parent.gameObject)
            Destroy(transform.parent.gameObject);
        if(boosterUI)
            Destroy(boosterUI);
    }

    // 'mini mine' unique action
    private void GetCoins()
    {
        // reward will be 10 coins every 'timeBeforeUniqueAction' seconds
        int reward = 10;

        // add coins
        GameManager.instance.AddCoins(reward);

        // load reward info
        GameObject rewardBarPrefab = Resources.Load<GameObject>("Prefabs/Reward Bar");
        RewardBar rewardBar = Instantiate(rewardBarPrefab, transform.parent).GetComponent<RewardBar>();
        rewardBar.name = "Reward Bar";

        // show reward info
        rewardBar.ShowReward(reward);
    }
    // 'fan' unique action
    private void ToggleAirFlows(bool state)
    {
        if (state)
        {
            transform.GetComponentInChildren<ParticleSystem>().Play();
        }
        else
        {
            if (ps)
            {
                ps.Stop();
                ps.transform.parent = null;
                ps.transform.localScale = Vector3.one;
                Destroy(ps.gameObject, 5f);
            }
        }
    }
}
public enum BoosterType
{
    Scarecrow = 0,
    HorizontalThorns = 1,
    VerticalThorns = 2,
    PotatoThrower = 3,
    Fan = 4,
    MiniMine = 5,
    Minefield = 6,
    BattlefieldCleaner = 7,
    LifeBottle = 8,
    Barrier = 9
}

