using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

// Enum representing different types of tiles in the maze
public enum TileType
{
    Floor,
    Wall,
    OuterWall,
    StartPoint,
    ExitPoint
}

// Class responsible for generating a procedural maze
public class ProceduralMazeGenerator : MonoBehaviour
{
    [Header("Maze Size Settings")]
    public int Width = 10;               // Width of the maze
    public int Height = 10;              // Height of the maze

    [Header("Tile settings")]
    public Tilemap Tilemap;              // Reference to the Tilemap component
    public TileBase FloorTile;           // Tile used for floor cells
    public TileBase WallTile;            // Tile used for wall cells
    public TileBase OuterWallTile;       // Tile used for outer wall cells
    public TileBase StartPointTile;      // Tile used for the start point cell
    public TileBase ExitPointTile;       // Tile used for the exit point cell

    [Header("Maze Settings")]
    [Range(0, 1), Tooltip("The higher the number, the slower the generation")] public float GenerationSlowness = 0.1f;  // Speed at which the maze is generated
    public bool InstantGeneration; // Flag indicating whether the maze should be generated instantly

    [Header("UI Elements")]
    public GameObject GenerationSlownessSlider; // The silder object for the Maze Generation Speed
    public GameObject MazeWidthInput; // The input field for the Maze Width
    public GameObject MazeHeightInput;  // The input field for the Maze Height
    public GameObject MazeInstantGen; // The toggle UI for instant generation

    private TMP_InputField _mazeWidth;
    private TMP_InputField _mazeHeight;
    private Slider _mazeGenerationSlowness;
    private Toggle _mazeInstantGenToggle;

    private bool[,] _maze;                // 2D array representing the maze cells
    private Coroutine _generationCoroutine;  // Reference to the maze generation coroutine

    private void Awake()
    {
        _mazeGenerationSlowness = GenerationSlownessSlider.GetComponent<Slider>();
        _mazeWidth = MazeWidthInput.GetComponent<TMP_InputField>();
        _mazeHeight = MazeHeightInput.GetComponent<TMP_InputField>();
        _mazeInstantGenToggle = MazeInstantGen.GetComponent<Toggle>();
    }

    private void Update()
    {
        if (String.IsNullOrEmpty(_mazeHeight.text) || String.IsNullOrEmpty(_mazeWidth.text)) return;

        Height = int.Parse(_mazeHeight.text);
        Width = int.Parse(_mazeWidth.text);
        GenerationSlowness = _mazeGenerationSlowness.value;
        InstantGeneration = _mazeInstantGenToggle.isOn;
        //test
    }

