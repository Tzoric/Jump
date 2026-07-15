using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public sealed class LevelExitDoor : MonoBehaviour
{
    [SerializeField] private string destinationScene = "DungeonOverview";
    [SerializeField, Min(0.2f)] private float entranceSeconds = 0.9f;
    [SerializeField, Min(0)] private int levelNumber;

    public string DestinationScene => destinationScene;
    public bool IsUsed { get; private set; }
    public int LevelNumber => levelNumber;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    public void Configure(string sceneName)
    {
        destinationScene = sceneName;
    }

    public void Configure(string sceneName, int completedLevelNumber)
    {
        destinationScene = sceneName;
        levelNumber = Mathf.Max(0, completedLevelNumber);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryUse(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryUse(other);
    }

    private void TryUse(Collider2D other)
    {
        HeroMovement hero = other.GetComponentInParent<HeroMovement>();
        if (IsUsed || hero == null || !hero.IsGrounded)
        {
            return;
        }

        IsUsed = true;
        StartCoroutine(EnterDoorRoutine(hero));
    }

    private IEnumerator EnterDoorRoutine(HeroMovement hero)
    {
        hero.SetInputLocked(true);
        Rigidbody2D body = hero.GetComponent<Rigidbody2D>();
        Collider2D bodyCollider = hero.GetComponent<Collider2D>();
        Animator animator = hero.GetComponent<Animator>();
        MinerOutfitVisual outfit = hero.GetComponent<MinerOutfitVisual>();
        if (bodyCollider != null) bodyCollider.enabled = false;
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.bodyType = RigidbodyType2D.Kinematic;
        }
        if (animator != null) animator.SetFloat("Speed", 1f);
        if (outfit != null) outfit.PlayWalkAway();

        Vector3 start = hero.transform.position;
        Vector3 destination = new(transform.position.x, transform.position.y - 0.65f, start.z);
        SpriteRenderer renderer = outfit != null && outfit.VisualRenderer != null
            ? outfit.VisualRenderer
            : hero.GetComponent<SpriteRenderer>();
        Color original = renderer == null ? Color.white : renderer.color;
        float elapsed = 0f;
        while (elapsed < entranceSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / entranceSeconds);
            hero.transform.position = Vector3.Lerp(start, destination, t);
            if (renderer != null) renderer.color = new Color(original.r, original.g, original.b, 1f - t);
            yield return null;
        }

        GameProgress.CompleteLevel(levelNumber);
        SceneManager.LoadScene(destinationScene);
    }
}
