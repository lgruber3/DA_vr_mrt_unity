using UnityEngine;
using UnityEngine.UI;

public class UILoginDropdown : MonoBehaviour
{
    [SerializeField] RectTransform backplate;
    public GameObject createMeetingButton;
    public GameObject joinMeetingButton;
    public GameObject joinCodeInputField;

    public void OnDropdownValueChanged(int value)
    {
        Debug.Log("Dropdown value changed to: " + value);
        if (value == 0) 
        {
            createMeetingButton.SetActive(true);
            joinMeetingButton.SetActive(false);
            joinCodeInputField.SetActive(false);
        }
        else if (value == 1) 
        {
            createMeetingButton.SetActive(false);
            joinMeetingButton.SetActive(true);
            joinCodeInputField.SetActive(true);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(backplate);
    }
}
