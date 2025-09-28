using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static int CurrentLevel = 0;

    private enum LevelMode { TutorialWithOrder, MemoryCountsOnly }
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
    [SerializeField] private TMP_Text recipeText;         
    [SerializeField] private TMP_Text loseReasonText;     
    [SerializeField] private TMP_Text hudCountsText;      
    [SerializeField] private TMP_Text hudSequenceText;    
    [SerializeField] private TMP_Text winButtonLabel;     

    private readonly Dictionary<string, int> recipe = new Dictionary<string, int>();
    private readonly Dictionary<string, int> sliced = new Dictionary<string, int>();
    private readonly List<string> expectedSequence = new List<string>();

    private int currentIndex = 0;
    private bool gameActive = false;
    private bool lastWin = false;

    private const float RECIPE_SHOW_L0 = 3f;
    private const float RECIPE_SHOW_L1 = 5f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start() => ShowMenu();

    // ---------- UI ----------
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

        if (winButtonLabel != null) winButtonLabel.text = "Replay";
    }

    public void StartGame()
    {
        if (CurrentLevel == 0)
        {
            mode = LevelMode.TutorialWithOrder;
            BuildLevel0();
            ShowRecipePanel(RECIPE_SHOW_L0);
        }
        else
        {
            mode = LevelMode.MemoryCountsOnly;
            int numFruits = Mathf.Min(2 + CurrentLevel, spawner.GetAllFruitNames().Count);
            BuildRandomCountsLevel(numFruits, minCount: 1, maxCount: 3);
            ShowRecipePanel(RECIPE_SHOW_L1);
        }
    }

    private void ShowRecipePanel(float seconds)
    {
        panelMenu.SetActive(false);
        panelRecipe.SetActive(true);
        panelGameHUD.SetActive(false);
        panelWin.SetActive(false);
        panelLose.SetActive(false);

        if (mode == LevelMode.TutorialWithOrder)
            recipeText.text = BuildRecipeDisplayTextCountsAndOrder();
        else
            recipeText.text = BuildRecipeDisplayTextCountsOnly();

        Invoke(nameof(BeginPlay), seconds);
    }

    private void BeginPlay()
    {
        panelRecipe.SetActive(false);

        if (mode == LevelMode.TutorialWithOrder)
        {
            panelGameHUD.SetActive(true);
            UpdateHUD();
        }
        else
        {
            panelGameHUD.SetActive(false);
        }

        currentIndex = 0;
        gameActive = true;
        spawner.StartSpawning();
    }

    // ---------- Build Levels ----------
    private void BuildLevel0()
    {
        recipe.Clear(); sliced.Clear(); expectedSequence.Clear();

        // Level 0 always uses Apple + Banana + Strawberry
        List<string> chosen = new List<string> { "Apple", "Banana", "Strawberry" };

        recipe["Apple"] = 1;
        recipe["Banana"] = 2;
        recipe["Strawberry"] = 1;

        foreach (var kv in recipe) sliced[kv.Key] = 0;

        expectedSequence.Add("Apple");
        expectedSequence.Add("Banana");
        expectedSequence.Add("Banana");
        expectedSequence.Add("Strawberry");

        spawner.SetAllowedFruits(chosen);
    }

    private void BuildRandomCountsLevel(int numFruits, int minCount, int maxCount)
    {
        recipe.Clear(); sliced.Clear(); expectedSequence.Clear();

        List<string> allNames = spawner.GetAllFruitNames();
        List<string> chosen = new List<string>();

        numFruits = Mathf.Min(numFruits, allNames.Count);
        for (int i = 0; i < numFruits; i++)
        {
            int idx = Random.Range(0, allNames.Count);
            string pick = allNames[idx];
            chosen.Add(pick);
            allNames.RemoveAt(idx);
        }

        foreach (string fruit in chosen)
        {
            int count = Random.Range(minCount, maxCount + 1);
            recipe[fruit] = count;
            sliced[fruit] = 0;

            // Sequence is not enforced (any order allowed)
            for (int i = 0; i < count; i++)
                expectedSequence.Add(fruit);
        }

        spawner.SetAllowedFruits(chosen);
    }

    // ---------- UI Helpers ----------
    private string BuildRecipeDisplayTextCountsAndOrder()
    {
        List<string> counts = new List<string>();
        foreach (var kv in recipe) counts.Add($"{kv.Key}×{kv.Value}");
        string countsLine = "Recipe: " + string.Join("  ", counts);
        string seqLine = "Order: " + string.Join(" → ", expectedSequence);
        return countsLine + "\n" + seqLine;
    }

    private string BuildRecipeDisplayTextCountsOnly()
    {
        List<string> counts = new List<string>();
        foreach (var kv in recipe) counts.Add($"{kv.Key}×{kv.Value}");
        return "Recipe: " + string.Join("  ", counts);
    }

    private void UpdateHUD()
    {
        List<string> lines = new List<string>();
        foreach (var kv in recipe)
            lines.Add($"{kv.Key}: {sliced[kv.Key]}/{kv.Value}");
        hudCountsText.text = string.Join("\n", lines);

        string seq = string.Join(" → ", expectedSequence);
        string next = (currentIndex < expectedSequence.Count) ? expectedSequence[currentIndex] : "—";
        hudSequenceText.text = $"Sequence: {seq}\nNext: {next}";
    }

    // ---------- Gameplay ----------
    public void OnFruitSliced(Fruit fruit)
    {
        if (!gameActive || fruit == null) return;
        string name = fruit.fruitName;

        if (mode == LevelMode.TutorialWithOrder)
        {
            // strict order check
            if (currentIndex >= expectedSequence.Count)
            {
                EndGame(false, $"Extra slice: {name}");
                return;
            }

            string expected = expectedSequence[currentIndex];
            if (name != expected)
            {
                EndGame(false, $"Wrong! Expected {expected}, got {name}");
                return;
            }

            currentIndex++;
            sliced[name]++;
            if (sliced[name] > recipe[name])
            {
                EndGame(false, $"Too many {name}s!");
                return;
            }
            UpdateHUD();

            if (currentIndex >= expectedSequence.Count)
                EndGame(true, null);
        }
        else
        {
            // counts-only mode (order doesn’t matter)
            if (!recipe.ContainsKey(name))
            {
                EndGame(false, $"Wrong fruit: {name}");
                return;
            }

            sliced[name]++;
            if (sliced[name] > recipe[name])
            {
                EndGame(false, $"Too many {name}s!");
                return;
            }

            // check if all counts complete
            foreach (var kv in recipe)
                if (sliced[kv.Key] < kv.Value) return;

            EndGame(true, null);
        }
    }

    private void EndGame(bool win, string reason)
    {
        lastWin = win;
        gameActive = false;
        spawner.StopSpawning();
        panelGameHUD.SetActive(false);

        if (win)
        {
            panelWin.SetActive(true);
            if (winButtonLabel != null) winButtonLabel.text = "Next";
        }
        else
        {
            panelLose.SetActive(true);
            loseReasonText.text = reason;
            if (winButtonLabel != null) winButtonLabel.text = "Replay";
        }
    }

    public void Replay()
    {
        if (lastWin) CurrentLevel++;
        UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
    }
}
