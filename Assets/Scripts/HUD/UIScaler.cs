using UnityEngine;
using UnityEngine.UIElements;

public class UIScaler : MonoBehaviour
{
    [SerializeField] private PanelSettings _panelSettings;
    private const float ReferenceHeight = 1688f;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        float scale = Screen.height / ReferenceHeight;
        _panelSettings.scale = scale;
        Debug.Log($"[UIScaler] Screen: {Screen.width}x{Screen.height}, scale: {scale}");
    }
}