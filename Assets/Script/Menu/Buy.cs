using UnityEditor;
using UnityEngine;

public class Buy : MonoBehaviour
{
    [SerializeField] private GameObject uiBuy;
    [SerializeField] private GameObject smallPack;
    [SerializeField] private GameObject bigPack;
    [SerializeField] private GameObject adsPack;
    [SerializeField] private GameObject back;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OpenBuyUI()
    {
        uiBuy.SetActive(true);
    }

    public void ClosedBuyUI()
    {
        uiBuy.SetActive(false);
    }
}
