using UnityEngine;

public class Fruit : MonoBehaviour
{
    private SpriteRenderer sr;

    [Header("Slice Effect")]
    [SerializeField] private Color slicedColor = Color.red;
    [SerializeField] private float shrinkDuration = 0.2f;

    private bool isSliced = false;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only react if the thing colliding is the Blade
        if (isSliced) return;
        if (!other.CompareTag("Blade")) return;

        Slice();
    }

    private void Slice()
    {
        isSliced = true;

        // Change color to indicate slicing
        if (sr != null)
        {
            sr.color = slicedColor;
        }

        // Disable physics so fruit stops interacting
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.simulated = false;
        }

        // Optionally disable collider so Blade doesn’t trigger again
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

        // Start disappearing effect
        Destroy(gameObject, shrinkDuration);
    }
}