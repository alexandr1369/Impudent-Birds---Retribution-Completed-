using UnityEngine;
using Pixelplacement;
using System.Linq;
using System.Collections;
using Pixelplacement.TweenSystem;

public class Bird : MonoBehaviour
{
    private bool hasItemDropped = false;

    private Square square; // current attacking square
    private ActionType actionType; // bird attacking type

    [SerializeField]
    private Transform parentTransform; // bird has to have a parent obj

    [SerializeField]
    private GameObject boomEffectPrefab; // boom effect prefab

    [SerializeField]
    private Animator animator; // main animator

    [SerializeField]
    private int damage; // damage amount (attack)

    [SerializeField]
    private int score; // score amount (attack)

    #region Curves

    [SerializeField]
    private AnimationCurve flyCurve;
    [SerializeField]
    private AnimationCurve glideCurve;
    [SerializeField]
    private AnimationCurve attackCurve;
    [SerializeField]
    private AnimationCurve landingCurve;
    [SerializeField]
    private AnimationCurve takingOffCurve;
    [SerializeField]
    private AnimationCurve enemyFallCurve;

    #endregion

    #region Movement Utils

    [SerializeField]
    private float startFlightDuration = 5f; // start bird's movement time
    private float flightDuration; // current flight duration
    [SerializeField]
    private float startAnimationSpeed = 1.3f; // start speed multiplier of bird's animation
    private float animationSpeed; // current animation speed

    private int flightsAmountLeft; // current amount of flights left before switching to 'gliding' animation

    private bool isLanding; // is bird landed on current attacking square (while pooping with landing type bird)

    #endregion

    private void Start()
    {
        flightsAmountLeft = Random.Range(3, 6);
        isLanding = false;
    }
    private void Update()
    {
        if(!hasItemDropped)
        {
            // check for being under current square to drop an item
            if(actionType == ActionType.PassingBy)
            {
                RaycastHit hit;
                if (Physics.Raycast(parentTransform.position, new Vector3(parentTransform.position.x, -100, parentTransform.position.z), out hit, 100, LayerMask.GetMask("Square")))
                {
                    if (hit.transform.GetComponent<Square>() == square)
                    {
                        // attack booster (square is occupied)
                        if (square.GetItem() as Booster)
                        {
                            Attack();
                        }
                        // drop an item
                        else
                        {
                            DropItem();
                        }
                    }
                }
            }
            // check for landing on current square
            else if(actionType == ActionType.Landing)
            {
                // check for still having a square reference (prevent multi-attacking)
                if (!square) return;

                float distance = Vector3.Distance(square.transform.position, parentTransform.position);
                if(distance <= 7f)
                {
                    // attack booster (square is occupied)
                    if (square.GetItem() as Booster)
                    {
                        Attack();
                    }
                    // land on square
                    else
                    {
                        if (!isLanding)
                        {
                            Landing();
                        }
                    }
                }
            }
        }
    }

    // move the bird in two different ways
    public void StartMovement(Square square, ActionType actionType)
    {
        // set current square
        this.square = square;
        square.IsTargeting = true;

        // get get flight duration and animation speed 
        float multiplier = GetMultiplier();
        flightDuration = startFlightDuration / multiplier;
        animationSpeed = startAnimationSpeed * multiplier - (multiplier - 1) / multiplier * 1.5f;

        // start 'flying' animation
        animator.SetBool("Flying", true);

        // choose action type and start movement (unique landing - 7.5%)
        bool chance = Random.Range(0, 101) > 7.5f;

        // UPDATE : there wont't be any landing type bird!
        if (actionType == ActionType.PassingBy)
        {
            // set type
            this.actionType = ActionType.PassingBy;
            Vector3 squarePosition = square.transform.position, birdPosition = parentTransform.transform.position;
            Vector3 endPosition = birdPosition + new Vector3(squarePosition.x - birdPosition.x, 0, squarePosition.z - birdPosition.z).normalized * 30;

            // start movement
            //animator.SetFloat("Speed", animationSpeed);
            transform.LookAt(endPosition);
            Tween.Position(parentTransform, endPosition, flightDuration, 0,
            startCallback: () =>
            {
                transform.LookAt(endPosition);
            },
            completeCallback: () =>
            {
                Destroy(parentTransform.gameObject);
            });
        }
        else
        {
            // set type
            this.actionType = ActionType.Landing;

            // start movement
            animator.SetFloat("Speed", animationSpeed);
            Tween.Position(parentTransform, square.transform.position, flightDuration, 0,
            startCallback: () =>
            {
                transform.LookAt(square.transform.position);
            });
        }
    }
    // get animation and movement speed utility
    private float GetMultiplier() => 1 + (BirdsManager.instance.GetSpeedMultiplier() - 1) / 3;

