using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public sealed class LevelExitDoor : MonoBehaviour
{
    [SerializeField] private string destinationScene = "DungeonOverview";

    public string DestinationScene => destinationScene;
    public bool IsUsed { get; private set; }

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    public void Configure(string sceneName)
    {
        destinationScene = sceneName;
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
        SceneManager.LoadScene(destinationScene);
    }
}
