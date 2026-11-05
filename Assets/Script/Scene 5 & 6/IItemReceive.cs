namespace Solution
{
    public interface IItemReceiver
    {
        bool CanReceiveItem(string itemName);
        void OnReceiveItem(string itemName);
    }
}