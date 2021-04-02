using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// square attack
[RequireComponent(typeof(Rigidbody))]
public class Enemy : Item
{
    #region tutorial utils

    public bool IsTutorial;
    private GameObject tutorialBar;
    public void LoadTutorialBar()
    {
        // load tutorial bar
        string tutorialBarName = enemyType == EnemyType.Bomb ? "Tutorial Not Select Bar" : "Tutorial Select Bar";
        GameObject tutorialBarPrefab = Resources.Load<GameObject>("Prefabs/" + tutorialBarName);
        tutorialBar = Instantiate(tutorialBarPrefab, transform.parent);
        tutorialBar.transform.position = new Vector3(transform.position.x, transform.position.y + 2, transform.position.z);
        tutorialBar.name = tutorialBarName;
    }

    #endregion

    private bool isSelected; // selected state(prevent multi-click on square)

    [SerializeField]
    private bool isSurpriseBox; // is from surprise box

    [SerializeField]
    private float duration = 2f; // duration of current enemy
    [SerializeField]
    private EnemyType enemyType; // enemy type

    [SerializeField]
    private int damage; // damage (lifes)
    [SerializeField]
    private int reward; // reward (coins)
    [SerializeField]
    private int score; // score (amount)

    private float chanceForSound = 15f; // chance to play spawn sound

    private Vector3 squarePosition; // square position

    private GameObject enemyUI; // reference to UI version of current enemy

    private TimerBar timerBar; // timer bar

    private new Rigidbody rigidbody; // enemy rigidbody

