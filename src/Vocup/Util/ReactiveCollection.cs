using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Vocup.Util
{
    public class ReactiveCollection<T> : ObservableCollection<T>
    {
        private readonly List<Action<T>> onAddActions;
        private readonly List<Action<T>> onRemoveActions;

        public ReactiveCollection()
        {
            onAddActions = new List<Action<T>>();
            onRemoveActions = new List<Action<T>>();
        }

        public void ForEach(Action<T> action)
        {
            foreach (var item in this) action(item);
        }

        public void OnAdd(Action<T> action)
        {
            foreach (var item in this) action(item);
            onAddActions.Add(action);
        }

        public void OnRemove(Action<T> action)
        {
            onRemoveActions.Add(action);
        }

        protected override void InsertItem(int index, T item)
        {
            base.InsertItem(index, item);
            foreach (var action in onAddActions) action(item);
        }

        protected override void RemoveItem(int index)
        {
            var item = this[index];
            base.RemoveItem(index);
            foreach (var action in onRemoveActions) action(item);
        }
    }
}