    /// <summary>
    /// Generates a new maze
    /// </summary>
    public void GenerateMaze()
    {
        if (Width <= 0 || Height <= 0 ) return;
        ClearMaze();

        // Ensure odd dimensions for the maze
        Width = (Width % 2 == 0) ? Width + 1 : Width;
        Height = (Height % 2 == 0) ? Height + 1 : Height;

        _maze = new bool[Width, Height];

        // Initialize maze cells and set outer walls
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (x == 0 || x == Width - 1 || y == 0 || y == Height - 1)
                {
                    _maze[x, y] = true;
                    Tilemap.SetTile(new Vector3Int(x, y, 0), OuterWallTile);
                }
                else if (x % 2 == 0 || y % 2 == 0)
                {
                    _maze[x, y] = true;
                    Tilemap.SetTile(new Vector3Int(x, y, 0), WallTile);
                }
            }
        }

        // Stop the current generation coroutine, if any
        if (_generationCoroutine != null) StopCoroutine(_generationCoroutine);

        if (InstantGeneration) GenerateMazeInstantly();
        else _generationCoroutine = StartCoroutine(GenerateMazeCoroutine());
    }

    /// <summary>
    /// Coroutine for generating the maze over time
    /// </summary>
    /// <returns></returns>
    private IEnumerator GenerateMazeCoroutine()
    {
        WaitForSeconds wait = new WaitForSeconds(GenerationSlowness);

        List<Vector2Int> visitedCells = new List<Vector2Int>();
        Stack<Vector2Int> cellStack = new Stack<Vector2Int>();

        Vector2Int currentCell = new Vector2Int(1, 1);
        visitedCells.Add(currentCell);
        cellStack.Push(currentCell);
        _maze[currentCell.x, currentCell.y] = true;
        Tilemap.SetTile(new Vector3Int(currentCell.x, currentCell.y, 0), FloorTile);

        while (cellStack.Count > 0)
        {
            yield return wait;

            // Get unvisited neighboring cells
            List<Vector2Int> neighbors = GetUnvisitedNeighbors(currentCell);

            if (neighbors.Count > 0)
            {
                // Choose a random neighbor
                Vector2Int randomNeighbor = neighbors[UnityEngine.Random.Range(0, neighbors.Count)];

                // Remove the wall between the current cell and the chosen neighbor
                RemoveWallBetweenCells(currentCell, randomNeighbor);

                currentCell = randomNeighbor;
                visitedCells.Add(currentCell);
                cellStack.Push(currentCell);
                _maze[currentCell.x, currentCell.y] = true;
                Tilemap.SetTile(new Vector3Int(currentCell.x, currentCell.y, 0), FloorTile);
            }
            else
            {
                currentCell = cellStack.Pop();
            }
        }

        // Create start and exit points in the maze
        MakeStartAndExit();

        _generationCoroutine = null;
    }

    /// <summary>
    /// Generates the maze instantly without gradual updates
    /// </summary>
    private void GenerateMazeInstantly()
    {
        List<Vector2Int> visitedCells = new List<Vector2Int>();
        Stack<Vector2Int> cellStack = new Stack<Vector2Int>();

        Vector2Int currentCell = new Vector2Int(1, 1);
        visitedCells.Add(currentCell);
        cellStack.Push(currentCell);
        _maze[currentCell.x, currentCell.y] = true;
        Tilemap.SetTile(new Vector3Int(currentCell.x, currentCell.y, 0), FloorTile);

        while (cellStack.Count > 0)
        {
            List<Vector2Int> neighbors = GetUnvisitedNeighbors(currentCell);

            if (neighbors.Count > 0)
            {
                Vector2Int randomNeighbor = neighbors[UnityEngine.Random.Range(0, neighbors.Count)];

                RemoveWallBetweenCells(currentCell, randomNeighbor);

                currentCell = randomNeighbor;
                visitedCells.Add(currentCell);
                cellStack.Push(currentCell);
                _maze[currentCell.x, currentCell.y] = true;
                Tilemap.SetTile(new Vector3Int(currentCell.x, currentCell.y, 0), FloorTile);
            }
            else
            {
                currentCell = cellStack.Pop();
            }
        }

        MakeStartAndExit();
    }

    /// <summary>
    /// Returns a list of unvisited neighboring cells for the given cell
    /// </summary>
    /// <param name="cell">Cell is the current cell it's on</param>
    /// <returns></returns>
    private List<Vector2Int> GetUnvisitedNeighbors(Vector2Int cell)
    {
        List<Vector2Int> unvisitedNeighbors = new List<Vector2Int>();

        if (cell.x > 1 && !_maze[cell.x - 2, cell.y])
        {
            unvisitedNeighbors.Add(new Vector2Int(cell.x - 2, cell.y));
        }

        if (cell.x < Width - 2 && !_maze[cell.x + 2, cell.y])
        {
            unvisitedNeighbors.Add(new Vector2Int(cell.x + 2, cell.y));
        }

        if (cell.y > 1 && !_maze[cell.x, cell.y - 2])
        {
            unvisitedNeighbors.Add(new Vector2Int(cell.x, cell.y - 2));
        }

        if (cell.y < Height - 2 && !_maze[cell.x, cell.y + 2])
        {
            unvisitedNeighbors.Add(new Vector2Int(cell.x, cell.y + 2));
        }

        return unvisitedNeighbors;
    }

    /// <summary>
    /// Removes the wall between two adjacent cells
    /// </summary>
    /// <param name="currentCell">Cell is the current cell it's on</param>
    /// <param name="neighborCell">The cell that is neighboring the currentcell</param>
    private void RemoveWallBetweenCells(Vector2Int currentCell, Vector2Int neighborCell)
    {
        int wallX = currentCell.x + (neighborCell.x - currentCell.x) / 2;
        int wallY = currentCell.y + (neighborCell.y - currentCell.y) / 2;
        _maze[wallX, wallY] = true;
        Tilemap.SetTile(new Vector3Int(wallX, wallY, 0), FloorTile);
    }

    /// <summary>
    /// Creates the start and exit points in the maze
    /// </summary>
    private void MakeStartAndExit()
    {
        List<Vector2Int> outerWallCells = new List<Vector2Int>();

        // Identify cells on the outer walls of the maze
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (x == 0 || x == Width - 1 || y == 0 || y == Height - 1)
                {
                    outerWallCells.Add(new Vector2Int(x, y));
                }
            }
        }

        // Set the start and exit points
        Tilemap.SetTile(new Vector3Int(1, 1, 0), FloorTile);
        Tilemap.SetTile(new Vector3Int(Width - 2, Height - 2, 0), FloorTile);

        // Choose a random valid cell on the outer walls for the start point
        Vector2Int startPoint = GetRandomValidCell(outerWallCells);
        outerWallCells.Remove(startPoint);
        Tilemap.SetTile(new Vector3Int(startPoint.x, startPoint.y, 0), StartPointTile);
        outerWallCells.RemoveAll(cell => cell == startPoint);

        // Choose a random valid cell on the remaining outer walls for the exit point
        Vector2Int exitPoint = GetRandomValidCell(outerWallCells);
        Tilemap.SetTile(new Vector3Int(exitPoint.x, exitPoint.y, 0), ExitPointTile);

        Debug.Log("Maze generation finished.");
    }

    // Returns a random valid cell from the given list of cells
    private Vector2Int GetRandomValidCell(List<Vector2Int> cells)
    {
        List<Vector2Int> validCells = GetValidAdjacentCells(cells);
        return validCells[UnityEngine.Random.Range(0, validCells.Count)];
    }

    /// <summary>
    /// Returns a list of valid adjacent cells for the given list of cells
    /// </summary>
    /// <param name="cells">Cell is the current cell it's on</param>
    /// <returns></returns>
    private List<Vector2Int> GetValidAdjacentCells(List<Vector2Int> cells)
    {
        List<Vector2Int> validCells = new List<Vector2Int>();
        foreach (Vector2Int cell in cells)
        {
            if (IsAdjacentToFloorTile(cell))
            {
                validCells.Add(cell);
            }
        }
        return validCells;
    }

    /// <summary>
    /// Checks if a given cell is adjacent to a floor tile
    /// </summary>
    /// <param name="cell">Cell is the current cell it's checking</param>
    /// <returns></returns>
    private bool IsAdjacentToFloorTile(Vector2Int cell)
    {
        int x = cell.x;
        int y = cell.y;

        if (x > 0 && Tilemap.GetTile(new Vector3Int(x - 1, y, 0)) == FloorTile)
            return true;

        if (x < Width - 1 && Tilemap.GetTile(new Vector3Int(x + 1, y, 0)) == FloorTile)
            return true;

        if (y > 0 && Tilemap.GetTile(new Vector3Int(x, y - 1, 0)) == FloorTile)
            return true;

        if (y < Height - 1 && Tilemap.GetTile(new Vector3Int(x, y + 1, 0)) == FloorTile)
            return true;

        return false;
    }

    /// <summary>
    /// Clears the maze by removing all tiles from the tilemap
    /// </summary>
    private void ClearMaze()
    {
        Tilemap.ClearAllTiles();
    }
}
