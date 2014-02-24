// Copyright (c) Christof Senn. All rights reserved. See license.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Windows.Threading;

namespace ActiveFileBackupManager.Model
{
    public abstract class Observable : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var propertyChanged = PropertyChanged;
            if (propertyChanged != null)
            {
                Invoke(() => propertyChanged(this, new PropertyChangedEventArgs(propertyName)));
            }
        }

        protected void OnPropertyChanged<T>(Expression<Func<T>> property)
        {
            var propertyName = ((MemberExpression)property.Body).Member.Name;
            OnPropertyChanged(propertyName);
        }

        protected void Invoke(Action action, bool sync = false)
        {
            var dispatcher = Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                if (sync)
                {
                    dispatcher.BeginInvoke(action);
                }
                else
                {
                    dispatcher.Invoke(action);
                }
            }
            else
            {
                action();
            }
        }

        public static Dispatcher Dispatcher
        {
            get { return Dispatcher.CurrentDispatcher; }
        }
    }
}
