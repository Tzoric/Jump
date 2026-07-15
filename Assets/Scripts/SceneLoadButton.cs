using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class SceneLoadButton : MonoBehaviour
{
    [SerializeField] private string targetScene;

    private Button button;

    public string TargetScene => targetScene;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(LoadTargetScene);
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(LoadTargetScene);
        }
    }

    public void Configure(string sceneName)
    {
        targetScene = sceneName;
    }

    public void LoadTargetScene()
    {
        if (string.IsNullOrWhiteSpace(targetScene))
        {
            Debug.LogError("SceneLoadButton has no target scene.");
            return;
        }

        SceneManager.LoadScene(targetScene);
    }
}
