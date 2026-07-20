using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class SceneLoadButton : MonoBehaviour
{
    [SerializeField] private string targetScene;
    [SerializeField] private bool beginPlaytestRun;

    private Button button;

    public string TargetScene => targetScene;
    public bool BeginsPlaytestRun => beginPlaytestRun;

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

    public void Configure(string sceneName, bool startPlaytestRun = false)
    {
        targetScene = sceneName;
        beginPlaytestRun = startPlaytestRun;
    }

    public void LoadTargetScene()
    {
        if (string.IsNullOrWhiteSpace(targetScene))
        {
            Debug.LogError("SceneLoadButton has no target scene.");
            return;
        }

        if (beginPlaytestRun && GameProgress.PlaytestAccessEnabled)
        {
            GameProgress.BeginPlaytestRun();
        }

        SceneManager.LoadScene(targetScene);
    }
}
