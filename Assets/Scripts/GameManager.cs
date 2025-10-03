using UnityEngine;
using TMPro;
using System.Collections;   
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
    [SerializeField] private GameObject panelTutorialEnd;

    [SerializeField] private GameObject panelGameCleared; 
    [SerializeField] private GameObject panelFruitGuide;   

    [Header("UI Buttons")]
    [SerializeField] private UnityEngine.UI.Button proceedButton; 


    [Header("UI Texts")]
    [SerializeField] private TMP_Text recipeText;
    [SerializeField] private TMP_Text loseReasonText;
    [SerializeField] private TMP_Text hudCountsText;
    [SerializeField] private TMP_Text hudSequenceText;
    [SerializeField] private TMP_Text winButtonLabel;

    
    [SerializeField] private TMP_Text levelText;            
    [SerializeField] private TMP_Text tutorialEndTitle;   



    
    private readonly Dictionary<string, int> recipe = new Dictionary<string, int>();
    private readonly Dictionary<string, int> sliced = new Dictionary<string, int>();
    private readonly List<string> expectedSequence = new List<string>();

    private static Dictionary<string, int> lastRecipe = null; 

    private int currentIndex = 0;
    private bool gameActive = false;
    private bool lastWin = false;

    private const float RECIPE_SHOW_TUTORIAL = 3f;
    private const float RECIPE_SHOW = 5f;
    private const int FINAL_LEVEL = 4;

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

    
    public void ShowMenu()
    {
        panelMenu.SetActive(true);
        panelRecipe.SetActive(false);
        panelGameHUD.SetActive(false);
        panelWin.SetActive(false);
        panelLose.SetActive(false);
        if (panelTutorialEnd != null) panelTutorialEnd.SetActive(false);
        if (panelGameCleared != null) panelGameCleared.SetActive(false);

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
        
    }
    public void StartGame()
    {
        if (retryRecipe != null && retryMode != null)
        {
            mode = retryMode.Value; 
            RebuildRetryLevel();
            ShowRecipePanel(mode == LevelMode.Tutorial ? RECIPE_SHOW_TUTORIAL : RECIPE_SHOW);
            return;
        }
        
        if (CurrentLevel > FINAL_LEVEL)
        {
            ShowGameCleared();
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
        if (panelTutorialEnd != null) panelTutorialEnd.SetActive(false); 

        recipeText.text = BuildRecipeDisplayText();

        if (levelText != null)
            levelText.text = $"Level {CurrentLevel}"; 

        Invoke(nameof(BeginPlay), seconds);
    }

    private void BoostNextFruit()
    {
        spawner.ResetWeights(1f); 

        if (currentIndex < expectedSequence.Count)
        {
            string nextFruit = expectedSequence[currentIndex];
            spawner.SetFruitWeight(nextFruit, 8f); 
        }
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
        BoostNextFruit(); 
    }
    private void BoostRecipeFruitWeights()
    {
        spawner.ResetWeights(); 
        foreach (var fruit in recipe.Keys)
        {
            spawner.SetFruitWeight(fruit, 6); 
        }
    }

    
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
            allNames.RemoveAt(idx); 
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
    }

    private void BuildExpandedLevel(int extraFruits, int minCount, int maxCount)
    {
        recipe.Clear(); sliced.Clear(); expectedSequence.Clear();

        foreach (var kv in lastRecipe)
        {
            recipe[kv.Key] = kv.Value;
            sliced[kv.Key] = 0;
            for (int i = 0; i < kv.Value; i++)
                expectedSequence.Add(kv.Key);
        }

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
    }

    private void SaveRecipe() => lastRecipe = new Dictionary<string, int>(recipe);

    
    private string BuildRecipeDisplayText()
    {
        List<string> counts = new List<string>();
        foreach (var kv in recipe)
            counts.Add($"{kv.Key} × {kv.Value}");

        string baseLine = "Smoothie Recipe: " + string.Join("  ", counts);

        
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
        {
            EndGame(true, null);
            return;
        }

        
        BoostNextFruit();
    }

    private static Dictionary<string, int> retryRecipe = null; 
    private void EndGame(bool win, string reason)
    {
        lastWin = win;
        gameActive = false;
        spawner.StopSpawning();
        panelGameHUD.SetActive(false);

        if (mode == LevelMode.Tutorial)
        {
            panelTutorialEnd.SetActive(true);

            if (tutorialEndTitle != null)
                tutorialEndTitle.text = win ? "Great Job!" : "Try Again!";

            return;
        }

        if (win)
        {
            panelWin.SetActive(true);
            retryRecipe = null;
            retryMode = null;
            if (winButtonLabel != null) winButtonLabel.text = "Next Level";

            
            if (CurrentLevel >= FINAL_LEVEL)
            {
                ShowGameCleared();
                return;
            }
            
            string[] winPhrases = new string[]
            {
                "W Level!\nBlend it into a smoothie win!",
                "You Cooked That!\nNow sip your victory smoothie!",
                "Too EZ!\nSmoothie skills unlocked!",
                "Certified Slicer!\nSmoothie master in the making!",
                "That's a Dub!\nTime to pour a victory smoothie!",
                "Big Slice Energy!\nSmooth moves for a smooth smoothie!"
            };

            
            string randomPhrase = winPhrases[Random.Range(0, winPhrases.Length)];

            
            TMP_Text winText = panelWin.GetComponentInChildren<TMP_Text>();
            if (winText != null) winText.text = randomPhrase;
        }
        else
        {
            panelLose.SetActive(true);
            loseReasonText.text = reason;
            retryRecipe = new Dictionary<string, int>(recipe);
            retryMode = mode;
            if (winButtonLabel != null) winButtonLabel.text = "Retry";
        }
    }



    public void Replay()
    {
        if (lastWin)
        {
            CurrentLevel++;
            retryRecipe = null; 
            retryMode = null;
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
    }
   

    public void OnClick_Tutorial()
    {
        CurrentLevel = 0;
        retryRecipe = null; retryMode = null;
        ShowFruitGuide();
    }

    public void OnClick_Play()
    {
        
        if (retryRecipe != null && retryMode != null)
        {
            StartGame();
            return;
        }

        
        CurrentLevel = Mathf.Max(1, CurrentLevel);
        retryRecipe = null;
        retryMode = null;
        StartGame();
    }


    public void OnClick_NextLevel()
    {
        CurrentLevel++;
        retryRecipe = null; retryMode = null;
        UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
    }

    public void OnClick_RetryLevel()
    {
        
        if (retryRecipe != null && retryMode != null)
        {
            mode = retryMode.Value;
            panelLose.SetActive(false);
            panelWin.SetActive(false);

            RebuildRetryLevel();
            ShowRecipePanel(mode == LevelMode.Tutorial ? RECIPE_SHOW_TUTORIAL : RECIPE_SHOW);
            return;
        }

        
        UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
    }

    public void OnClick_StartGameFromTutorial()
    {
        CurrentLevel = 1;
        retryRecipe = null; retryMode = null;
        StartGame();
    }

    public void OnClick_MainMenu()
    {
        CurrentLevel = 0;
        retryRecipe = null; retryMode = null;
        ShowMenu();
    }

    public void NextLevel()
    {
        CurrentLevel++;
        
        if (CurrentLevel > FINAL_LEVEL)
        {
            ShowGameCleared();
            return;
        }
        retryRecipe = null;
        retryMode = null;

        
        StartGame();
    }

    public void OnClick_GameClearedToMenu()
    {
        CurrentLevel = 0;
        retryRecipe = null;
        retryMode = null;
        ShowMenu();
    }

    private void StartTutorialAfterGuide()
    {
        mode = LevelMode.Tutorial;   
        BuildRandomLevel(numFruits: 3, minCount: 1, maxCount: 2);
        ShowRecipePanel(RECIPE_SHOW_TUTORIAL);
    }

    public void ShowFruitGuide()
    {
        
        if (spawner != null) spawner.StopSpawning();
        CancelInvoke(nameof(BeginPlay));   
        gameActive = false;
        currentIndex = 0;

        panelMenu.SetActive(false);
        panelRecipe.SetActive(false);
        panelGameHUD.SetActive(false);
        panelWin.SetActive(false);
        panelLose.SetActive(false);
        panelTutorialEnd.SetActive(false);

        panelFruitGuide.SetActive(true);

        
        var anim = panelFruitGuide.GetComponent<FruitGuideAnimator>();
        if (anim != null) anim.ResetRows();

        
        proceedButton.onClick.RemoveAllListeners();
        proceedButton.onClick.AddListener(() =>
        {
            var a = panelFruitGuide.GetComponent<FruitGuideAnimator>();
            if (a != null)
            {
                StartCoroutine(PlayGuideExit(a));
            }
            else
            {
                panelFruitGuide.SetActive(false);
                StartTutorialAfterGuide();   
            }
        });
    }

    
    private IEnumerator PlayGuideExit(FruitGuideAnimator anim)
    {
        yield return StartCoroutine(anim.AnimateRows());

        panelFruitGuide.SetActive(false);
        StartTutorialAfterGuide();
    }

    private void ShowGameCleared()
    {
        panelMenu.SetActive(false);
        panelRecipe.SetActive(false);
        panelGameHUD.SetActive(false);
        panelWin.SetActive(false);
        panelLose.SetActive(false);
        panelTutorialEnd.SetActive(false);

        panelGameCleared.SetActive(true);
    }
}