    private void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
    }

    // get selected state
    public bool IsSelected() => isSelected;
    // get enemty type
    public EnemyType GetEnemyType() => enemyType;
    // reward on wrong action
    public void GetDamage()
    {
        GameManager.instance.GetDamage(damage);

        // load health reward info
        GameObject healthRewardBarPrefab = Resources.Load<GameObject>("Prefabs/Health Reward Bar");
        HealthRewardBar healthRewardBar = Instantiate(healthRewardBarPrefab, transform.parent.position, Quaternion.identity).GetComponent<HealthRewardBar>();
        healthRewardBar.name = "Health Reward Bar";

        // show health reward info
        healthRewardBar.ShowDamage(damage);
    }
    // reward on right action
    public void GetReward()
    {
        // check for multiply selection and increase reward value
        int reward = this.reward + 5 * (GameManager.instance.CurrentSelectionSeries - 1);
        GameManager.instance.AddCoins(reward > 0 ? reward : 0);

        // load reward info
        GameObject rewardBarPrefab = Resources.Load<GameObject>("Prefabs/Reward Bar");
        RewardBar rewardBar = Instantiate(rewardBarPrefab, transform.parent).GetComponent<RewardBar>();
        rewardBar.name = "Reward Bar";

        // show reward info
        rewardBar.ShowReward(reward);
    }
    // clear square from enemy(event when clicked before time is over)
    public void FreeSquare(bool isManual)
    {
        // toggle selected state
        isSelected = true;

        // toggle layer
        gameObject.layer = LayerMask.NameToLayer("Enemy");

        // enemy's action
        string dyingSoundName = string.Empty;
        switch (enemyType)
        {
            case EnemyType.Shit:
            {
                if (IsTutorial)
                {
                    if (!GameManager.instance.IsStageNumberSeven() && !GameManager.instance.IsStageNumberSix())
                        GameManager.instance.RightAction();
                }
                else
                {
                    // add coins
                    GetReward();
                }

                bool chance = Random.Range(0, 101) <= chanceForSound;
                if (chance || IsTutorial)
                {
                    // get dying sound name
                    List<string> availableShitSoundsNames = new List<string>
                    {
                        "Fart 2", "Fart 3", "Fart 4", "Fart 5", "Fart 6"
                    };
                    dyingSoundName = availableShitSoundsNames[Random.Range(0, availableShitSoundsNames.Count)];
                }

            } break;
            case EnemyType.Heart:
            {
                if (IsTutorial)
                {
                    if (!GameManager.instance.IsStageNumberSeven() && !GameManager.instance.IsStageNumberSix())
                        GameManager.instance.RightAction();
                }
                else
                {
                    // positive damage => healing
                    GetDamage();
                }

                // get dying sound name
                dyingSoundName = "Heart";
            } break;
            case EnemyType.Bomb:
            {
                if (IsTutorial)
                {
                    if (!GameManager.instance.IsStageNumberSeven() && !GameManager.instance.IsStageNumberSix())
                        GameManager.instance.WrongAction();
                }
                else
                {
                    if (!isManual)
                    {
                        // negative damage => hearting
                        GetDamage();
                    }
                }

                // get dying sound name
                dyingSoundName = "Bomb";
            }
            break;
        }

        // play dying sound name
        if(dyingSoundName != string.Empty)
        {
            AudioClip soundClip = Resources.Load<AudioClip>("Sounds/" + dyingSoundName);
            SoundManager.instance.PlaySingle(soundClip);
        }

        // stop timer
        if (timerBar)
            timerBar.StopTimer();

        // tutorial utility
        if (IsTutorial)
            Destroy(tutorialBar);
        // (start/continue) selection series
        else if (enemyType == EnemyType.Shit)
            GameManager.instance.ContinueSelectionSeries(score);

        // do unique action (throw 3d model and just destroy 2d one)
        rigidbody.isKinematic = false;
        rigidbody.AddForce(new Vector3(Random.Range(-1, 1), 1, Random.Range(-1, 1)) * Random.Range(15, 30), ForceMode.Impulse);

        // remove 2D enemy reference
        Destroy(enemyUI);
    }
    // get enemy name with spaces between words
    private string GetEnemyName(EnemyType enemyType)
    {
        string enemyName = string.Empty;
        switch (enemyType)
        {
            case EnemyType.Shit: enemyName = "Shit"; break;
            case EnemyType.Bomb: enemyName = "Bomb"; break;
            case EnemyType.Heart: enemyName = "Heart"; break;
        }

        return enemyName;
    }
    // set current square position
    public void SetSquarePosition(Vector3 squarePosition) => this.squarePosition = squarePosition;
    // set reference to enemy (UI)
    public void SetEnemyUI(GameObject enemyUI) => this.enemyUI = enemyUI;
    // check for having timer on
    public bool HasTimer() => timerBar;
    // give 'reward' on timer ending
    private void OnTimerEnded()
    {
        // clear squares
        GridManager.instance.GetSquares(this);

        // enemy was selected(clicked on)
        if (isSelected)
        {
            // destroy object(make fade out animation in future)
            if (this)
                Destroy(transform.parent.gameObject, 1f);
        }
        // timer ended
        else
        {
            if (IsTutorial)
            {
                if (!GameManager.instance.IsStageNumberSeven() && !GameManager.instance.IsStageNumberSix())
                {
                    if (enemyType == EnemyType.Shit || enemyType == EnemyType.Heart)
                        GameManager.instance.WrongAction();
                    else
                        GameManager.instance.RightAction();
                }
            }
            else
            {
                // negative damage => hearting
                if (enemyType == EnemyType.Shit) 
                    GetDamage();
            }

            // as timer is ended, we have to do unique action right here, not as when Clicked -> FreeSquare
            // destroy object(make fade out animation in future)
            if (this)
            {
                Destroy(transform.parent.gameObject);
                Destroy(enemyUI);
            }
        }
    }

    // 'landed' action
    public void OnEnemyLanded(Square square)
    {
        // check for ALREADY selecting enemy
        if (isSelected || timerBar && timerBar.IsEnding) return;

        // set layer (camera zoom utility)
        gameObject.layer = LayerMask.NameToLayer("Dropped Enemy");

        // set square item (removing 'that bug')
        StartCoroutine(AllowSelection(square, .01f));

        // add parent
        Transform enemyParentTransform = new GameObject().transform;
        enemyParentTransform.position = transform.position;
        enemyParentTransform.name = GetEnemyName(enemyType); // can add space between (???)
        transform.parent = enemyParentTransform;

        // load timer bar
        string timerBarName = enemyType == EnemyType.Bomb ? "Timer 2 Bar" : "Timer 1 Bar";
        GameObject timerBarPrefab = Resources.Load<GameObject>("Prefabs/" + timerBarName);
        timerBar = Instantiate(timerBarPrefab, transform.parent).GetComponent<TimerBar>();
        timerBar.transform.position = new Vector3(transform.position.x, transform.position.y + 1, transform.position.z);
        timerBar.name = "Timer Bar";

        // spawn 2D verison of current enemy and add reference to timer
        GameObject enemyUIPrefab = Resources.Load<GameObject>("Prefabs/Boosters/Boosters (UI)/" + enemyType);
        Transform enemyUI = Instantiate(enemyUIPrefab, GameObject.Find("Game Field (UI)").transform).transform;
        enemyUI.position = squarePosition;
        enemyUI.name = enemyType.ToString();
        timerBar.SetReferenceToUI
        (
            enemyUI.Find("Timer Bar/Background").GetComponent<UnityEngine.UI.Image>(),
            enemyUI.Find("Timer Bar/Foreground").GetComponent<UnityEngine.UI.Image>()
        );
        SetEnemyUI(enemyUI.gameObject);

        // start timer
        timerBar.StartTimer(duration);
        timerBar.onTimerEnded += OnTimerEnded;
    }

    #region Utils

    private IEnumerator AllowSelection(Square square, float delay)
    {
        yield return new WaitForSeconds(delay);

        square.SetItem(this);
        if (square.IsTargeting) square.IsTargeting = false;
    }

    #endregion
}
public enum EnemyType
{
    Shit = 0,
    Bomb = 1,
    Heart = 2
}
