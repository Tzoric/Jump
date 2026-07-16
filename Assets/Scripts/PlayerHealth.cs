using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody2D))]
public sealed class PlayerHealth : MonoBehaviour
{
    private const char HeartGlyph = '\u2665';

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
    public bool IsRespawning => respawning;
    public bool CanAct => currentHealth > 0 && !respawning;
    public string HealthDisplayText => healthDisplay == null ? string.Empty : healthDisplay.text;
    public bool HealthDisplaySupportsHeartGlyph => healthDisplay != null && healthDisplay.font != null &&
        healthDisplay.font.HasCharacter(HeartGlyph, true, true);

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        movement = GetComponent<HeroMovement>();
        MinerOutfitVisual outfit = GetComponent<MinerOutfitVisual>();
        spriteRenderer = outfit != null && outfit.VisualRenderer != null
            ? outfit.VisualRenderer
            : GetComponent<SpriteRenderer>();
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

    public void ConfigureBaseHealth(int hearts)
    {
        maxHealth = Mathf.Max(1, hearts);
        if (!Application.isPlaying) currentHealth = maxHealth;
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
        if (!MineLevelMenuController.IsPaused && MineInput.PotionPressed) TryUsePotion();
    }

    public bool TryUsePotion()
    {
        if (!CanAct || currentHealth >= maxHealth || !GameProgress.ConsumePotion()) return false;
        Heal(1);
        return true;
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
        GetComponent<ParachuteDescentController>()?.ResetDescentState();
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
            UnityEngine.SceneManagement.SceneManager.LoadScene("GameOver");
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
            string filled = new(HeartGlyph, currentHealth);
            string empty = new(HeartGlyph, maxHealth - currentHealth);
            healthDisplay.text = $"HEARTS  <color=#FF6767>{filled}</color><color=#493B45>{empty}</color>";
        }

        if (livesDisplay != null) livesDisplay.text = $"LIVES  {GameProgress.Lives}";

        healthChanged?.Invoke(currentHealth, maxHealth);
    }
}
