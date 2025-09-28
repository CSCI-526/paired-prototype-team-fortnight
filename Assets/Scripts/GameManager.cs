using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Persist level within the same Play session
    public static int CurrentLevel = 0;

    private enum LevelMode { TutorialCountAndOrder, MemoryOrderOnly }
    private LevelMode mode;

    [Header("References")]
    [SerializeField] private FruitSpawner spawner;

    [Header("UI Panels")]
    [SerializeField] private GameObject panelMenu;
    [SerializeField] private GameObject panelRecipe;
    [SerializeField] private GameObject panelGameHUD;
    [SerializeField] private GameObject panelWin;
    [SerializeField] private GameObject panelLose;

    [Header("UI Texts")]
    [SerializeField] private TMP_Text recipeText;         // Panel_Recipe/RecipeText
    [SerializeField] private TMP_Text loseReasonText;     // Panel_Lose/LoseReasonText
    [SerializeField] private TMP_Text hudCountsText;      // Panel_GameHUD/HUD_CountsText
    [SerializeField] private TMP_Text hudSequenceText;    // Panel_GameHUD/HUD_SequenceText
    [SerializeField] private TMP_Text winButtonLabel;     // Panel_Win/Btn_Replay/Replay (optional but recommended)

    // Data used in both levels
    private readonly Dictionary<string, int> recipe = new Dictionary<string, int>();
    private readonly Dictionary<string, int> sliced = new Dictionary<string, int>();
    private readonly List<string> expectedSequence = new List<string>();
    private readonly List<string> displayOrder = new List<string> { "Apple", "Banana", "Strawberry" };

    private int currentIndex = 0;
    private bool gameActive = false;
    private bool lastWin = false;

    private const float RECIPE_SHOW_L0 = 3f; // Level 0
    private const float RECIPE_SHOW_L1 = 5f; // Level 1

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

    // ---------- UI States ----------
    public void ShowMenu()
    {
        panelMenu.SetActive(true);
        panelRecipe.SetActive(false);
        panelGameHUD.SetActive(false);
        panelWin.SetActive(false);
        panelLose.SetActive(false);

        gameActive = false;
        currentIndex = 0;
        lastWin = false;

        // Optional: reset button label in case it was changed before
        if (winButtonLabel != null) winButtonLabel.text = "Replay";
    }

    // Called by Menu Play button
    public void StartGame()
    {
        // Choose mode based on level
        if (CurrentLevel == 0)
        {
            mode = LevelMode.TutorialCountAndOrder;   // Level 0
            BuildLevel0(); // Apple×1, Banana×2, Strawberry×1 (order enforced + HUD)
            ShowRecipePanel(RECIPE_SHOW_L0, showCountsAndOrder: true);
        }
        else if (CurrentLevel == 1)
        {
            mode = LevelMode.MemoryOrderOnly;         // Level 1
            BuildLevel1(); // Apple, Apple, Banana, Banana (order enforced, no HUD)
            ShowRecipePanel(RECIPE_SHOW_L1, showCountsAndOrder: false);
        }
        else
        {
            // For now, if someone hits Play beyond level 1, just loop back to L1
            mode = LevelMode.MemoryOrderOnly;
            BuildLevel1();
            ShowRecipePanel(RECIPE_SHOW_L1, showCountsAndOrder: false);
        }
    }

    private void ShowRecipePanel(float seconds, bool showCountsAndOrder)
    {
        panelMenu.SetActive(false);
        panelRecipe.SetActive(true);
        panelGameHUD.SetActive(false);
        panelWin.SetActive(false);
        panelLose.SetActive(false);

        if (showCountsAndOrder)
            recipeText.text = BuildRecipeDisplayTextCountsAndOrder();
        else
            recipeText.text = BuildRecipeDisplayTextOrderOnly();

        Invoke(nameof(BeginPlay), seconds);
    }

    private void BeginPlay()
    {
        panelRecipe.SetActive(false);

        if (mode == LevelMode.TutorialCountAndOrder)
        {
            // HUD visible and updating live
            panelGameHUD.SetActive(true);
            UpdateHUD();
        }
        else
        {
            // Memory mode: no HUD
            panelGameHUD.SetActive(false);
        }

        currentIndex = 0;
        gameActive = true;
        if (spawner != null) spawner.StartSpawning();
    }

    // ---------- Build Levels ----------

    // Level 0: Counts + Sequence + Live HUD + order enforced
    private void BuildLevel0()
    {
        recipe.Clear(); sliced.Clear(); expectedSequence.Clear();

        recipe["Apple"] = 1;
        recipe["Banana"] = 2;
        recipe["Strawberry"] = 1;

        foreach (var kv in recipe) sliced[kv.Key] = 0;

        expectedSequence.Add("Apple");
        expectedSequence.Add("Banana");
        expectedSequence.Add("Banana");
        expectedSequence.Add("Strawberry");
    }

    // Level 1: Memory (order only, no HUD). Show order for 5s, then hide.
    // Sequence: Apple, Apple, Banana, Banana
    private void BuildLevel1()
    {
        recipe.Clear(); sliced.Clear(); expectedSequence.Clear();

        // We don't need counts here, but keep sliced dictionary safe/empty
        expectedSequence.Add("Apple");
        expectedSequence.Add("Apple");
        expectedSequence.Add("Banana");
        expectedSequence.Add("Banana");
    }

    // ---------- UI Helpers ----------
    private string BuildRecipeDisplayTextCountsAndOrder()
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

    private string BuildRecipeDisplayTextOrderOnly()
    {
        // e.g., "Order: Apple → Apple → Banana → Banana"
        return "Order: " + string.Join(" → ", expectedSequence);
    }

    private void UpdateHUD()
    {
        if (mode != LevelMode.TutorialCountAndOrder) return;

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

    // ---------- Gameplay Events ----------
    public void OnFruitSliced(Fruit fruit)
    {
        if (!gameActive || fruit == null) return;
        string name = fruit.fruitName;

        // Enforce: Only fruits that appear in the sequence are allowed
        bool nameInSequence = expectedSequence.Contains(name);
        if (!nameInSequence)
        {
            EndGame(false, $"Wrong fruit: {name}");
            return;
        }

        // Already completed but extra slice happened
        if (currentIndex >= expectedSequence.Count)
        {
            EndGame(false, $"Extra slice: {name}");
            return;
        }

        // Must match expected order
        string expected = expectedSequence[currentIndex];
        if (name != expected)
        {
            EndGame(false, $"Wrong order! Expected {expected}, got {name}");
            return;
        }

        // Advance
        currentIndex++;

        if (mode == LevelMode.TutorialCountAndOrder)
        {
            // Update counts and no extras beyond required
            if (!sliced.ContainsKey(name)) sliced[name] = 0;
            sliced[name]++;

            if (recipe.ContainsKey(name) && sliced[name] > recipe[name])
            {
                EndGame(false, $"Too many {name}s!");
                return;
            }

            UpdateHUD();
        }

        // Completed sequence exactly
        if (currentIndex >= expectedSequence.Count)
        {
            EndGame(true, null);
        }
    }

    private void EndGame(bool win, string reason)
    {
        lastWin = win;
        gameActive = false;
        if (spawner != null) spawner.StopSpawning();

        panelGameHUD.SetActive(false);

        if (win)
        {
            // If we're finishing a level, show Win and set button to "Next"
            panelWin.SetActive(true);
            if (winButtonLabel != null) winButtonLabel.text = "Next";
        }
        else
        {
            panelLose.SetActive(true);
            if (loseReasonText != null) loseReasonText.text = reason;
            // On lose, keep button label as "Retry/Replay"
            if (winButtonLabel != null) winButtonLabel.text = "Replay";
        }
    }

    // Called by Win "Next" and Lose "Retry" (both wired to this)
    public void Replay()
    {
        // If we just won, advance level; if we lost, keep same level
        if (lastWin) CurrentLevel++;

        UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
    }
}
