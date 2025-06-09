using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ViSyncMaster.DataModel;

public static class ObservableCollectionExtensions
{
    public static void SyncWith<T, TKey>(
        this ObservableCollection<T> existing,
        IEnumerable<T> newItems,
        Func<T, TKey> keySelector)
    {
        var existingByKey = existing.ToDictionary(keySelector);
        var newByKey = newItems.ToDictionary(keySelector);

        // 1) Usuń elementy, których już nie ma
        foreach (var keyToRemove in existingByKey.Keys.Except(newByKey.Keys).ToList())
            existing.Remove(existingByKey[keyToRemove]);

        // 2) Dodaj nowe elementy
        foreach (var keyToAdd in newByKey.Keys.Except(existingByKey.Keys).ToList())
            existing.Add(newByKey[keyToAdd]);

        // 3) Zastąp zmienione elementy
        foreach (var keyToUpdate in existingByKey.Keys.Intersect(newByKey.Keys))
        {
            var oldItem = existingByKey[keyToUpdate];
            var newItem = newByKey[keyToUpdate];

            if (!EqualityComparer<T>.Default.Equals(oldItem, newItem))
            {
                // Załóżmy, że T implementuje IUpdatable<T>
                // lub ręcznie przypisujesz właściwości:
                if (oldItem is MachineStatusGrouped oldGr && newItem is MachineStatusGrouped newGr)
                {
                    oldGr.ShiftCounterPass = newGr.ShiftCounterPass;
                    oldGr.ShiftCounterFail = newGr.ShiftCounterFail;
                    oldGr.Operators = newGr.Operators;
                    // itd. — wszystkie właściwości, które mogą się różnić
                }
                else
                {
                    // inne typy T — albo ignoruj, albo wymuś replace
                }
            }
        }
    }
}
