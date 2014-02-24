// Copyright (c) Christof Senn. All rights reserved. See license.txt in the project root for license information.

using ActiveFileBackupManager.Model;
using ActiveFileBackupManager.Service;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ActiveFileBackupManager.ViewModel
{
    public sealed class DirectorySelectionViewModel : Observable
    {
        public IEnumerable<Folder> Directories { get; private set; }

        public ICollectionView SelectedFolders { get; private set; }

        public async Task InitializeAsync(ActiveFileBackup.Configuration.Config config)
        {
            var selectedFolders =
                from folder in config.FolderList
                select new SelectedFolder(new DirectoryInfo(folder.Path), folder.Recursive);
            var selectedFoldersObservableCollection = new ObservableCollection<SelectedFolder>(selectedFolders);
            selectedFoldersObservableCollection.CollectionChanged += SelectedFolders_CollectionChanged;

            var drives = await FileProvider.GetDrivesAsync();            
            var directories =
                from drive in drives
                select new Folder(drive, selectedFoldersObservableCollection);

            Directories = directories;
            OnPropertyChanged(() => Directories);

            var oldSelectedFolders = SelectedFolders;

            SelectedFolders = new ListCollectionView(selectedFoldersObservableCollection);
            SelectedFolders.SortDescriptions.Add(new SortDescription("Directory.FullName", ListSortDirection.Ascending));
            SelectedFolders.MoveCurrentToPosition(-1);
            OnPropertyChanged(() => SelectedFolders);

            if (oldSelectedFolders != null)
            {
                ((ObservableCollection<SelectedFolder>)oldSelectedFolders.SourceCollection).CollectionChanged -= SelectedFolders_CollectionChanged;
            }
        }

        private void SelectedFolders_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace:
                    var removedItems = e.OldItems;
                    if (removedItems != null)
                    {
                        foreach (SelectedFolder item in removedItems)
                        {
                            item.RaiseDeletedNotification();
                        }
                    }
                    break;
            }
        }

        public void UpdateConfig(ActiveFileBackup.Configuration.Config config)
        {
            config.FolderList.Clear();

            var selectedFolders = SelectedFolders.OfType<SelectedFolder>().OrderBy(x => x.Directory.FullName).ToList();
            foreach (var folder in selectedFolders)
            {
                config.FolderList.Add(new ActiveFileBackup.Configuration.Folder(folder.Directory.FullName, folder.Recursive));
            }
        }
    }
}
