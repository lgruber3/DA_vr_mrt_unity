using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ErrorDialog : MonoBehaviour
{
    public static ErrorDialog Instance; 

    [SerializeField] private TextMeshProUGUI errorText;
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject panelRoot;

    private void Awake()
    {
        Instance = this;
        closeButton.onClick.AddListener(Hide);
        Hide(); 
    }

    public void Show(string message)
    {
        panelRoot.SetActive(true);
        errorText.text = message;
    }

    public void Hide()
    {
        panelRoot.SetActive(false);
    }
}