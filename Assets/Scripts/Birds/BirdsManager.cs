using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pixelplacement;
using TMPro;

public class BirdsManager : MonoBehaviour
{
    #region Singleton

    public static BirdsManager instance;
    private void Awake()
    {
        instance = this;
    }

    #endregion

    private bool isActive; // activity state

    // population graphic drawing
    [SerializeField]
    private Camera uiCamera;
    [SerializeField]
    private GameObject crackSign;

    #region Spawn Utils

    [SerializeField]
    private Vector2 xPositionRange;
    [SerializeField]
    private Vector2 yPositionRange;
    [SerializeField]
    private Vector2 zPositionRange;

    #endregion

    [SerializeField]
    private List<GameObject> boardsList; // graphic boards(for 3, 4 and 5 mins)

    // birds population
    [SerializeField]
    private GameObject birdPrefab;
    [SerializeField]
    private GameObject populationAmountIcon;
    [SerializeField]
    private TextMeshProUGUI populationAmountText;
    private List<PopulationPoint> population; 
    private int currentPopulationAmount;
    [SerializeField]
    private float birdSpawnDelay;
    private float currentBirdSpawnDelay = 0;
    [SerializeField]
    private float delayBeforeNextLandingBird = 20f;
    private float currentDelayBeforeNextLandingBird = 0;

    // additional speed (will be increased with round time lasting)
    [SerializeField]
    private float maxSpeedMultiplier;
    private float speedMultiplier;

    private List<Bird> birds; // all current flying birds (alive)

    private void Start()
    {
        Init();
    }
    private void Update()
    {
        // check for activity
        if (!isActive) return;

        // increase speed multiplier
        if(!GameManager.instance.IsPaused && speedMultiplier < maxSpeedMultiplier)
        {
            float value = Time.deltaTime / Screen.currentResolution.refreshRate * 2.5f;
            speedMultiplier += value;
        }

        // update population amount
        if (population.FindAll(t => !t.isUsed).Count > 0)
        {
            int pointIndex = (int)GameManager.instance.GetCurrentTimeAmount() / 10;
            pointIndex = pointIndex != population.Count ? pointIndex : population.Count - 1;
            PopulationPoint point = population[pointIndex];
            if (!point.isUsed)
            {
                // animated changing population amount
                currentPopulationAmount = point.amount;

                // update population info
                point.isUsed = true;
                population.RemoveAt(pointIndex);
                population.Insert(pointIndex, point);

                // set population amount text
                populationAmountText.text = currentPopulationAmount.ToString();
            }
        }

        // spawn bird (passing by)
        if (currentBirdSpawnDelay <= 0)
        {
            if (birds.Count < currentPopulationAmount)
            {
                if (GridManager.instance.GetFreeFromEnemiesSquares().Count > 0)
                {
                    SpawnBird(ActionType.PassingBy);
                    currentBirdSpawnDelay = birdSpawnDelay;

                    // chance for double spawn ~ 15%
                    bool chance = Random.Range(0, 101) >= 85;
                    if (chance)
                    {
                        SpawnBird(ActionType.PassingBy);

                        // chance for tripple spawn ~ 15% * 33% ~ .05
                        chance = Random.Range(0, 101) >= 66;
                        if (chance)
                            SpawnBird(ActionType.PassingBy);
                    }
                }
            }
        }
        else
            currentBirdSpawnDelay -= Time.deltaTime;

        // spawn bird (landing)
        if (currentDelayBeforeNextLandingBird <= 0)
        {
            SpawnBird(ActionType.Landing);
            currentDelayBeforeNextLandingBird = delayBeforeNextLandingBird;
        }
        else
            currentDelayBeforeNextLandingBird -= Time.deltaTime;

        // set crack sign
        if (GameManager.instance.GetCurrentTimeAmount() >= 300f)
        {
            populationAmountIcon.SetActive(false);
            populationAmountText.gameObject.SetActive(false);
            crackSign.SetActive(true);
        }
    }

    // spawning activator (in the begin)
    public IEnumerator ToggleSpawning(bool state)
    {
        yield return null;

        // get active state
        isActive = state;

        // set current population
        if (isActive && population != null)
        {
            if (!population[0].isUsed)
            {
                PopulationPoint point = population[0];
                currentPopulationAmount = point.amount;
                point.isUsed = true;
                population.RemoveAt(0);
                population.Insert(0, point);
            }
        }

        // set current population amount text
        populationAmountText.text = currentPopulationAmount.ToString();
    }

    // start settings
    private void Init()
    {
        // create birds list
        birds = new List<Bird>();

        // get population
        population = GetRandomizedPopulation();

        // set current population
        currentPopulationAmount = 0;
        speedMultiplier = 1f;
    }
    // get population depending on round time
    private List<PopulationPoint> GetRandomizedPopulation()
    {
        // create population
        List<PopulationPoint> population = new List<PopulationPoint>();
        float time = 0;
        int i = 0;
        int amount = 4;

        // fill population
        population.Add(new PopulationPoint(time, amount));
        while(amount < 9)
        {
            time = 10f * i++;
            amount += UnityEngine.Random.Range(1, 101) > 75 ? 2 : 1;
            population.Add(new PopulationPoint(time, Mathf.Clamp(amount, 4, 9)));
        }

        return population;
    }
    // spawn bird at 'random' place
    private void SpawnBird(ActionType actionType)
    {
        // check for free from enemies squares
        if (!GridManager.instance.HasFreeFromEnemiesSquares()) return;

        // get square free from enemies
        Square square = GridManager.instance.GetFreeFromEnemiesSquares()[UnityEngine.Random.Range(0, GridManager.instance.GetFreeFromEnemiesSquares().Count)];

        // get random position
        float xPosition = 0, yPosition = 0, zPosition = 0;
        if (xPositionRange != null && yPositionRange != null && zPositionRange != null)
        {
            xPosition = UnityEngine.Random.Range(0, 2) == 0 ?
                UnityEngine.Random.Range(xPositionRange.x - 5, xPositionRange.x + 1) : UnityEngine.Random.Range(xPositionRange.y, xPositionRange.y + 6);
            yPosition = UnityEngine.Random.Range(yPositionRange.x, yPositionRange.y + 1);
            zPosition = UnityEngine.Random.Range(zPositionRange.x, zPositionRange.y + 1);
        }

        // spawn bird
        Bird bird = Instantiate(birdPrefab, new Vector3(xPosition, yPosition, zPosition), Quaternion.identity, null).GetComponentInChildren<Bird>();
        bird.transform.parent.gameObject.name = "Raven";

        // start bird's movement
        bird.StartMovement(square, actionType);

        // save bird info
        if(actionType == ActionType.PassingBy)
            birds.Add(bird);
    }
    // remove bird from the list
    public void RemoveBird(Bird bird) => birds.Remove(bird);
    // get speed multiplier
    public float GetSpeedMultiplier() => speedMultiplier;
}
[System.Serializable]
public struct PopulationPoint
{
    public bool isUsed; // is point used in population (cant be used twice)
    public float time; // current (0 - timer) time
    public int amount; // current birds amount

    public PopulationPoint(float time, int amount)
    {
        this.time = time;
        this.amount = amount;
        this.isUsed = false;
    }
}