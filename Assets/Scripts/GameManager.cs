using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private FruitSpawner spawner;
    [SerializeField] private Canvas canvasMain;

    [Header("UI Panels")]
    [SerializeField] private GameObject panelMenu;
    [SerializeField] private GameObject panelRecipe;
    [SerializeField] private GameObject panelGameHUD;
    [SerializeField] private GameObject panelWin;
    [SerializeField] private GameObject panelLose;

    [Header("UI Texts")]
    [SerializeField] private TMP_Text recipeText;
    [SerializeField] private TMP_Text loseReasonText;

    // Recipe tracking
    private Dictionary<string, int> recipe = new Dictionary<string, int>();
    private Dictionary<string, int> sliced = new Dictionary<string, int>();

    private bool gameActive = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        ShowMenu();
    }

    public void ShowMenu()
    {
        panelMenu.SetActive(true);
        panelRecipe.SetActive(false);
        panelGameHUD.SetActive(false);
        panelWin.SetActive(false);
        panelLose.SetActive(false);
    }

    public void StartGame()
    {
        panelMenu.SetActive(false);
        panelRecipe.SetActive(true);
        panelGameHUD.SetActive(false);
        panelWin.SetActive(false);
        panelLose.SetActive(false);

        // Build a random recipe
        BuildRecipe();

        // Show it on screen
        string recipeStr = "Recipe:\n";
        foreach (var kvp in recipe)
            recipeStr += $"{kvp.Key}×{kvp.Value}\n";
        recipeText.text = recipeStr;

        // After short delay, hide recipe and start
        Invoke(nameof(BeginPlay), 3f);
    }

    private void BeginPlay()
    {
        panelRecipe.SetActive(false);
        panelGameHUD.SetActive(true);

        gameActive = true;
        spawner.StartSpawning();
    }

    private void BuildRecipe()
    {
        recipe.Clear();
        sliced.Clear();

        // Example: always 3 fruits, random counts
        recipe["Apple"] = Random.Range(1, 3); // 1–2 apples
        recipe["Banana"] = Random.Range(1, 3); // 1–2 bananas
        recipe["Strawberry"] = Random.Range(1, 2); // 1 strawberry

        // Init sliced tracker
        foreach (var key in recipe.Keys)
            sliced[key] = 0;
    }

    public void OnFruitSliced(Fruit fruit)
    {
        if (!gameActive) return;

        string name = fruit.fruitName;

        // Wrong fruit
        if (!recipe.ContainsKey(name))
        {
            EndGame(false, $"Wrong fruit: {name}");
            return;
        }

        // Count sliced
        sliced[name]++;

        // Too many of one fruit
        if (sliced[name] > recipe[name])
        {
            EndGame(false, $"Too many {name}s!");
            return;
        }

        // Check if recipe complete
        foreach (var key in recipe.Keys)
        {
            if (sliced[key] < recipe[key])
                return; // not done yet
        }

        EndGame(true, null);
    }

    private void EndGame(bool win, string reason)
    {
        gameActive = false;
        spawner.StopSpawning();

        panelGameHUD.SetActive(false);

        if (win)
        {
            panelWin.SetActive(true);
        }
        else
        {
            panelLose.SetActive(true);
            loseReasonText.text = reason;
        }
    }

    public void Replay()
    {
        // Reset scene quickly
        UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
    }
}
