using UnityEngine;

public class Setting : MonoBehaviour
{
    [SerializeField] private GameObject uiSetting;

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OpenSetting()
    {
        uiSetting.SetActive(true);
    }

    public void ClosedSetting()
    {
        uiSetting.SetActive(false);
    }
}
