using UnityEngine;
using TMPro;

public class MetaKeyboardHandler : MonoBehaviour
{
    private TMP_InputField _inputField;
    private TouchScreenKeyboard _overlayKeyboard;

#if UNITY_EDITOR
    [Header("Editor Keyboard Mock")]
    [TextArea]
    public string editorInput;
#endif

    void Start()
    {
        _inputField = GetComponent<TMP_InputField>();
        TouchScreenKeyboard.hideInput = true;
        _inputField.onSelect.AddListener((string _) => OpenSystemKeyboard());
    }

    void OpenSystemKeyboard()
    {
#if UNITY_EDITOR
        Debug.Log("Editor mode: using inspector input");
        return;
#endif

        _overlayKeyboard = TouchScreenKeyboard.Open(
            _inputField.text,
            TouchScreenKeyboardType.Default
        );
    }

    void Update()
    {
#if UNITY_EDITOR
        if (_inputField.text != editorInput)
            _inputField.text = editorInput;
        return;
#endif

        if (_overlayKeyboard == null)
            return;

        _inputField.text = _overlayKeyboard.text;
        AuthManager.Instance.SetAccessCode(_overlayKeyboard.text);

        if (_overlayKeyboard.status == TouchScreenKeyboard.Status.Done ||
            _overlayKeyboard.status == TouchScreenKeyboard.Status.Canceled)
        {
            _overlayKeyboard = null;
        }
    }
}
