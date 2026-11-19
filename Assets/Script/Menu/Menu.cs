
using UnityEngine;

public class Menu : MonoBehaviour
{
    [Header("Background")]
    [SerializeField] private GameObject mainBackground;
    [SerializeField] private GameObject selectionBackground;

    [Header("Main Menu Buttons")]
    [SerializeField] private GameObject[] mainMenuButtons;

    [Header("Selection UI")]
    [SerializeField] private GameObject chapterUI;
    [SerializeField] private GameObject endingUI;

    [Header("Back Button")]
    [SerializeField] private GameObject backButton;

    private void Start()
    {
        ShowMainMenu();
    }

    public void ShowChapter()
    {
        mainBackground.SetActive(false);
        selectionBackground.SetActive(true);

        foreach (GameObject obj in mainMenuButtons)
            if (obj != null) obj.SetActive(false);

        chapterUI.SetActive(true);
        endingUI.SetActive(false);
        backButton.SetActive(true);
    }

    public void ShowEnding()
    {
        mainBackground.SetActive(false);
        selectionBackground.SetActive(true);

        foreach (GameObject obj in mainMenuButtons)
            if (obj != null) obj.SetActive(false);

        chapterUI.SetActive(false);
        endingUI.SetActive(true);
        backButton.SetActive(true);
    }

    public void ShowMainMenu()
    {
        mainBackground.SetActive(true);
        selectionBackground.SetActive(false);

        foreach (GameObject obj in mainMenuButtons)
            if (obj != null) obj.SetActive(true);

        chapterUI.SetActive(false);
        endingUI.SetActive(false);
        backButton.SetActive(false);
    }
}
