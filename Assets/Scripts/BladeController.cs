using UnityEngine;

public class BladeController : MonoBehaviour
{
    private Camera cam;
    private TrailRenderer trail;
    private Rigidbody2D rb;
    private CircleCollider2D col;   // <- add collider reference

    void Awake()
    {
        cam = Camera.main;
        trail = GetComponent<TrailRenderer>();
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CircleCollider2D>();   // <- get collider

        trail.emitting = false; 
        col.enabled = false;   // <- collider off at start
    }

    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            Vector3 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
            rb.position = new Vector2(mousePos.x, mousePos.y);
            Debug.Log("Blade moving to " + rb.position);

            trail.emitting = true;
            col.enabled = true;   // <- collider ON when cutting
        }
        else
        {
            trail.emitting = false;
            col.enabled = false;  // <- collider OFF when not cutting
        }
    }
}