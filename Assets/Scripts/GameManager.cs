using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static int CurrentLevel = 0;

    private enum LevelMode { Tutorial, Memory }
    private static LevelMode? retryMode = null;
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
private void RebuildRetryLevel()
{
    recipe.Clear(); sliced.Clear(); expectedSequence.Clear();

    foreach (var kv in retryRecipe)
    {
        recipe[kv.Key] = kv.Value;
        sliced[kv.Key] = 0;
        for (int i = 0; i < kv.Value; i++)
            expectedSequence.Add(kv.Key);
    }
    BoostRecipeFruitWeights();
    // spawner.SetAllowedFruits(new List<string>(recipe.Keys));
}
    public void StartGame()
{
    if (retryRecipe != null && retryMode != null)
    {
        mode = retryMode.Value; // restore mode
        RebuildRetryLevel();
        ShowRecipePanel(mode == LevelMode.Tutorial ? RECIPE_SHOW_TUTORIAL : RECIPE_SHOW);
        return;
    }


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
    private void BoostRecipeFruitWeights()
    {
        spawner.ResetWeights(); // reset to default (we’ll add this in FruitSpawner)
        foreach (var fruit in recipe.Keys)
        {
            spawner.SetFruitWeight(fruit, 6); // boost recipe fruits
        }
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
        BoostRecipeFruitWeights();
        // spawner.SetAllowedFruits(chosen);
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
        BoostRecipeFruitWeights();
        // spawner.SetAllowedFruits(new List<string>(recipe.Keys));
    }

    private void SaveRecipe() => lastRecipe = new Dictionary<string, int>(recipe);

    // ---------- UI Helpers ----------
    private string BuildRecipeDisplayText()
{
    List<string> counts = new List<string>();
    foreach (var kv in recipe)
        counts.Add($"{kv.Key} × {kv.Value}");

    string baseLine = "Recipe: " + string.Join("  ", counts);

    // Show order ONLY in tutorial (Level 0)
    if (mode == LevelMode.Tutorial)
    {
        string seqLine = "Order: " + string.Join(" → ", expectedSequence);
        return baseLine + "\n" + seqLine;
    }

    return baseLine;
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
    private static Dictionary<string, int> retryRecipe = null; // store recipe on lose
    private void EndGame(bool win, string reason)
{
    lastWin = win;
    gameActive = false;
    spawner.StopSpawning();
    panelGameHUD.SetActive(false);

    if (win)
    {
        panelWin.SetActive(true);
        retryRecipe = null; // clear retry recipe
        retryMode = null;
        if (winButtonLabel != null) winButtonLabel.text = "Next Level";
    }
    else
    {
        panelLose.SetActive(true);
        loseReasonText.text = reason;
        retryRecipe = new Dictionary<string, int>(recipe); // save current recipe
        retryMode = mode; // save the mode too
        if (winButtonLabel != null) winButtonLabel.text = "Retry";
    }
}

    public void Replay()
{
        if (lastWin)
        {
            CurrentLevel++;
            retryRecipe = null; // don’t carry over losing recipe
            retryMode = null;
    }

    UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
}
}
