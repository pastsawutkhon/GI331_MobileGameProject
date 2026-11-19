using UnityEngine;

public class Ending : MonoBehaviour
{
    [SerializeField] private GameObject showFamUI;
    [SerializeField] private GameObject[] uiEnding;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void ShowFamEnding()
    {
        foreach (GameObject obj in uiEnding)
            if (obj != null) obj.SetActive(false);

        showFamUI.SetActive(true);
    }

    public void Back()
    {
        foreach (GameObject obj in uiEnding)
            if (obj != null) obj.SetActive(true);

        showFamUI.SetActive(false);
    }
}
