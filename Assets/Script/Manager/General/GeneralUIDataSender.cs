using UnityEngine;
using UnityEngine.SceneManagement;

public class GeneralUIDataSender : MonoBehaviour
{
    [SerializeField]
    GeneralUIData uIData;

    public static GeneralUIDataSender instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }
    public GeneralUIData GetData() => uIData;
    public ObjectToggleButton GetToggleBtn() => uIData.toggleButton;
    public string GetSenderSceneName() => SceneManager.GetActiveScene().name;
}
