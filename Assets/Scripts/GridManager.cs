using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class GridManager : MonoBehaviour
{
    #region Singleton

    public static GridManager instance;
    private void Awake()
    {
        instance = this;
    }

    #endregion

    [SerializeField]
    private float gridLength; // length of grid (width&height)
    public float GridLength { get { return gridLength; } }

    [SerializeField]
    private int amount; // amount of squares in the grid (MxM)
    public int Amount { get { return amount; } }

    [SerializeField]
    private float offset; // offset between squares
    public float Offset { get { return offset; } }

    #region Square Utils

    private float graphicsWidth;
    private float squareScaleUnit;
    private float graphicsScaleUnit;

    [SerializeField]
    private List<Material> materialsList;

    [SerializeField]
    private Color defaultBorderColor; // start default border color

    #endregion

    [SerializeField]
    private GameObject defaultSquarePrefab; // just a squares to load

    private Transform gridParent; // just a parent empty object

    private List<List<Square>> squares; // list of all squares

    private bool isShopOpen = false; // shop state

    private void Start()
    {
        Init();
        LoadGameFieldGrid();
        StartCoroutine(LoadRandomBarriers());
    }

    // get shop state
    public bool IsShopOpen() => isShopOpen;
    // toggle shop (on/off)
    public void ToggleShop(bool state)
    {
        // set materials
        for (int i = 0; i < squares.Count; i++)
            for (int j = 0; j < squares[i].Count; j++)
                squares[i][j].ToggleShopState(state);

        // toggle shop state
        isShopOpen = state;
    }
    public void TrySetSquaresBorderColorIntensity(float intensity)
    {
        Color newColor;
        float factor = intensity * intensity;
        newColor = defaultBorderColor * factor;

        // change color for all materials
        for (int i = 0; i < materialsList.Count; i++)
            if (materialsList[i].HasProperty("MultiplyColor"))
                materialsList[i].SetColor("MultiplyColor", newColor);

        // try to change current squares color
        for (int i = 0; i < squares.Count; i++)
            for (int j = 0; j < squares[i].Count; j++)
                squares[i][j].TrySetCurrentBorderColor(newColor);
    }
    // get all enemies squares (enemies are currently on)
    public List<Square> GetEnemiesSquares()
    {
        List<Square> enemiesSquares = new List<Square>();
        for(int i = 0; i < squares.Count; i++)
        {
            for(int j = 0; j < squares[i].Count; j++)
            {
                Square square = squares[i][j];
                if (square.GetItem() as Enemy)
                    enemiesSquares.Add(square);
            }
        }

        return enemiesSquares;
    }
    // get free from boosters square
    public Square GetFreeFromBoostersSquare()
    {
        Square randSquare = null;
        while (!randSquare)
        {
            randSquare = squares[Random.Range(0, amount)][Random.Range(0, amount)];
            if (!(randSquare.GetItem() as Booster))
                break;
            else
                randSquare = null;
        }

        return randSquare;
    }
    // get all free from boosters squares
    public List<Square> GetFreeFromBoostersSquares(int amount)
    {
        List<Square> freeSquares = new List<Square>();
        List<Square> squaresList = new List<Square>();

        // get all free squares
        for(int i = 0; i < squares.Count; i++)
        {
            for(int j = 0; j < squares[i].Count; j++)
            {
                Square square = squares[i][j];
                if (!(square.GetItem() as Booster)) freeSquares.Add(square);
            }
        }

        // get needed amount of free squares
        if (freeSquares.Count > amount)
        {
            while (squaresList.Count < amount)
            {
                Square square = freeSquares[Random.Range(0, freeSquares.Count)];
                squaresList.Add(square);
                freeSquares.Remove(square);
            }
        }
        else
            squaresList = freeSquares;

        return squaresList;
    }
    // get all free from enemies squares
    public List<Square> GetFreeFromEnemiesSquares()
    {
        List<Square> squares = new List<Square>();
        for (int i = 0; i < this.squares.Count; i++)
            for (int j = 0; j < this.squares[i].Count; j++)
            {
                Square square = this.squares[i][j];
                if (!(square.GetItem() as Enemy) && !square.IsTargeting) squares.Add(this.squares[i][j]);
            }

        return squares;
    }
    public bool HasFreeFromEnemiesSquares()
    {
        for (int i = 0; i < this.squares.Count; i++)
            for (int j = 0; j < this.squares[i].Count; j++)
            {
                Square square = this.squares[i][j];
                if (!(square.GetItem() as Enemy) && !square.IsTargeting) return true;
            }

        return false;
    }
    // get square scale
    public float GetGraphicsScaleUnit() => graphicsScaleUnit;

    #region Building Utils

    public int GetSquareColumn(Square square)
    {
        int squareColumn = -1;
        for (int i = 0; i < squares.Count; i++)
            for (int j = 0; j < squares[i].Count; j++)
                if (squares[i][j] == square) squareColumn = j;

        return squareColumn;
    }
    public float GetSquareScaleUnit() => squareScaleUnit;
    public List<Square> GetSquares(Item item)
    {
        List<Square> squares = new List<Square>();
        for (int i = 0; i < this.squares.Count; i++)
        {
            for (int j = 0; j < this.squares[i].Count; j++)
            {
                Square square = this.squares[i][j];
                if (square.GetItem() == item)
                    squares.Add(square);
            }
        }

        return squares;
    }
    public Square GetSquare(int row, int column) => squares[row][column];
    public Square GetSquare(Square square, int rowOffset, int columnOffset)
    {
        int currentRow = -1, currentColumn = -1;
        for (int i = 0; i < squares.Count; i++)
        {
            for (int j = 0; j < squares[i].Count; j++)
            {
                if (square == squares[i][j])
                {
                    currentRow = i;
                    currentColumn = j;
                }
            }
        }
        
        return squares[currentRow + rowOffset][currentColumn + columnOffset];
    }
    public List<GridSide> GetGridSide(Square square)
    {
        List<GridSide> gridSideList = new List<GridSide>();
        int leftColumn = 0, rightColumn = squares[0].Count - 1,
            topRow = squares.Count - 1, bottomRow = 0;

        for (int i = 0; i < squares.Count; i++)
        {
            if (squares[i][leftColumn] == square)
                gridSideList.Add(GridSide.Left);
            if (squares[i][rightColumn] == square)
                gridSideList.Add(GridSide.Right);
            if (squares[topRow][i] == square)
                gridSideList.Add(GridSide.Top);
            if (squares[bottomRow][i] == square)
                gridSideList.Add(GridSide.Bottom);
        }

        return gridSideList;
    }

    #endregion

    #region Start Settings

    private void Init()
    {
        // create grid parent
        gridParent = new GameObject("Game Field Grid").transform;

        // create square list
        squares = new List<List<Square>>();
        for (int i = 0; i < amount; i++)
            squares.Add(new List<Square>());

        for (int i = 0; i < materialsList.Count; i++)
        {
            if (materialsList[i].HasProperty("MultiplyColor"))
            {
                float factor = 2.5f * 2.5f;
                materialsList[i].SetColor("MultiplyColor", defaultBorderColor * factor);
            }
        }
    }
    // load all squares
    private void LoadGameFieldGrid()
    {
        // get square scale, [def. scale = (1, 1, 1)]
        graphicsWidth = (gridLength - offset * (amount - 1)) / amount;
        squareScaleUnit = graphicsWidth + offset;
        graphicsScaleUnit = (squareScaleUnit - offset) / squareScaleUnit;

        // load squares(will be changed to prefabs with shaders and scripts I suppose)
        for (int z = amount - 1; z >= 0; z--)
        {
            float zSquarePosition = gridParent.position.x - gridLength / 2 + squareScaleUnit / 2 + squareScaleUnit * z;
            for (int x = 0; x < amount; x++)
            {
                // place the square in a right place in the grid and set scale
                GameObject square = Instantiate(defaultSquarePrefab, gridParent);
                Vector3 squareScale = new Vector3(squareScaleUnit, squareScaleUnit, squareScaleUnit);
                Vector3 graphicsScale = new Vector3(graphicsScaleUnit, graphicsScaleUnit, graphicsScaleUnit);
                float xSquarePosition = gridParent.position.y - gridLength / 2 + squareScaleUnit / 2 + squareScaleUnit * x;
                square.transform.localScale = squareScale;
                square.transform.GetChild(0).localScale = graphicsScale;
                square.transform.position = new Vector3(xSquarePosition, square.transform.position.y, zSquarePosition);
                square.name = "Square";

                // add square to list for further management
                squares[z].Add(square.GetComponent<Square>());
            }
        }
    }
    // load random help barriers
    private IEnumerator LoadRandomBarriers()
    {
        int barriersAmount = Random.Range(0, amount);
        for(int i = 0; i < barriersAmount; i++)
        {
            Square square = GetFreeFromBoostersSquare();
            string barrierName = GetRandomBarrierName();

            GameObject barrierPrefab = Resources.Load<GameObject>("Prefabs/Boosters/" + barrierName);
            Transform barrierTransform = Instantiate(barrierPrefab).transform;
            Vector3 barrierPosition = square.transform.position;
            barrierPosition.y = 0;
            barrierTransform.position = barrierPosition;
            barrierTransform.name = barrierName;

            GameObject barrierPrefabUI = Resources.Load<GameObject>("Prefabs/Boosters/Boosters (UI)/" + barrierName);
            Transform boosterUI = Instantiate(barrierPrefabUI, GameObject.Find("Game Field (UI)").transform).transform;
            boosterUI.position = barrierPosition;
            boosterUI.name = barrierName;

            Booster barrier = barrierTransform.GetComponent<Booster>();
            if (barrier != null)
            {
                square.SetBarrier(barrier);
                barrier.SetSquares(square);
                barrier.LoadHealthBar();
                barrier.HealthBar.SetReferenceToUI(boosterUI.GetComponentInChildren<TMPro.TextMeshProUGUI>());
                barrier.SetBoosterUI(boosterUI.gameObject);

                // utility
                if (barrier.BoosterType == BoosterType.Scarecrow)
                    barrier.Place();
            }

            yield return null;
        }
    }
    // get random help barrier name
    private string GetRandomBarrierName()
    {
        return new string[4]
        {
            "Fire Hydrant",
            "Cactus",
            "Sign Post",
            "Scarecrow"
        }[Random.Range(0, 4)];
    }

    #endregion
}
public enum GridSide
{
    Left = 0,
    Right = 1,
    Top = 2,
    Bottom = 3
}