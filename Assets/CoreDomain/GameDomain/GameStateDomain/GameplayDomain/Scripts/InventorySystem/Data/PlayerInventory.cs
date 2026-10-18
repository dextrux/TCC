using System;
using System.Collections.Generic;
using System.Linq;
using CoreDomain.Scripts.Services.Logger.Base;
using UnityEngine;

namespace CoreDomain.GameDomain.GameStateDomain.GameplayDomain.Scripts.InventorySystem.Data
{
    public class PlayerInventory
    {
        private readonly ItemStack[] _slots;
        
        public event Action OnInventoryChanged;

        public int Capacity => _slots.Length;
        public IReadOnlyList<ItemStack> Slots => _slots;
        
        public int SelectedIndex { get; private set; }
        public ItemStack SelectedStack => _slots[SelectedIndex];

        public PlayerInventory(int slotCount)
        {
            _slots = new ItemStack[Mathf.Max(1, slotCount)];
        }

        public bool Selected(int index)
        {
            if (index < 0 || index >= _slots.Length || index == SelectedIndex) return false;
            
            SelectedIndex = index;
            OnInventoryChanged?.Invoke();
            return true;
        }
        
        public void ScrollToSelected(int direction)
        {
            if (direction == 0) return;
            
            var length = _slots.Length;
            Selected(((SelectedIndex + direction) % length + length) % length) ;
        }

        private bool CanAdd(int itemId, int amount, int maxStacks)
        {
            if(amount <= 0 || maxStacks <= 0) return false;

            var space = 0;
            foreach (var slot in _slots)
            {
                if(slot.IsEmpty()) space += maxStacks;
                else if(slot.ItemId == itemId) space += Mathf.Max(0, maxStacks - slot.Count);
                
                if(space >= amount)
                {
                    LogService.Log($"[PlayerInventory] Trying adding item {itemId}");
                    return true;
                }
            }

            LogService.Log("[PlayerInventory] Nao foi possivel adicionar o item");
            return false;
        }

        public bool TryAdd(int itemId, int amount, int maxStacks)
        {
            if (!CanAdd(itemId, amount, maxStacks)) return false;

            //Adicionando no pack do item
            for (var i = 0; i < _slots.Length && amount > 0; i++)
            {
                var slot = _slots[i];
                if(slot.IsEmpty() || slot.ItemId != itemId || slot.Count >= maxStacks) continue;
                
                
                var take = Mathf.Min(amount, maxStacks - slot.Count);
                _slots[i] = new  ItemStack(itemId, slot.Count + take);
                amount -= take;
            }
            
            //Slots vazios
            for (var i = 0; i < _slots.Length && amount > 0; i++)
            {
                if(!_slots[i].IsEmpty()) continue;
                
                var take = Mathf.Min(amount, maxStacks);
                _slots[i] = new ItemStack(itemId, take);
                amount -= take;
            }
            
            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool TryRemove(int itemId, int amount)
        {
            if (amount <= 0) return false;

            var total = _slots.Where(slot => !slot.IsEmpty() && slot.ItemId == itemId).Sum(slot => slot.Count);

            if (total < amount)
            {
                LogService.Log($"[PlayerInventory] Trying removing failed because {itemId} have more than {total} items");
                return false;
            }

            for (var i = _slots.Length - 1; i >= 0 && amount > 0; i--)
            {
                var slot = _slots[i];
                if(slot.IsEmpty() || slot.ItemId != itemId) continue;
                
                var take = Mathf.Min(amount, slot.Count);
                _slots[i] = new ItemStack(itemId, slot.Count - take);
                amount -= take;
            }
            
            OnInventoryChanged?.Invoke();
            return true;
        }
    }
}