    #region Flying Utils

    // fly up a bit after wings flapping
    // have to use local position because of some issues with global transform for parent and child objects with surge on each (???)
    private void Fly()
    {
        if (isLanding) return;

        // how much a bird will upper its flight altitude
        float riseAmount = Random.Range(4f, 6f);

        // start flying
        Vector3 currentPosition = transform.localPosition;
        Vector3 endValue = new Vector3(currentPosition.x, currentPosition.y + riseAmount, currentPosition.z);
        Tween.LocalPosition(transform, endValue, .7f, .25f, flyCurve);

        // check for gliding
        if (flightsAmountLeft <= 0)
            animator.SetTrigger("Gliding");
        else
            flightsAmountLeft--;
    }
    private void Fly(float rise)
    {
        if (isLanding) return;

        // how much a bird will upper its flight altitude
        float riseAmount = rise;

        // start flying
        Vector3 currentPosition = transform.localPosition;
        Vector3 endValue = new Vector3(currentPosition.x, currentPosition.y + riseAmount, currentPosition.z);
        Tween.LocalPosition(transform, endValue, .7f, .25f, flyCurve);

        // check for gliding
        if (flightsAmountLeft <= 0)
            animator.SetTrigger("Gliding");
        else
            flightsAmountLeft--;
    }
    // fall down smoothly after gliding is started
    private void Glide()
    {
        if (isLanding) return;

        // how much a bird will lower its flight altitude
        float fallAmount = Random.Range(12f, 15f);

        // start gliding
        Vector3 currentPosition = transform.localPosition;
        Vector3 endValue = new Vector3(currentPosition.x, currentPosition.y - fallAmount, currentPosition.z);
        Tween.LocalPosition(transform, endValue, 2.875f, 0, glideCurve);

        // reset amount of flights
        flightsAmountLeft = Random.Range(3, 6);
    }
    // landing with smooth resetting birds local position
    private void Landing() 
    {
        // toggle landing state
        isLanding = true;

        // get landing duration and local rotation
        float landingTime = 0f;
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == "Raven Armature|Landing")
            {
                landingTime = clip.length;
                break;
            }
        }
        float duration = landingTime / animationSpeed;
        Vector3 eulerAngles = transform.eulerAngles;

        // reset child rotation and position
        Tween.Value(transform.eulerAngles.x, 0, (float t) => transform.eulerAngles = new Vector3(t, eulerAngles.y, eulerAngles.z), duration, 0);
        Tween.LocalPosition(transform, Vector3.zero, duration, 0);

        // get nearest square side
        float x, z;
        Vector3 direction, nearestSquareSide;
        direction = (square.transform.position - parentTransform.position).normalized;
        x = square.transform.position.x - direction.x * GridManager.instance.GetGraphicsScaleUnit() * 3 / 5;
        z = square.transform.position.z - direction.z * GridManager.instance.GetGraphicsScaleUnit() * 3 / 5;
        nearestSquareSide = new Vector3(x, square.transform.position.y, z);

        // start landing
        Tween.Position(parentTransform, nearestSquareSide/*square.transform.position*/, duration, 0, landingCurve);

        // set triggers
        animator.SetTrigger("Landing");
        animator.SetBool("Flying", false);
    }
    // taking off starting with turning bird 180 degrees around origin
    private void TakingOffStart()
    {
        // rotate bird's parent 180 degrees on the OY
        Vector3 eulerAngles = parentTransform.eulerAngles;
        parentTransform.eulerAngles = new Vector3(eulerAngles.x, eulerAngles.y - 180, eulerAngles.z);
				
        // reset amount of flights
        flightsAmountLeft = 12;

        // reset 'flying' trigger
        animator.SetBool("Flying", true);
    }
    // start movement after starting taking off
    private void TakingOffMovement()
    {
        // start movement
        Vector3 endPosition = new Vector3(transform.forward.x * 15, 8, transform.forward.z * 15);
        Tween.Position(parentTransform, endPosition, flightDuration, 0, takingOffCurve,
        completeCallback: () =>
        {
            BirdsManager.instance.RemoveBird(this);
            Destroy(parentTransform.gameObject);
        });
    }

    #endregion

    #region Actions

    // bird shoots itselft like a bullet at current attacking square 
    private void Attack()
    {
        // stop animator
        animator.enabled = false;

        // start attaking
        Booster booster = (square.GetItem() as Booster);
        float attackTime = 1f / (1 + (BirdsManager.instance.GetSpeedMultiplier() - 1) / 3) * Vector3.Distance(parentTransform.position, booster.transform.position) / 7f;

        // show boom effect
        GameObject boomEffect = Instantiate(boomEffectPrefab, square.transform.position, Quaternion.identity);
        boomEffect.name = boomEffectPrefab.name;
        Destroy(boomEffect, 2f);

        // move bird
        Tween.Position(parentTransform, booster.transform.position, attackTime, 0, attackCurve,
        startCallback: () =>
        {
            if(booster)
                transform.LookAt(booster.transform.position);
            Tween.LocalPosition(transform, Vector3.zero, attackTime, 0, attackCurve);
        },
        completeCallback: () =>
        {
            if (booster)
                booster.GetDamage(damage);
            //GameManager.instance.ContinueSelectionSeries(score);
            BirdsManager.instance.RemoveBird(this);
            Destroy(parentTransform.gameObject);
        });

        // clear square
        square.IsTargeting = false;
        square = null;
    }
    // action for 'passing by' action type 
    private void DropItem()
    {
        string enemyName = string.Empty;
        float chance = Random.Range(0, 101);

        // get item to drop
        // 'shit'
        if (0 <= chance && chance < 85)
        {
            enemyName = "Shit";
        }
        else
        {
            // 'heart'
            if (!GameObject.FindGameObjectWithTag("Heart") && GameManager.instance.HasGotDamage() && chance >= 97)
            {
                enemyName = "Heart";
            }
            // 'bomb'
            else
            {
                enemyName = "Bomb";
            }
        }

        Vector3 enemyPosition = new Vector3(square.transform.position.x, transform.position.y, square.transform.position.z);
        GameObject enemyPrefab = Resources.Load<GameObject>("Prefabs/Enemies/" + enemyName);
        Transform enemy = Instantiate(enemyPrefab).transform;
        enemy.GetComponent<Enemy>().SetSquarePosition(square.transform.position);
        enemy.position = enemyPosition;
        enemy.name = enemyName;

        // move enemy to square
        Tween.Position(enemy, square.transform.position, .75f, 0, enemyFallCurve, 
        startCallback: () =>
        {
            hasItemDropped = true;
            BirdsManager.instance.RemoveBird(this);
        },
        completeCallback: () =>
        {
            // land the enemy
            enemy.GetComponent<Enemy>().OnEnemyLanded(square);

            // clear square
            square = null;
        });
    }
    // action for 'landing' action type
    private void SpawnShit()
    {
        // spawn 'shit' ONLY
        string enemyName = "Shit";
        Vector3 enemyPosition = new Vector3(square.transform.position.x, square.transform.position.y, square.transform.position.z);
        GameObject enemyPrefab = Resources.Load<GameObject>("Prefabs/Enemies/" + enemyName);
        Transform enemy = Instantiate(enemyPrefab).transform;
        enemy.GetComponent<Enemy>().SetSquarePosition(square.transform.position);
        enemy.position = enemyPosition;
        enemy.name = enemyName;

        // toggle shit dropping
        hasItemDropped = true;

        // land the enemy
        enemy.GetComponent<Enemy>().OnEnemyLanded(square);

        // clear square
        square = null;
    }

    #endregion
}
public enum ActionType
{
    PassingBy = 0,
    Landing = 1
}