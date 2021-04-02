using System;
using UnityEngine.SceneManagement;
using UnityEngine;
using Pixelplacement;
using TMPro;
using UnityEngine.Playables;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    #region Singleton

    public static GameManager instance;
    private void Awake()
    {
        instance = this;
    }

    #endregion

    #region Fields

    // demo state
    [SerializeField]
    private bool isDemo = false;

    // game pause
    private bool isPaused;
    public bool IsPaused { get => isPaused; }

    // languages
    private Language currentLanguage;
    [SerializeField]
    private Text skipToggleText;
    [SerializeField]
    private Text btnSkipText;
    [SerializeField]
    private Image btnMenuImage;

    // playable director (cutscene)
    [SerializeField]
    private PlayableDirector pd;

    // intro utility
    private bool isIntro = false;

    // tutorial utils
    private bool isTutorial = false;
    public bool IsTutorial { get => isTutorial; }
    private bool isRightAction = false;
    [SerializeField]
    private AnimationCurve enemyFallCurve;
    private int currentTutorialStage = 0;
    [SerializeField]
    private Text currentTutorialStageText;
    [SerializeField]
    private ToggleButton btnPause;

    // current scene type (scene type will have all scenes names)
    private SceneType sceneType;
    public SceneType SceneType { get => sceneType; }

    // amount of lifes
    [SerializeField]
    private TextMeshProUGUI lifes;
    public int StartLifesAmount { get => startLifesAmount; }
    [SerializeField]
    private int startLifesAmount = 5;
    public int LifesAmount { get => lifesAmount; }
    private int lifesAmount;
    [SerializeField]
    private LifesTrackController lifesTrackController;

    // (init visual effects)
    [SerializeField]
    private Animator bushFenceAnimator;

    // round ending
    [SerializeField]
    private EndGameRoundManager endRoundManager;

    // amount of coins
    [SerializeField]
    private TextMeshProUGUI coins;
    [SerializeField]
    private int maxCoinsAmount = 999;
    private int coinsAmount;

    // timer (in sec)
    [SerializeField]
    private float populationMaxTimeAmount;
    private bool isTimerOn = false;
    private float timeAmount;

    // score
    [SerializeField]
    private TextMeshProUGUI score;
    private int currentScore;

    // score multi touch
    [SerializeField]
    private float selectionSeriesTime = 1f;
    private float currentSelectionSeriesTime;
    public int CurrentSelectionSeries { get => currentSelectionSeries; }
    private int currentSelectionSeries = 1;

    // skybox
    [SerializeField]
    private Material skyboxMaterial;

    #endregion

    public void Start()
    {
        Init();
    }
    public void Update()
    {
        //// DEMO
        //if (Input.GetKeyDown(KeyCode.C))
        //    ReloadCurrentScene();
        //if (Input.GetKeyDown(KeyCode.Space))
        //    ScreenCapture.CaptureScreenshot(DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ".png");

        // check for cutscenes ending
        if (pd && pd.state == PlayState.Paused && pd.time == 0)
        {
            // check for 'intro' ending
            if (isIntro && pd.playableAsset.name == "Intro")
            {
                // toggle intro state
                isIntro = false;

                // check for 'tutorial' cutscene
                bool isFirstGame = !PlayerPrefs.HasKey("FirstGame");
                if (isDemo || isFirstGame)
                    StartTutorial();
                else
                    StartGameRound();
            }
            // check for 'tutorial' ending
            else if(isTutorial && pd.playableAsset.name == "Tutorial")
            {
                // toggle tutorial state
                isTutorial = false;
                btnPause.enabled = true;
                PlayerPrefs.SetInt("FirstGame", 0);

                // start game round
                StartGameRound();
            }
        }

        // check for tutorial stages
        if (isTutorial && pd.state != PlayState.Playing)
        {
            switch (currentTutorialStage)
            {
                // zoom in
                case 1:
                {
                    // check for next stage
                    if(CameraController.instance.CurrentZoom == 1)
                    {
                        pd.Play();
                    }
                } break;
                // zoom out
                case 2:
                {
                    // check for next stage
                    if (CameraController.instance.CurrentZoom == 0)
                    {
                        CameraController.instance.CanOnlyZoomOut = false;
                        pd.Play();
                    }
                } break;
                // select 'shit'
                case 3: 
                // select 'heart'
                case 4:
                // not select 'bomb'
                case 5:
                // click on 'life bottle' button
                case 7:
                {
                    if (isRightAction)
                    {
                        isRightAction = false;
                        pd.Play();

                        // load health reward info
                        GameObject healthRewardBarPrefab = Resources.Load<GameObject>("Prefabs/Health Reward Bar");
                        Vector3 healthRewardBarPosition = Camera.main.transform.position;
                        HealthRewardBar healthRewardBar = 
                            Instantiate(healthRewardBarPrefab, healthRewardBarPosition, Quaternion.identity, GameObject.Find("UI").transform)
                            .GetComponent<HealthRewardBar>();
                        healthRewardBar.name = "Health Reward Bar";

                        // show health reward info
                        int damage = -2;
                        healthRewardBar.ShowDamage(damage);
                    }
                } break;
                // place 'battlefield cleaner'
                case 6:
                {
                    if (isRightAction)
                    {
                        isRightAction = false;
                        pd.Play();
                    }
                    else
                    {
                        if (GameObject.FindGameObjectsWithTag("Bomb").Length + GameObject.FindGameObjectsWithTag("Shit").Length == 0)
                            SpawnTutorialEnemies(5);
                    }
                } break;
            }
        }

        // raycast camera -> mouse pos to click on squares
        if (Input.GetMouseButton(0))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, 100, LayerMask.GetMask("Square")))
            {
                Square square = hit.transform.GetComponent<Square>();
                if (square != null)
                {
                    // try to select
                    square.Select();
                }
            }
        }

        // timer playing
        if (isTimerOn)
            timeAmount += Time.deltaTime;

        // skybox rotating (slow)
        if (skyboxMaterial && skyboxMaterial.HasProperty("_Rotation"))
            skyboxMaterial.SetFloat("_Rotation", skyboxMaterial.GetFloat("_Rotation") - Time.deltaTime / 3);

        // selection series lasting
        if (currentSelectionSeriesTime > 0)
            currentSelectionSeriesTime -= Time.deltaTime;
        else
            if (currentSelectionSeries > 1)
            currentSelectionSeries = 1;
    }

    // game round starter
    private void StartGameRound()
    {
        // turn timer on
        ToggleTimer(true);

        // start birds's spawning
        StartCoroutine(BirdsManager.instance.ToggleSpawning(true));

        // turn bg music on
        AudioClip bgAudioClip = Resources.Load<AudioClip>("Sounds/Battle");
        SoundManager.instance.PlayBackgroundMusic(bgAudioClip);

        // bush fence visual effect
        bushFenceAnimator.SetTrigger("Appearing");

        // allow camera zooming
        CameraController.instance.AllowCameraZooming();

        // load 3d 'Heart' models on 'Lifes Track'
        if (lifesTrackController)
            lifesTrackController.StartLifesTrack();

        // start 'fire' animations
        foreach (GameObject fire in GameObject.FindGameObjectsWithTag("FirePS"))
        {
            ParticleSystem ps = fire.GetComponent<ParticleSystem>();
            ps.Simulate(2f);
            ps.Play();
        }
    }
    // end current round
    private void EndGameRound()
    {
        // toggle pause
        TogglePause(true);

        // show panel
        endRoundManager.gameObject.SetActive(true);

        // set score
        endRoundManager.SetScore(currentScore);
    }

    #region Init

    // start settings
    private void Init()
    {
        //// DEMO
        //PlayerPrefs.DeleteAll();

        // prevent pausing when spawning between scenes
        TogglePause(false);

        // quality settings
        Application.targetFrameRate = 60;

        // get scene type
        sceneType = (SceneType)Enum.Parse(typeof(SceneType), SceneManager.GetActiveScene().name.Split(' ')[0]);
        switch(sceneType)
        {
            case SceneType.Menu:
            {
                // play menu into fx
                AudioClip menuIntroFxClip = Resources.Load<AudioClip>("Sounds/Menu Intro Fx");
                SoundManager.instance.PlaySingle(menuIntroFxClip);

                // start bg music with delay (1 sec)
                AudioClip menuClip = Resources.Load<AudioClip>("Sounds/Menu");
                SoundManager.instance.PlayBackgroundMusic(menuClip, .75f);

                // set language
                SetLanguage();

                // set current record
                score.text = "--";
                if (PlayerPrefs.HasKey("ScoreRecord") && PlayerPrefs.HasKey("FirstGame"))
                    score.text = PlayerPrefs.GetInt("ScoreRecord").ToString();

            } break;
            case SceneType.Battle:
            {
                // set language
                UpdateLanguage();

                // set coins
                if (coins)
                {
                    coinsAmount = 0;
                    //coinsAmount = 999;
                    coins.text = coinsAmount.ToString();
                }

                // set lifes
                if (lifes)
                {
                    lifesAmount = startLifesAmount;
                    lifes.text = lifesAmount.ToString();
                }

                // set score
                if (score)
                {
                    currentScore = 0;
                    score.text = currentScore.ToString();
                }

                // check for start cutscene
                if (PlayerPrefs.GetInt("BattleReloaded") != 1 &&
                    (isDemo || !PlayerPrefs.HasKey("CutsceneSkip") || PlayerPrefs.HasKey("CutsceneSkip") && PlayerPrefs.GetInt("CutsceneSkip") != 1))
                {
                    // load and play 'intro' animation
                    pd.playableAsset = Resources.Load<PlayableAsset>("Animations/Cutscenes/Intro");
                    pd.Play();
                }
                // start game round immediately
                else 
                {
                    // check for 'battle' scene reloading
                    if(PlayerPrefs.GetInt("BattleReloaded") == 1)
                        PlayerPrefs.DeleteKey("BattleReloaded");

                    // check for 'tutorial' cutscene
                    bool isFirstGame = !PlayerPrefs.HasKey("FirstGame");
                    if (isDemo || isFirstGame)
                        StartTutorial();
                    else
                        StartGameRound();
                }
            } break;
        }

        // set skybox rotation
        if (skyboxMaterial && skyboxMaterial.HasProperty("_Rotation"))
            skyboxMaterial.SetFloat("_Rotation", 0);
    }

    // timer activator
    private void ToggleTimer(bool state) => isTimerOn = state;
    // get time {xx:xx}
    private string GetTime(float sec)
    {
        int minutes = (int)sec / 60, seconds = (int)sec % 60;
        return (minutes / 10 >= 1 ? minutes.ToString() : "0" + minutes) + ":" + (seconds / 10 >= 1 ? seconds.ToString() : "0" + seconds);
    }

    #endregion

    #region Main

    // turn game pause (on/off)
    public void TogglePause()
    {
        Time.timeScale = isPaused ? 1f : 0;
        isPaused = !isPaused;
    }
    public void TogglePause(bool state)
    {
        Time.timeScale = state ? 0 : 1f;
        isPaused = state;
    }
    // scene loaders
    public void LoadScene(SceneType sceneType)
    {
        string sceneName = sceneType.ToString();
        if (sceneType == SceneType.Battle)
            sceneName = SceneManager.GetActiveScene().name == "Battle 1" ? "Battle 2" : "Battle 1";
        SceneManager.LoadScene(sceneName);
    }
    public void ReloadCurrentScene()
    {
        string sceneName = sceneType.ToString();
        if (sceneType == SceneType.Battle)
        {
            sceneName = SceneManager.GetActiveScene().name == "Battle 1" ? "Battle 2" : "Battle 1";
            PlayerPrefs.SetInt("BattleReloaded", 1);
        }
        SceneManager.LoadScene(sceneName);
    }
    // add score (with checking for selection series)
    public void ContinueSelectionSeries(int scoreAmount)
    {
        // check for right score amount
        if (scoreAmount <= 0) return;

        // check for current selection series
        int multiplier = 1;
        if(currentSelectionSeriesTime > 0 && currentSelectionSeries > 1)
            multiplier = currentSelectionSeries;

        // update current score
        currentScore += scoreAmount * multiplier;
        score.text = currentScore.ToString();

        // update selection series
        currentSelectionSeriesTime = selectionSeriesTime;
        currentSelectionSeries++;

        if (currentSelectionSeries >= 5)
        {
            // get new score scale
            Vector3 scoreScale = score.transform.localScale;
            Vector3 newScoreScale = new Vector3(scoreScale.x + .1f, scoreScale.y + .1f, scoreScale.z + .1f);

            // animate scale changing
            Tween.LocalScale(score.transform, newScoreScale, .25f, 0,
                completeCallback: () => Tween.LocalScale(score.transform, new Vector3(1f, 1f, 1f), .25f, 0));
        }
    }
    // get damage (player)
    public void GetDamage(int damage)
    {
        // check for real damage
        if (damage == 0 || damage < 0 && lifesAmount == startLifesAmount) return;

        // set lifes amount
        lifesAmount = Mathf.Clamp(lifesAmount - damage, 0, startLifesAmount);
        lifes.text = lifesAmount.ToString();

        // move lifes track controller (visual 3d lifes indicator)
        if (lifesTrackController)
        {
            if (damage > 0)
                lifesTrackController.MoveLifesTrack();
            else
                for(int i = 0; i < Mathf.Abs(damage); i++)
                    StartCoroutine(lifesTrackController.AddLife());
        }

        // check for death
        if (!isPaused && lifesAmount <= 0)
            EndGameRound();
    }
    // has player got damage
    public bool HasGotDamage() => lifesAmount + GameObject.FindGameObjectsWithTag("Heart").Length >= startLifesAmount ? false : lifesAmount < startLifesAmount;
    // get current time (seconds)
    public float GetCurrentTimeAmount() => timeAmount;
    // get population max time (300 sec)
    public float GetPopulationMaxTimeAmount() => populationMaxTimeAmount;

    #region Coins Utils

    // get coins amount
    public int GetCoins() => coinsAmount;
    // add coins to current bank
    public void AddCoins(int amount)
    {
        coinsAmount += amount;
        if (coinsAmount < 0) coinsAmount = 0;
        else if (coinsAmount > maxCoinsAmount) coinsAmount = maxCoinsAmount;

        coins.text = coinsAmount.ToString();
    }
    // remove coins (buy smth: can afford -> true : false)
    public bool RemoveCoins(int amount)
    {
        if (coinsAmount - amount < 0)
            return false;
        else
        {
            coinsAmount -= amount;
            coins.text = coinsAmount.ToString();
        }

        return true;
    }

    #endregion

    #endregion

    #region 'Intro' cutscene utils

    public void SkipStartCutscene(Toggle toggle)
    {
        // check for skipping cutscene forever
        if (toggle.isOn)
            PlayerPrefs.SetInt("CutsceneSkip", 1);

        // skip current cutscene
        pd.time = pd.playableAsset.duration;
    }
    // 'intro' signal utility
    public void IntroStart()
    {
        // toggle intro state
        isIntro = true;

        // play 'intro' bg music
        AudioClip introAudioClip = Resources.Load<AudioClip>("Sounds/Intro");
        SoundManager.instance.PlayBackgroundMusic(introAudioClip);
    }

    #endregion

    #region 'Tutorial' cutscene utils

    private void StartTutorial()
    {
        // disable 'pause' btn
        btnPause.enabled = false;

        // reset music volume (utility)
        SoundManager.instance.SetMusicVolume(1f);

        // allow camera zooming
        CameraController.instance.AllowCameraZooming();

        // load 3d 'Heart' models on 'Lifes Track'
        if (lifesTrackController)
            lifesTrackController.StartLifesTrack();

        // load and play 'tutorial' animation
        pd.time = 0;
        pd.playableAsset = Resources.Load<PlayableAsset>("Animations/Cutscenes/Tutorial");
        pd.Play();

        // toggle tutorial
        isTutorial = true;
    }
    private void SpawnTutorialEnemy(EnemyType enemyType)
    {
        // prevent bug with multi-falling bombs (only 1 is possible)
        if (!IsStageNumberSix())
        {
            foreach (GameObject bomb in GameObject.FindGameObjectsWithTag("Bomb"))
                if (!bomb.GetComponent<Enemy>().IsSelected()) return;
        }

        // load and spawn enemy
        float yEnemyPosition = 7f;
        Square square = GridManager.instance.GetFreeFromBoostersSquare();
        Vector3 enemyPosition = new Vector3(square.transform.position.x, yEnemyPosition, square.transform.position.z);
        GameObject enemyPrefab = Resources.Load<GameObject>("Prefabs/Enemies/" + enemyType);
        Enemy enemy = Instantiate(enemyPrefab).GetComponent<Enemy>();
        enemy.SetSquarePosition(square.transform.position);
        enemy.transform.position = enemyPosition;
        enemy.name = enemyType.ToString();
        enemy.IsTutorial = true;

        // move enemy to square
        Tween.Position(enemy.transform, square.transform.position, .75f, 0, enemyFallCurve,
        completeCallback: () =>
        {
            // land the enemy
            enemy.GetComponent<Enemy>().OnEnemyLanded(square);
            enemy.LoadTutorialBar();
        });
    }
    private void SpawnTutorialEnemies(int amount)
    {
        List<Square> squares = GridManager.instance.GetFreeFromBoostersSquares(amount);
        for(int i = 0; i < squares.Count; i++)
        {
            // get enemy type
            EnemyType enemyType = UnityEngine.Random.Range(0, 2) == 0 ? EnemyType.Shit : EnemyType.Bomb;

            // load and spawn enemy
            float yEnemyPosition = 7f;
            Square square = squares[i];
            Vector3 enemyPosition = new Vector3(square.transform.position.x, yEnemyPosition, square.transform.position.z);
            GameObject enemyPrefab = Resources.Load<GameObject>("Prefabs/Enemies/" + enemyType);
            Enemy enemy = Instantiate(enemyPrefab).GetComponent<Enemy>();
            enemy.SetSquarePosition(square.transform.position);
            enemy.transform.position = enemyPosition;
            enemy.name = enemyType.ToString();
            enemy.IsTutorial = true;

            // move enemy to square
            Tween.Position(enemy.transform, square.transform.position, .75f, 0, enemyFallCurve,
            completeCallback: () =>
            {
                // land the enemy
                enemy.GetComponent<Enemy>().OnEnemyLanded(square);
                enemy.LoadTutorialBar();
            });
        }
    }

    public void SetTutorialStage(int tutorialStage)
    {
        if(isTutorial && pd.playableAsset.name == "Tutorial")
        {
            switch (tutorialStage)
            {
                case 1:
                {
                    CameraController.instance.CanOnlyZoomIn = true;
                    CameraController.instance.CanOnlyZoomOut = false;
                } break;
                case 2:
                {
                    CameraController.instance.CanOnlyZoomOut = true;
                    CameraController.instance.CanOnlyZoomIn = false;
                } break;
                case 3:
                {
                    isRightAction = false;
                    SpawnTutorialEnemy(EnemyType.Shit);
                } break;
                case 4:
                {
                    isRightAction = false;
                    SpawnTutorialEnemy(EnemyType.Heart);
                } break;
                case 5:
                {
                    isRightAction = false;
                    SpawnTutorialEnemy(EnemyType.Bomb);
                } break;
                case 6:
                {
                    isRightAction = false;
                    SpawnTutorialEnemies(5);
                    AddCoins(500);
                } break;
                case 7:
                {
                    isRightAction = false;
                    AddCoins(999);
                    GetDamage(2);
                } break;
            }

            pd.Pause();
            currentTutorialStage = tutorialStage;
            currentTutorialStageText.text = currentTutorialStage + "/7";
        }
    }
    public void RightAction()
    {
        isRightAction = true;
    }
    public void WrongAction()
    {
        switch (currentTutorialStage)
        {
            case 3: SpawnTutorialEnemy(EnemyType.Shit); break;
            case 4: SpawnTutorialEnemy(EnemyType.Heart); break;
            case 5: SpawnTutorialEnemy(EnemyType.Bomb); break;
        }
    }
    public IEnumerator WrongAction(float delay)
    {
        yield return new WaitForSeconds(delay);
        switch (currentTutorialStage)
        {
            case 3: SpawnTutorialEnemy(EnemyType.Shit); break;
            case 4: SpawnTutorialEnemy(EnemyType.Heart); break;
            case 5: SpawnTutorialEnemy(EnemyType.Bomb); break;
        }
    }
    public void TutorialEnded() => StartGameRound();
    public bool IsStageNumberSix() => isTutorial && currentTutorialStage == 6;
    public bool IsStageNumberSeven() => isTutorial && currentTutorialStage == 7;

    #endregion

    #region Language

    // IT'LL BE BETTER TO CONVERT THIS TO DICTIONARY (but there're only 7 items - the only reason)
    // menu lang updater
    public void SetLanguage(Language language)
    {
        // set language
        currentLanguage = language;
        PlayerPrefs.SetString("Language", currentLanguage.ToString());

        // update elements data
        Image btnPlay = GameObject.Find("Right Panel/Btn Play").GetComponent<Image>();
        if (btnPlay)
            btnPlay.sprite = Resources.Load<Sprite>("Textures/UI/Menu/Btn Play (" + currentLanguage + ")");
    }
    public void SetLanguage()
    {
        // set language
        currentLanguage = Language.Uk;
        if (PlayerPrefs.HasKey("Language"))
            currentLanguage = PlayerPrefs.GetString("Language") == "Ru" ? Language.Ru : Language.Uk;
        else
        {
            PlayerPrefs.SetString("Language", Language.Uk.ToString());
            currentLanguage = Language.Uk;
        }

        // update elements data
        Image btnPlay = GameObject.Find("Right Panel/Btn Play").GetComponent<Image>();
        if (btnPlay)
            btnPlay.sprite = Resources.Load<Sprite>("Textures/UI/Menu/Btn Play (" + currentLanguage + ")");
    }
    // battle lang updater
    private void UpdateLanguage()
    {
        // set language
        currentLanguage = Language.Uk;
        if (PlayerPrefs.HasKey("Language"))
            currentLanguage = PlayerPrefs.GetString("Language") == "Ru" ? Language.Ru : Language.Uk;
        else
        {
            PlayerPrefs.SetString("Language", Language.Uk.ToString());
            currentLanguage = Language.Uk;
        }

        // update elements data
        btnSkipText.text = currentLanguage == Language.Ru? "Пропустить" : "Skip";
        skipToggleText.text = currentLanguage == Language.Ru? "Больше не показывать" : "Don't show again";
        btnMenuImage.sprite = Resources.Load<Sprite>("Textures/UI/Playing Field/Btn Menu (" + currentLanguage + ")");
        GameObject.Find("Left Panel/Btn Pause").GetComponent<Image>().sprite = Resources.Load<Sprite>("Textures/UI/Playing Field/Btn Pause (" + currentLanguage + ")");
        GameObject.Find("Left Panel/Population Board").GetComponent<Image>().sprite = Resources.Load<Sprite>("Textures/UI/Playing Field/Population Board (" + currentLanguage + ")");
        GameObject.Find("Right Panel/Coins Board").GetComponent<Image>().sprite = Resources.Load<Sprite>("Textures/UI/Playing Field/Coins Board (" + currentLanguage + ")");
    }

    #endregion
}
public enum SceneType
{
    Menu = 0,
    Battle = 1
}
public enum Language
{
    Ru = 0,
    Uk = 1
}