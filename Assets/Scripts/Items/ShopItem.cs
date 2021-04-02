using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Pixelplacement;
using UnityEngine.UI;
using System.Linq;

public class ShopItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField]
    private Canvas canvas; // UI canvas

    [SerializeField]
    private BoosterType boosterType; // booster type (loading prefab)
    [SerializeField]
    private int price; // booster price
    [SerializeField]
    private float cooldown; // init cooldown (between purchasing)
    private float currentCooldown = 0; // current cooldown
    private float checkDelay = 0; // minefield utility

    [SerializeField]
    private GameObject defIcon; // default icon (getting width and height and image filling)

    private RectTransform boosterIconRectTransform; // booster image snapped to cursor

    private List<Square> lastTryingSquares; // squares booster is trying to be built on

    private Booster booster; // current booster
    private Transform boosterUI; // current booster (UI)

    private bool isDragging = false; // dragging state
    private bool canBeBuilt = false; // bulding state

    private void Start()
    {
        lastTryingSquares = new List<Square>();
    }
    private void Update()
    {
        // check for cooldown
        if (currentCooldown > 0)
        {
            currentCooldown -= Time.deltaTime;
            defIcon.GetComponent<Image>().fillAmount = 1 - (currentCooldown / cooldown);
            return;
        }

        // action only while dragging
        if (!isDragging) return;

        // check for game pause
        if (GameManager.instance.IsPaused)
        {
            isDragging = false;

            // reset icon
            defIcon.GetComponent<Image>().fillAmount = 1f;

            // clear squares
            if (lastTryingSquares.Count > 0)
            {
                lastTryingSquares.ForEach(t => t.ClearBuild());
                lastTryingSquares.RemoveRange(0, lastTryingSquares.Count);
            }

            // remove 2d booster version
            if(boosterIconRectTransform)
                Destroy(boosterIconRectTransform.gameObject);

            // remove booster from the grid
            if (booster != null)
            {
                Destroy(booster.gameObject);
                Destroy(boosterUI.gameObject);
            }

            // close shop (visual)
            GridManager.instance.ToggleShop(false);

            return;
        }

        // try to build
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, 100, LayerMask.GetMask("Square")))
        {
            Square square = hit.transform.GetComponent<Square>();
            Vector3 rayHitPoint = hit.point;

            if (square)
            {
                if(boosterType == BoosterType.BattlefieldCleaner)
                {
                    // get current trying squares
                    List<Square> currentTryingSquares = GridManager.instance.GetEnemiesSquares();
                    if (currentTryingSquares.Count == 0)
                        return;

                    // check for squares changing
                    if (lastTryingSquares.Count > 0)
                    {
                        for (int i = 0; i < currentTryingSquares.Count; i++)
                        {
                            if (!lastTryingSquares.Find(t => t == currentTryingSquares[i]))
                            {
                                lastTryingSquares.ForEach(t => t.ClearBuild());
                                break;
                            }
                        }
                    }

                    // try to place this booster on squares
                    canBeBuilt = true;
                    for (int i = 0; i < currentTryingSquares.Count; i++)
                    {
                        if (!currentTryingSquares[i].TryBuild(boosterType))
                            canBeBuilt = false;
                    }

                    // save current tring squares
                    lastTryingSquares = currentTryingSquares;
                }
                else if (boosterType == BoosterType.Minefield)
                {
                    if (checkDelay <= 0)
                    {
                        // unique action with grid highlightning
                        // ...

                        // (DEMO - will be chanched [squares won't be highlighted)
                        int mineFieldLength = 3;
                        List<Square> currentTryingSquares = GridManager.instance.GetFreeFromBoostersSquares(mineFieldLength * mineFieldLength);

                        // check for squares changing
                        if (lastTryingSquares.Count > 0)
                        {
                            for (int i = 0; i < currentTryingSquares.Count; i++)
                            {
                                if (!lastTryingSquares.Find(t => t == currentTryingSquares[i]))
                                {
                                    lastTryingSquares.ForEach(t => t.ClearBuild());
                                    break;
                                }
                            }
                        }

                        // try to place this booster on squares
                        canBeBuilt = true;
                        for (int i = 0; i < currentTryingSquares.Count; i++)
                        {
                            if (!currentTryingSquares[i].TryBuild(boosterType))
                                canBeBuilt = false;
                        }

                        // save current tring squares
                        lastTryingSquares = currentTryingSquares;

                        // reset check delay
                        checkDelay = 1f;
                    }
                    // continue waiting
                    else
                    {
                        checkDelay -= Time.deltaTime;
                    }
                }
                else
                {
                    // hide booster icon
                    if (boosterIconRectTransform)
                        boosterIconRectTransform.gameObject.GetComponent<Image>().color = new Color(0, 0, 0, 0);

                    // get trying squares
                    List<Square> currentTryingSquares = GetTryingSquares(square, rayHitPoint);

                    // load booster prefab
                    if (!booster)
                    {
                        GameObject boosterPrefab = Resources.Load<GameObject>("Prefabs/Boosters/" + GetBoosterName());
                        GameObject boosterPrefabUI = Resources.Load<GameObject>("Prefabs/Boosters/Boosters (UI)/" + GetBoosterName());
                        booster = Instantiate(boosterPrefab).GetComponent<Booster>();
                        boosterUI = Instantiate(boosterPrefabUI, GameObject.Find("Game Field (UI)").transform).transform;
                        boosterUI.name = GetBoosterName();
                        booster.SetBoosterUI(boosterUI.gameObject);
                    }

                    // check for spawning to play selecting sound
                    if(lastTryingSquares.Count == 0 || !lastTryingSquares.SequenceEqual(currentTryingSquares))
                    {
                        AudioClip selectSoundClip = Resources.Load<AudioClip>("Sounds/Booster Placing");
                        SoundManager.instance.PlaySingle(selectSoundClip);
                    }

                    // check for squares changing
                    if (lastTryingSquares.Count > 0)
                    {
                        for (int i = 0; i < currentTryingSquares.Count; i++)
                        {
                            if (!lastTryingSquares.Find(t => t == currentTryingSquares[i]))
                            {
                                lastTryingSquares.ForEach(t => t.ClearBuild());
                                break;
                            }
                        }
                    }

                    // try to place this booster on squares
                    canBeBuilt = true;
                    for (int i = 0; i < currentTryingSquares.Count; i++)
                    {
                        if (!currentTryingSquares[i].TryBuild())
                            canBeBuilt = false;
                    }

                    // place booster 
                    booster.transform.position = boosterUI.position = GetBoosterPosition(currentTryingSquares);

                    // save current tring squares
                    lastTryingSquares = currentTryingSquares;
                }
            }
        }
        else
        {
            // clear squares
            if (lastTryingSquares.Count > 0)
            {
                canBeBuilt = false;
                lastTryingSquares.ForEach(t => t.ClearBuild());
                lastTryingSquares.RemoveRange(0, lastTryingSquares.Count);
            }

            // remove booster from the grid
            if (booster)
            {
                Destroy(booster.gameObject);
                Destroy(boosterUI.gameObject);
            }

            // show booster icon
            if (boosterIconRectTransform)
            {
                Image boosterIconImage = boosterIconRectTransform.gameObject.GetComponent<Image>();
                if (boosterIconImage.color == new Color(0, 0, 0, 0))
                    boosterIconImage.color = new Color(1, 1, 1, 1);
            }

            // reset 'Minefield' utility
            if (boosterType == BoosterType.Minefield)
                checkDelay = 0;
        }
    }

    #region Dragging

    public void OnBeginDrag(PointerEventData eventData)
    {
        // check for dragging
        isDragging = Mathf.Abs(eventData.delta.x) > Mathf.Abs(eventData.delta.y);
        if (GameManager.instance.IsTutorial)
        {
            if (GameManager.instance.IsStageNumberSix())
                isDragging = boosterType == BoosterType.BattlefieldCleaner;
            else 
                isDragging = false;
        }
        else if (GameManager.instance.IsPaused || currentCooldown > 0) 
            isDragging = false;

        if (isDragging && currentCooldown <= 0)
        {
            // toggle shop
            GridManager.instance.ToggleShop(true);

            // load Image prefab according to booster type
            boosterIconRectTransform = Instantiate(GetBoosterIcon(), canvas.transform).GetComponent<RectTransform>();

            // place booster icon to cursor position
            boosterIconRectTransform.anchoredPosition = Input.mousePosition;

            // hide default icon (fill)
            defIcon.GetComponent<Image>().fillAmount = 0;
        }
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging && boosterIconRectTransform && currentCooldown <= 0)
            boosterIconRectTransform.anchoredPosition += eventData.delta/* / canvas.scaleFactor*/;
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        // check drag ending
        if (isDragging && boosterIconRectTransform && currentCooldown <= 0)
        {
            // reset dragging
            isDragging = false;

            // remove booster icon
            Destroy(boosterIconRectTransform.gameObject);
            boosterIconRectTransform = null;

            // try to cancel 
            if (!canBeBuilt || !GameManager.instance.RemoveCoins(price))
            {
                // reset icon
                defIcon.GetComponent<Image>().fillAmount = 1f;

                // clear squares
                if (lastTryingSquares.Count > 0)
                {
                    lastTryingSquares.ForEach(t => t.ClearBuild());
                    lastTryingSquares.RemoveRange(0, lastTryingSquares.Count);
                }

                // remove booster from the grid
                if (booster != null) 
                { 
                    Destroy(booster.gameObject);
                    Destroy(boosterUI.gameObject);
                }
            }

            // close shop (visual)
            GridManager.instance.ToggleShop(false);

            // reset 'minefield' utility
            checkDelay = 0;

            // check for booster building
            if (lastTryingSquares.Count > 0)
            {
                // set cooldown
                currentCooldown = cooldown;

                // check for 'minefield' booster
                if(boosterType == BoosterType.BattlefieldCleaner)
                {
                    foreach(Square square in lastTryingSquares)
                    {
                        Enemy enemy = square.GetItem() as Enemy;
                        if (enemy)
                            enemy.FreeSquare(true);
                    }
                }
                else if(boosterType == BoosterType.Minefield)
                {
                    foreach(Square square in lastTryingSquares)
                    {
                        // load mines upon squares
                        GameObject minePrefab = Resources.Load<GameObject>("Prefabs/Boosters/Mine");
                        GameObject minePrefabUI = Resources.Load<GameObject>("Prefabs/Boosters/Boosters (UI)/Mine");
                        Booster mine = Instantiate(minePrefab).GetComponent<Booster>();
                        mine.transform.position = 
                            new Vector3(square.transform.position.x, square.transform.position.y + Random.Range(5, 8), square.transform.position.z);

                        // animate falling for every mine separately with updating square data about current booster (mine)
                        Tween.Position(mine.transform, square.transform.position, .75f, 0, /*mineFallCurve,*/ 
                        completeCallback: () =>
                        {
                            // set booster
                            square.SetItem(mine);

                            // spawn 2D booster reference
                            Transform mineUI = Instantiate(minePrefabUI, GameObject.Find("Game Field (UI)").transform).transform;
                            mine.SetBoosterUI(mineUI.gameObject);
                            mineUI.position = mine.transform.position;

                            // set squares
                            mine.SetSquares(square);

                            // health bar
                            mine.LoadHealthBar();
                            mine.HealthBar.SetReferenceToUI(mineUI.GetComponentInChildren<TMPro.TextMeshProUGUI>());
                        });
                    } 
                }
                // others
                else
                {
                    // build up
                    lastTryingSquares.ForEach(t => t.SetItem(booster));

                    // set squares
                    booster.SetSquares(lastTryingSquares);

                    // health bar
                    booster.LoadHealthBar();
                    booster.HealthBar.SetReferenceToUI(boosterUI.GetComponentInChildren<TMPro.TextMeshProUGUI>());

                    // place booster (animation utility)
                    booster.Place();
                }

                // clear squares list
                lastTryingSquares.RemoveRange(0, lastTryingSquares.Count);

                // toggle right action
                if(GameManager.instance.IsStageNumberSix())
                    GameManager.instance.RightAction();
            }

            // reset utils
            booster = null;
            boosterUI = null;
            canBeBuilt = false;
        }
    }

    #endregion

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
        }

        return boosterName;
    }
    // get booster image 
    private GameObject GetBoosterIcon()
    {
        string path = "Prefabs/Boosters/Boosters Icons/";
        switch (boosterType)
        {
            case BoosterType.Scarecrow: path += "Scarecrow"; break;
            case BoosterType.HorizontalThorns: path += "Horizontal Thorns"; break;
            case BoosterType.VerticalThorns: path += "Vertical Thorns"; break;
            case BoosterType.PotatoThrower: path += "Potato Thrower"; break;
            case BoosterType.Fan: path += "Fan"; break;
            case BoosterType.MiniMine: path += "Mini Mine"; break;
            case BoosterType.Minefield: case BoosterType.BattlefieldCleaner: path += "Minefield"; break;
        }

        return Resources.Load<GameObject>(path);
        //return Resources.Load<GameObject>("Prefabs/Boosters/BoostersIcons/Scarecrow");
    }
    // get all interactive squares for this booster
    private List<Square> GetTryingSquares(Square square, Vector3 rayHitPoint)
    {
        List<Square> tryingSquares = new List<Square>();
        List<GridSide> gridSide = GridManager.instance.GetGridSide(square);

        switch (boosterType)
        {
            case BoosterType.Scarecrow:
            case BoosterType.MiniMine:
            {
                tryingSquares.Add(square);
            }
            break;
            case BoosterType.HorizontalThorns:
            {
                // add selected square
                tryingSquares.Add(square);

                // enter grid from left side
                if (gridSide.IndexOf(GridSide.Left) >= 0)
                {
                    tryingSquares.Add(GridManager.instance.GetSquare(square, 0, 1));
                }
                // enter grid from right side
                else if (gridSide.IndexOf(GridSide.Right) >= 0)
                {
                    tryingSquares.Add(GridManager.instance.GetSquare(square, 0, -1));
                }
                else
                {
                    if (GetHorizontalSquareSide(square.transform.position, rayHitPoint) == SquareSide.Left)
                        tryingSquares.Add(GridManager.instance.GetSquare(square, 0, -1));
                    else
                        tryingSquares.Add(GridManager.instance.GetSquare(square, 0, 1));
                }
            }
            break;
            case BoosterType.VerticalThorns:
            {
                // add selected square
                tryingSquares.Add(square);

                // enter grid from top side
                if (gridSide.IndexOf(GridSide.Top) >= 0)
                {
                    tryingSquares.Add(GridManager.instance.GetSquare(square, -1, 0));
                }
                // enter grid from bottom side
                else if (gridSide.IndexOf(GridSide.Bottom) >= 0)
                {
                    tryingSquares.Add(GridManager.instance.GetSquare(square, 1, 0));
                }
                else
                {
                    if (GetVerticalSquareSide(square.transform.position, rayHitPoint) == SquareSide.Top)
                        tryingSquares.Add(GridManager.instance.GetSquare(square, 1, 0));
                    else
                        tryingSquares.Add(GridManager.instance.GetSquare(square, -1, 0));
                }
            }
            break;
            case BoosterType.PotatoThrower:
            {
                // add selected square
                tryingSquares.Add(square);

                // angle enter
                if (gridSide.Count >= 2)
                {
                    if (gridSide.IndexOf(GridSide.Top) >= 0)
                    {
                        tryingSquares.Add(GridManager.instance.GetSquare(square, -1, 0));
                        if (gridSide.IndexOf(GridSide.Left) >= 0)
                        {
                            tryingSquares.Add(GridManager.instance.GetSquare(square, 0, 1));
                            tryingSquares.Add(GridManager.instance.GetSquare(square, -1, 1));
                        }
                        else if (gridSide.IndexOf(GridSide.Right) >= 0)
                        {
                            tryingSquares.Add(GridManager.instance.GetSquare(square, 0, -1));
                            tryingSquares.Add(GridManager.instance.GetSquare(square, -1, -1));
                        }
                    }
                    else if (gridSide.IndexOf(GridSide.Bottom) >= 0)
                    {
                        tryingSquares.Add(GridManager.instance.GetSquare(square, 1, 0));
                        if (gridSide.IndexOf(GridSide.Left) >= 0)
                        {
                            tryingSquares.Add(GridManager.instance.GetSquare(square, 0, 1));
                            tryingSquares.Add(GridManager.instance.GetSquare(square, 1, 1));
                        }
                        else if (gridSide.IndexOf(GridSide.Right) >= 0)
                        {
                            tryingSquares.Add(GridManager.instance.GetSquare(square, 0, -1));
                            tryingSquares.Add(GridManager.instance.GetSquare(square, 1, -1));
                        }
                    }
                }
                // one side enter
                else if (gridSide.Count == 1)
                {
                    if (gridSide.IndexOf(GridSide.Top) >= 0)
                    {
                        tryingSquares.Add(GridManager.instance.GetSquare(square, -1, 0));
                        if (GetHorizontalSquareSide(square.transform.position, rayHitPoint) == SquareSide.Left)
                        {
                            tryingSquares.Add(GridManager.instance.GetSquare(square, 0, -1));
                            tryingSquares.Add(GridManager.instance.GetSquare(square, -1, -1));
                        }
                        else
                        {
                            tryingSquares.Add(GridManager.instance.GetSquare(square, 0, 1));
                            tryingSquares.Add(GridManager.instance.GetSquare(square, -1, 1));
                        }
                    }
                    else if (gridSide.IndexOf(GridSide.Bottom) >= 0)
                    {
                        tryingSquares.Add(GridManager.instance.GetSquare(square, 1, 0));
                        if (GetHorizontalSquareSide(square.transform.position, rayHitPoint) == SquareSide.Left)
                        {
                            tryingSquares.Add(GridManager.instance.GetSquare(square, 0, -1));
                            tryingSquares.Add(GridManager.instance.GetSquare(square, 1, -1));
                        }
                        else
                        {
                            tryingSquares.Add(GridManager.instance.GetSquare(square, 0, 1));
                            tryingSquares.Add(GridManager.instance.GetSquare(square, 1, 1));
                        }
                    }
                    else if (gridSide.IndexOf(GridSide.Left) >= 0)
                    {
                        tryingSquares.Add(GridManager.instance.GetSquare(square, 0, 1));
                        if (GetVerticalSquareSide(square.transform.position, rayHitPoint) == SquareSide.Top)
                        {
                            tryingSquares.Add(GridManager.instance.GetSquare(square, 1, 0));
                            tryingSquares.Add(GridManager.instance.GetSquare(square, 1, 1));
                        }
                        else
                        {
                            tryingSquares.Add(GridManager.instance.GetSquare(square, -1, 0));
                            tryingSquares.Add(GridManager.instance.GetSquare(square, -1, 1));
                        }
                    }
                    else
                    {
                        tryingSquares.Add(GridManager.instance.GetSquare(square, 0, -1));
                        if (GetVerticalSquareSide(square.transform.position, rayHitPoint) == SquareSide.Top)
                        {
                            tryingSquares.Add(GridManager.instance.GetSquare(square, 1, 0));
                            tryingSquares.Add(GridManager.instance.GetSquare(square, 1, -1));
                        }
                        else
                        {
                            tryingSquares.Add(GridManager.instance.GetSquare(square, -1, 0));
                            tryingSquares.Add(GridManager.instance.GetSquare(square, -1, -1));
                        }
                    }
                }
                // moving inside
                else
                {
                    int columnOffset = GetHorizontalSquareSide(square.transform.position, rayHitPoint) == SquareSide.Left ? -1 : 1,
                        rowOffset = GetVerticalSquareSide(square.transform.position, rayHitPoint) == SquareSide.Top ? 1 : -1;

                    tryingSquares.Add(GridManager.instance.GetSquare(square, rowOffset, 0));
                    tryingSquares.Add(GridManager.instance.GetSquare(square, 0, columnOffset));
                    tryingSquares.Add(GridManager.instance.GetSquare(square, rowOffset, columnOffset));
                }

            }
            break;
            case BoosterType.Fan:
            {
                int squareColumn = GridManager.instance.GetSquareColumn(square);
                for (int i = 0; i < GridManager.instance.Amount; i++)
                    tryingSquares.Add(GridManager.instance.GetSquare(i, squareColumn));
            }
            break;
        }

        return tryingSquares;
    }
    // get square touched side on the vertical
    private SquareSide GetVerticalSquareSide(Vector3 position, Vector3 rayHitPoint) => rayHitPoint.z > position.z ? SquareSide.Top : SquareSide.Bottom;
    // get square touched side on the horizontal
    private SquareSide GetHorizontalSquareSide(Vector3 position, Vector3 rayHitPoint) => rayHitPoint.x < position.x ? SquareSide.Left : SquareSide.Right;
    // get booster position between squares (in the mid)
    private Vector3 GetBoosterPosition(List<Square> squares)
    {
        if (boosterType == BoosterType.Fan)
        {
            Vector3 fanPosition;
            float squareScaleUnit;
            Square square = squares[0];
            int columnIndex = GridManager.instance.GetSquareColumn(square);

            square = GridManager.instance.GetSquare(0, columnIndex);
            fanPosition = square.transform.position;
            squareScaleUnit = GridManager.instance.GetSquareScaleUnit();

            return new Vector3(fanPosition.x, fanPosition.y, fanPosition.z - squareScaleUnit * 2 / 3);
        }
        else
        {
            float minX, minZ, maxX, maxZ;
            minX = maxX = squares[0].transform.position.x;
            minZ = maxZ = squares[0].transform.position.z;

            for (int i = 0; i < squares.Count; i++)
            {
                Vector3 sPos = squares[i].transform.position;

                if (sPos.x <= minX) minX = sPos.x;
                else if (sPos.x > maxX) maxX = sPos.x;

                if (sPos.z <= minZ) minZ = sPos.z;
                else if (sPos.z > maxZ) maxZ = sPos.z;
            }

            return new Vector3(minX + Mathf.Abs(maxX - minX) / 2, squares[0].transform.position.y, minZ + Mathf.Abs(maxZ - minZ) / 2);
        }
    }
}
public enum SquareSide
{
    Left = 0,
    Right = 1,
    Top = 2,
    Bottom = 3
}
