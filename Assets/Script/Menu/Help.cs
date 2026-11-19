using UnityEngine;

public class Help : MonoBehaviour
{
    [SerializeField] private GameObject uiHelp;
    [SerializeField] private GameObject useCoin;
    [SerializeField] private GameObject useAds;
    [SerializeField] private GameObject back;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OpenHelp()
    {
        uiHelp.SetActive(true);
    }

    public void ClosedHelp()
    {
        uiHelp.SetActive(false);
    }
}
