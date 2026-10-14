namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.Data
{
    public readonly struct ItemStack
    {
        public readonly int ItemId;
        public readonly int Count;

        public ItemStack(int itemId, int count)
        {
            ItemId = itemId;
            Count = count;
        }
        
        public bool IsEmpty() => Count <= 0;
    }
}