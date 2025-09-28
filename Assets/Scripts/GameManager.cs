using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private FruitSpawner spawner;

    [Header("UI Panels")]
    [SerializeField] private GameObject panelMenu;
    [SerializeField] private GameObject panelRecipe;
    [SerializeField] private GameObject panelGameHUD;
    [SerializeField] private GameObject panelWin;
    [SerializeField] private GameObject panelLose;

    [Header("UI Texts")]
    [SerializeField] private TMP_Text recipeText;       // On Panel_Recipe
    [SerializeField] private TMP_Text loseReasonText;   // On Panel_Lose
    [SerializeField] private TMP_Text hudCountsText;    // On Panel_GameHUD
    [SerializeField] private TMP_Text hudSequenceText;  // On Panel_GameHUD

    // ---- Level 0 (tutorial) data ----
    private readonly Dictionary<string, int> recipe = new Dictionary<string, int>();
    private readonly Dictionary<string, int> sliced = new Dictionary<string, int>();
    private readonly List<string> expectedSequence = new List<string>();
    private int currentIndex = 0;
    private bool gameActive = false;

    // Display order for counts in HUD (so it's not random)
    private readonly List<string> displayOrder = new List<string> { "Apple", "Banana", "Strawberry" };

    // Show recipe for 3s in Level 0
    private const float RECIPE_SHOW_SECONDS = 3f;

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

    // ---- UI State ----
    public void ShowMenu()
    {
        panelMenu.SetActive(true);
        panelRecipe.SetActive(false);
        panelGameHUD.SetActive(false);
        panelWin.SetActive(false);
        panelLose.SetActive(false);

        // Safety
        gameActive = false;
        currentIndex = 0;
    }

    // Called by Btn_Play (Panel_Menu)
    public void StartGame()
    {
        // Setup Level 0 fixed tutorial recipe+sequence
        BuildLevel0();

        // Show recipe panel with text for 3 seconds
        panelMenu.SetActive(false);
        panelRecipe.SetActive(true);
        panelGameHUD.SetActive(false);
        panelWin.SetActive(false);
        panelLose.SetActive(false);

        recipeText.text = BuildRecipeDisplayText();

        Invoke(nameof(BeginPlay), RECIPE_SHOW_SECONDS);
    }

    private void BeginPlay()
    {
        panelRecipe.SetActive(false);
        panelGameHUD.SetActive(true);

        currentIndex = 0;
        UpdateHUD();

        gameActive = true;
        if (spawner != null) spawner.StartSpawning();
    }

    // ---- Level 0 setup ----
    private void BuildLevel0()
    {
        recipe.Clear();
        sliced.Clear();
        expectedSequence.Clear();

        // Fixed tutorial: Apple×1, Banana×2, Strawberry×1
        recipe["Apple"] = 1;
        recipe["Banana"] = 2;
        recipe["Strawberry"] = 1;

        // Init sliced to 0
        foreach (var kv in recipe)
            sliced[kv.Key] = 0;

        // Expected sequence (fixed order for tutorial)
        expectedSequence.Add("Apple");
        expectedSequence.Add("Banana");
        expectedSequence.Add("Banana");
        expectedSequence.Add("Strawberry");
    }

    private string BuildRecipeDisplayText()
    {
        // e.g., "Recipe: Apple×1  Banana×2  Strawberry×1\nOrder: Apple → Banana → Banana → Strawberry"
        var parts = new List<string>();
        foreach (var key in displayOrder)
            if (recipe.ContainsKey(key))
                parts.Add($"{key}×{recipe[key]}");

        string countsLine = "Recipe: " + string.Join("  ", parts);
        string seqLine = "Order: " + string.Join(" → ", expectedSequence);
        return countsLine + "\n" + seqLine;
    }

    private void UpdateHUD()
    {
        // Counts (multiline)
        var lines = new List<string>();
        foreach (var key in displayOrder)
        {
            if (!recipe.ContainsKey(key)) continue;
            int have = sliced.ContainsKey(key) ? sliced[key] : 0;
            int need = recipe[key];
            lines.Add($"{key}: {have}/{need}");
        }
        if (hudCountsText != null) hudCountsText.text = string.Join("\n", lines);

        // Sequence + next
        string seq = string.Join(" → ", expectedSequence);
        string next = (currentIndex < expectedSequence.Count) ? expectedSequence[currentIndex] : "—";
        if (hudSequenceText != null) hudSequenceText.text = $"Sequence: {seq}\nNext: {next}";
    }

    // Called by Fruit.cs when blade slices a fruit
    public void OnFruitSliced(Fruit fruit)
    {
        if (!gameActive || fruit == null) return;

        string name = fruit.fruitName;

        // If fruit not in recipe at all → wrong fruit
        if (!recipe.ContainsKey(name))
        {
            EndGame(false, $"Wrong fruit: {name}");
            return;
        }

        // Enforce order strictly
        if (currentIndex >= expectedSequence.Count)
        {
            // Already done but something else sliced -> treat as extra
            EndGame(false, $"Extra slice: {name}");
            return;
        }

        string expected = expectedSequence[currentIndex];
        if (name != expected)
        {
            EndGame(false, $"Wrong order! Expected {expected}, got {name}");
            return;
        }

        // Update count
        sliced[name] = (sliced.TryGetValue(name, out var v) ? v : 0) + 1;

        // No extras of any fruit
        if (sliced[name] > recipe[name])
        {
            EndGame(false, $"Too many {name}s!");
            return;
        }

        currentIndex++;
        UpdateHUD();

        // Completed sequence exactly
        if (currentIndex >= expectedSequence.Count)
        {
            EndGame(true, null);
        }
    }

    private void EndGame(bool win, string reason)
    {
        gameActive = false;
        if (spawner != null) spawner.StopSpawning();

        panelGameHUD.SetActive(false);

        if (win)
        {
            panelWin.SetActive(true);
        }
        else
        {
            panelLose.SetActive(true);
            if (loseReasonText != null) loseReasonText.text = reason;
        }
    }

    // Connected to Btn_Replay and Btn_Retry
    public void Replay()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
    }
}
