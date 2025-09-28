using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static int CurrentLevel = 0;

    private enum LevelMode { Tutorial, Memory }
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

    // Data
    private readonly Dictionary<string, int> recipe = new Dictionary<string, int>();
    private readonly Dictionary<string, int> sliced = new Dictionary<string, int>();
    private readonly List<string> expectedSequence = new List<string>();

    private static Dictionary<string, int> lastRecipe = null; // carry recipe forward

    private int currentIndex = 0;
    private bool gameActive = false;
    private bool lastWin = false;

    private const float RECIPE_SHOW_TUTORIAL = 3f;
    private const float RECIPE_SHOW = 5f;

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
            mode = LevelMode.Tutorial;
            BuildRandomLevel(numFruits: 3, minCount: 1, maxCount: 2);
            ShowRecipePanel(RECIPE_SHOW_TUTORIAL);
        }
        else if (CurrentLevel == 1)
        {
            mode = LevelMode.Memory;
            BuildRandomLevel(numFruits: 2, minCount: 2, maxCount: 3);
            SaveRecipe();
            ShowRecipePanel(RECIPE_SHOW);
        }
        else
        {
            mode = LevelMode.Memory;
            BuildExpandedLevel(extraFruits: 2, minCount: 2, maxCount: 3);
            SaveRecipe();
            ShowRecipePanel(RECIPE_SHOW);
        }
    }

    private void ShowRecipePanel(float seconds)
    {
        panelMenu.SetActive(false);
        panelRecipe.SetActive(true);
        panelGameHUD.SetActive(false);
        panelWin.SetActive(false);
        panelLose.SetActive(false);

        recipeText.text = BuildRecipeDisplayText();

        Invoke(nameof(BeginPlay), seconds);
    }

    private void BeginPlay()
    {
        panelRecipe.SetActive(false);

        if (mode == LevelMode.Tutorial)
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
    private void BuildRandomLevel(int numFruits, int minCount, int maxCount)
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
            allNames.RemoveAt(idx); // prevent infinite loop
        }

        foreach (string fruit in chosen)
        {
            int count = Random.Range(minCount, maxCount + 1);
            recipe[fruit] = count;
            sliced[fruit] = 0;

            for (int i = 0; i < count; i++)
                expectedSequence.Add(fruit);
        }

        spawner.SetAllowedFruits(chosen);
    }

    private void BuildExpandedLevel(int extraFruits, int minCount, int maxCount)
    {
        recipe.Clear(); sliced.Clear(); expectedSequence.Clear();

        // keep last recipe
        foreach (var kv in lastRecipe)
        {
            recipe[kv.Key] = kv.Value;
            sliced[kv.Key] = 0;
            for (int i = 0; i < kv.Value; i++)
                expectedSequence.Add(kv.Key);
        }

        // add new fruits
        List<string> allNames = spawner.GetAllFruitNames();
        foreach (string used in recipe.Keys)
            allNames.Remove(used);

        int toAdd = Mathf.Min(extraFruits, allNames.Count);
        for (int i = 0; i < toAdd; i++)
        {
            int idx = Random.Range(0, allNames.Count);
            string pick = allNames[idx];
            allNames.RemoveAt(idx);

            int count = Random.Range(minCount, maxCount + 1);
            recipe[pick] = count;
            sliced[pick] = 0;
            for (int j = 0; j < count; j++)
                expectedSequence.Add(pick);
        }

        spawner.SetAllowedFruits(new List<string>(recipe.Keys));
    }

    private void SaveRecipe() => lastRecipe = new Dictionary<string, int>(recipe);

    // ---------- UI Helpers ----------
    private string BuildRecipeDisplayText()
    {
        List<string> counts = new List<string>();
        foreach (var kv in recipe)
            counts.Add($"{kv.Key} × {kv.Value}");
        string seqLine = "Order: " + string.Join(" → ", expectedSequence);
        return "Recipe: " + string.Join("  ", counts) + "\n" + seqLine;
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

        if (mode == LevelMode.Tutorial) UpdateHUD();

        if (currentIndex >= expectedSequence.Count)
            EndGame(true, null);
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
            if (winButtonLabel != null) winButtonLabel.text = "Next Level";
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
