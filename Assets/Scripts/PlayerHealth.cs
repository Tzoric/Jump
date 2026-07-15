using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField, Min(1)] private int maxHealth = 5;
    [SerializeField, Min(0f)] private float invulnerabilitySeconds = 1f;

    [Header("Respawn")]
    [SerializeField, Min(0f)] private float respawnDelay = 0.6f;
    [SerializeField] private Vector2 damageKnockback = new(7f, 7f);

    [Header("Presentation")]
    [SerializeField] private TextMeshProUGUI healthDisplay;
    [SerializeField] private UnityEvent<int, int> healthChanged;

    private Rigidbody2D body;
    private HeroMovement movement;
    private SpriteRenderer spriteRenderer;
    private Vector3 respawnPosition;
    private int currentHealth;
    private float invulnerableUntil;
    private bool respawning;
    private TextMeshProUGUI livesDisplay;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public int RespawnCount { get; private set; }
    public bool IsInvulnerable => Time.time < invulnerableUntil;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        movement = GetComponent<HeroMovement>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        respawnPosition = transform.position;
        maxHealth = GameProgress.MaxHearts;
        currentHealth = maxHealth;
        RefreshDisplay();
    }

    public bool TakeDamage(int amount, Vector2 sourcePosition)
    {
        if (amount <= 0 || respawning || IsInvulnerable)
        {
            return false;
        }

        currentHealth = Mathf.Max(0, currentHealth - amount);
        invulnerableUntil = Time.time + invulnerabilitySeconds;

        float direction = Mathf.Sign(transform.position.x - sourcePosition.x);
        if (Mathf.Approximately(direction, 0f))
        {
            direction = 1f;
        }

        body.linearVelocity = new Vector2(direction * damageKnockback.x, damageKnockback.y);
        RefreshDisplay();

        if (currentHealth == 0)
        {
            StartCoroutine(RespawnRoutine());
        }
        else
        {
            StartCoroutine(DamageFlashRoutine());
        }

        return true;
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || currentHealth == maxHealth)
        {
            return;
        }

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        RefreshDisplay();
    }

    public void SetCheckpoint(Vector3 position)
    {
        respawnPosition = position;
    }

    public void ConfigureDisplay(TextMeshProUGUI display)
    {
        healthDisplay = display;
        RefreshDisplay();
    }

    public void ConfigureDisplays(TextMeshProUGUI health, TextMeshProUGUI lives)
    {
        healthDisplay = health;
        livesDisplay = lives;
        RefreshDisplay();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H) && currentHealth < maxHealth && GameProgress.ConsumePotion())
        {
            Heal(maxHealth);
        }
    }

    private IEnumerator DamageFlashRoutine()
    {
        if (spriteRenderer == null)
        {
            yield break;
        }

        Color original = spriteRenderer.color;
        while (IsInvulnerable && !respawning)
        {
            spriteRenderer.color = new Color(original.r, original.g, original.b, 0.35f);
            yield return new WaitForSeconds(0.08f);
            spriteRenderer.color = original;
            yield return new WaitForSeconds(0.08f);
        }

        spriteRenderer.color = original;
    }

    private IEnumerator RespawnRoutine()
    {
        respawning = true;
        RespawnCount++;
        bool hasAnotherLife = GameProgress.ConsumeLife();
        if (movement != null)
        {
            movement.enabled = false;
        }

        body.linearVelocity = Vector2.zero;
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        yield return new WaitForSeconds(respawnDelay);

        if (!hasAnotherLife)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("DungeonOverview");
            yield break;
        }

        transform.position = respawnPosition;
        body.linearVelocity = Vector2.zero;
        currentHealth = maxHealth;
        invulnerableUntil = Time.time + invulnerabilitySeconds;

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = Color.white;
        }

        if (movement != null)
        {
            movement.enabled = true;
        }

        respawning = false;
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        if (healthDisplay != null)
        {
            healthDisplay.text = $"HEARTS  {new string('♥', currentHealth)}{new string('♡', maxHealth - currentHealth)}";
        }

        if (livesDisplay != null) livesDisplay.text = $"LIVES  {GameProgress.Lives}";

        healthChanged?.Invoke(currentHealth, maxHealth);
    }
}
