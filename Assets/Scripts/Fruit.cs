using UnityEngine;

public class Fruit : MonoBehaviour
{
    private SpriteRenderer sr;
    private AudioSource audioSource;


    [Header("Fruit Info")]
    public string fruitName = "Apple";

    [Header("Slice Effect")]
    [SerializeField] private float fadeDuration = 0.3f;

    private bool isSliced = false;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isSliced) return;
        if (!other.CompareTag("Blade")) return;

        Slice();
    }

    private void Slice()
    {
        isSliced = true;

        var ps = GetComponentInChildren<ParticleSystem>();
        if (ps != null)
        {
            ps.transform.parent = null;
            ps.Play();
            Destroy(ps.gameObject, 1f);
        }

        if (AudioManager.Instance != null && audioSource != null)
        {
            AudioManager.Instance.PlaySliceSound(audioSource);
        }
        else
        {
            Debug.LogWarning("Slice sound not played: missing AudioManager or AudioSource.", this);
        }

        GameManager.Instance.OnFruitSliced(this);

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = false;

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        StartCoroutine(FadeAndDestroy());
    }

    private System.Collections.IEnumerator FadeAndDestroy()
    {
        float elapsed = 0f;
        Color original = sr.color;

        while (elapsed < fadeDuration)
        {
            float t = elapsed / fadeDuration;
            sr.color = new Color(original.r, original.g, original.b, 1f - t); 
            elapsed += Time.deltaTime;
            yield return null;
        }

        sr.color = new Color(original.r, original.g, original.b, 0f); 
        Destroy(gameObject);
    }
}