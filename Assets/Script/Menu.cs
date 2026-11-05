using UnityEngine;

public class Menu : MonoBehaviour
{
    [SerializeField] private GameObject uiChapter;
    [SerializeField] private GameObject uiEnding;

    public void OpenUIChapter()
    {
        uiChapter.SetActive(true);
    }

    public void OpenEnding()
    {
        uiEnding.SetActive(true);
    }

    public void ClosedChapter()
    {
        uiChapter.SetActive(false);
    }

    public void ClosedEnding()
    {
        uiEnding.SetActive(false);
    }
}
