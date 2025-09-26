using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FruitSpawner : MonoBehaviour
{
    [Header("Spawn Points")]
    [SerializeField] private bool autoFindSpawnPoints = true;
    [SerializeField] private Transform[] spawnPoints;

    [System.Serializable]
    public class FruitOption
    {
        public GameObject prefab;
        [Range(0f, 10f)] public float weight = 1f; // higher = more frequent
    }

    [Header("Fruit Prefabs (with weights)")]
    [SerializeField] private List<FruitOption> fruits = new List<FruitOption>();

    [Header("Spawning")]
    [Tooltip("Start spawning automatically on Play.")]
    [SerializeField] private bool autoStart = true;
    [Tooltip("Seconds between bursts.")]
    [SerializeField] private Vector2 intervalRange = new Vector2(0.6f, 1.4f);
    [Tooltip("How many fruits per burst.")]
    [SerializeField] private int minPerBurst = 1;
    [SerializeField] private int maxPerBurst = 3;
    [SerializeField] private bool randomizeSpawnPoint = true;

    [Header("Launch Forces (Impulse)")]
    [Tooltip("Horizontal impulse; negative = left, positive = right.")]
    [SerializeField] private Vector2 xForceRange = new Vector2(-3f, 3f);
    [Tooltip("Vertical impulse upward.")]
    [SerializeField] private Vector2 yForceRange = new Vector2(14f, 20f);
    [SerializeField] private Vector2 torqueRange = new Vector2(-180f, 180f);
    [Tooltip("Bias X so side spawn points throw toward screen center.")]
    [SerializeField] private bool biasXBySpawnPosition = true;

    [Header("Limits / Cleanup")]
    [SerializeField] private int maxActiveFruits = 25;
    [SerializeField] private float cleanupBelowY = -10f;
    [SerializeField] private float cleanupInterval = 1.0f;

    private readonly List<Rigidbody2D> active = new List<Rigidbody2D>();
    private Coroutine spawnLoopCo;
    private Coroutine cleanupCo;

    private void Reset()
    {
        TryAutoFindSpawnPoints();
    }

    private void Awake()
    {
        if (autoFindSpawnPoints) TryAutoFindSpawnPoints();
    }

    private void Start()
    {
        if (autoStart) StartSpawning();
    }

    public void StartSpawning()
    {
        if (spawnLoopCo == null) spawnLoopCo = StartCoroutine(SpawnLoop());
        if (cleanupCo == null) cleanupCo = StartCoroutine(CleanupLoop());
    }

    public void StopSpawning()
    {
        if (spawnLoopCo != null) { StopCoroutine(spawnLoopCo); spawnLoopCo = null; }
        if (cleanupCo != null) { StopCoroutine(cleanupCo); cleanupCo = null; }
    }

    private IEnumerator SpawnLoop()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[FruitSpawner] No spawn points assigned.");
            yield break;
        }

        while (true)
        {
            // Random delay between bursts
            float wait = Random.Range(intervalRange.x, intervalRange.y);
            yield return new WaitForSeconds(wait);

            // Respect max active limit
            PruneActiveList();
            if (active.Count >= maxActiveFruits) continue;

            int toSpawn = Random.Range(minPerBurst, maxPerBurst + 1);
            for (int i = 0; i < toSpawn; i++)
            {
                if (active.Count >= maxActiveFruits) break;
                SpawnOne();
            }
        }
    }

    private void SpawnOne()
    {
        Transform spawnT = randomizeSpawnPoint
            ? spawnPoints[Random.Range(0, spawnPoints.Length)]
            : spawnPoints[0];

        GameObject prefab = PickWeightedFruit();
        if (prefab == null)
        {
            Debug.LogWarning("[FruitSpawner] No fruit prefab configured.");
            return;
        }

        GameObject go = Instantiate(prefab, spawnT.position, Quaternion.identity, transform);
        go.name = $"{prefab.name}_Spawned";

        Rigidbody2D rb = go.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogWarning($"[FruitSpawner] Spawned fruit '{prefab.name}' has no Rigidbody2D.");
            return;
        }

        // Horizontal bias so fruits travel toward center
        float x = Random.Range(xForceRange.x, xForceRange.y);
        if (biasXBySpawnPosition)
        {
            float dir = spawnT.position.x >= 0f ? -1f : 1f; // right spawns go left, left spawns go right
            x = Mathf.Abs(x) * dir;
        }

        float y = Random.Range(yForceRange.x, yForceRange.y);
        rb.AddForce(new Vector2(x, y), ForceMode2D.Impulse);
        rb.AddTorque(Random.Range(torqueRange.x, torqueRange.y), ForceMode2D.Impulse);

        active.Add(rb);
    }

    private GameObject PickWeightedFruit()
    {
        if (fruits == null || fruits.Count == 0) return null;

        float total = 0f;
        for (int i = 0; i < fruits.Count; i++)
            total += Mathf.Max(0f, fruits[i].weight);

        if (total <= 0f)
            return fruits[Random.Range(0, fruits.Count)].prefab;

        float r = Random.value * total;
        float accum = 0f;
        for (int i = 0; i < fruits.Count; i++)
        {
            accum += Mathf.Max(0f, fruits[i].weight);
            if (r <= accum) return fruits[i].prefab;
        }
        return fruits[fruits.Count - 1].prefab;
    }

    private IEnumerator CleanupLoop()
    {
        var wait = new WaitForSeconds(cleanupInterval);
        while (true)
        {
            yield return wait;
            PruneActiveList();

            // Despawn anything that fell off-screen
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var rb = active[i];
                if (rb == null) { active.RemoveAt(i); continue; }
                if (rb.transform.position.y < cleanupBelowY)
                {
                    Destroy(rb.gameObject);
                    active.RemoveAt(i);
                }
            }
        }
    }

    private void PruneActiveList()
    {
        for (int i = active.Count - 1; i >= 0; i--)
            if (active[i] == null) active.RemoveAt(i);
    }

    private void TryAutoFindSpawnPoints()
    {
        var list = new List<Transform>();
        foreach (Transform child in transform)
            list.Add(child);
        spawnPoints = list.ToArray();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        if (spawnPoints != null)
        {
            foreach (var t in spawnPoints)
            {
                if (t == null) continue;
                Gizmos.DrawWireSphere(t.position, 0.2f);
                Gizmos.DrawLine(t.position, t.position + Vector3.up * 1.0f);
            }
        }
    }
#endif
}
