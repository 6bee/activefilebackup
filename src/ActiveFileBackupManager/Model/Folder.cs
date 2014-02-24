// Copyright (c) Christof Senn. All rights reserved. See license.txt in the project root for license information.

using ActiveFileBackupManager.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ActiveFileBackupManager.Model
{
    [DebuggerDisplay("{_directory.FullPath}")]
    public sealed class Folder : Observable
    {
        #region Fields

        private readonly DirectoryInfo _directory;
        private readonly Folder _parent;
        private readonly ObservableCollection<Folder> _children;
        private Collection<SelectedFolder> _selectedFolders;
        private bool _isLoadingChildren = false;
        private bool _areChildrenLoaded = false;

        #endregion Fields

        #region Constrcutor

        public Folder(DriveInfo drive, Collection<SelectedFolder> selectedFolders)
            : this(null, drive.RootDirectory, selectedFolders)
        {
            LoadChildrenAsync(selectedFolders);
        }

        public Folder(Folder parent, DirectoryInfo directory, Collection<SelectedFolder> selectedFolders)
        {
            _parent = parent;
            _directory = directory;
            _selectedFolders = selectedFolders;
            _children = new ObservableCollection<Folder>();

            if (_parent != null)
            {
                _parent.PropertyChanged += (sender, e) =>
                {
                    switch (e.PropertyName)
                    {
                        case "IsRecursive":
                            OnPropertyChanged("Parent." + e.PropertyName);
                            break;
                    }
                };
            }

            var selected = _selectedFolders.FirstOrDefault(d => AreDirectoryEqual(d, this));
            if (selected != null)
            {
                selected.PropertyChanged += SelectedFolder_PropertyChanged;
                _isSelected = true;
                _isRecursive = selected.Recursive;
            }
            else
            {
                _isChildSelected = IsSubdirectorySelected;
            }
        }

        #endregion Constrcutor

        #region Properties

        public DirectoryInfo Directory { get { return _directory; } }

        public ObservableCollection<Folder> Children { get{return _children;} }

        public bool IsSelected
        {
            get { return _isSelected; }
            set { SetSelected(value); }
        }
        private bool _isSelected = false;

        public bool IsRecursive
        {
            get { return _isRecursive || HasParentRecursiveSelection; }
            set
            {
                if (_isRecursive == value) return;
                _isRecursive = value;
                OnPropertyChanged("IsRecursive");
            }
        }
        private bool _isRecursive = false;

        public bool IsChildSelected
        {
            get { return _isChildSelected; }
            private set
            {
                if (_isChildSelected == value) return;
                _isChildSelected = value;
                OnPropertyChanged(() => IsChildSelected);
            }
        }
        private bool _isChildSelected;

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (_isExpanded == value) return;
                _isExpanded = value;
                OnPropertyChanged("IsExpanded");
            }
        }
        private bool _isExpanded;

        public bool IsAccessForbidden
        {
            get { return _isAccessForbidden; }
            private set { _isAccessForbidden = value; OnPropertyChanged(() => IsAccessForbidden); }
        }
        private bool _isAccessForbidden;

        private bool HasRecursiveSelection
        {
            get { return (IsSelected && IsRecursive) || HasParentRecursiveSelection; }
        }

        private bool HasParentRecursiveSelection
        {
            get { return _parent != null && _parent.HasRecursiveSelection; }
        }

        #endregion Properties

        #region Methods

        protected override void OnPropertyChanged(string propertyName)
        {
            if (!propertyName.StartsWith("Parent."))
            {
                base.OnPropertyChanged(propertyName);
            }

            switch (propertyName)
            {
                case "Parent.IsRecursive":
                    if (_parent != null && _parent.HasRecursiveSelection) SetUnselected();
                    OnPropertyChanged(() => IsRecursive);
                    break;

                case "IsSelected":
                    UpdateSelection();
                    if (_parent != null) _parent.SetChildSelected(IsSelected);
                    break;

                case "IsRecursive":
                    UpdateSelection();
                    break;

                case "IsExpanded":
                    if (IsExpanded)
                    {
                        foreach (var item in Children) item.LoadChildrenAsync();
                    }
                    break;
            }
        }

        private void UpdateSelection()
        {
            if (IsSelected)
            {
                Select();
            }
            else
            {
                Unselect();
            }
        }

        private async void LoadChildrenAsync(IEnumerable<SelectedFolder> selectedFolders = null)
        {
            if (_areChildrenLoaded || _isLoadingChildren) return;
            _isLoadingChildren = true;

            try
            {
                var children = await _directory.GetDirectoriesAsync();
                foreach (var item in children)
                {
                    Children.Add(new Folder(this, item, _selectedFolders));
                }

                if (selectedFolders != null)
                {
                    foreach (var item in Children.ToList())
                    {
                        if (selectedFolders.Any(f => GetIsSubdirectorySelected(f.Directory, item.Directory.FullName)))
                        {
                            item.LoadChildrenAsync(selectedFolders);
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                IsAccessForbidden = true;
            }

            _areChildrenLoaded = true;
            _isLoadingChildren = false;
        }

        private void SetChildSelected(bool selected)
        {
            IsChildSelected = selected || IsSubdirectorySelected;
            if (_parent != null) _parent.SetChildSelected(IsChildSelected || IsSelected);
        }

        private bool IsSubdirectorySelected
        {
            get
            {
                return _selectedFolders.Any(d =>
                    Directory.FullName.Length < d.Directory.FullName.Length &&
                    GetIsSubdirectorySelected(d.Directory, Directory.FullName));
            }
        }

        private void SetUnselected()
        {
            if (_isSelected)
            {
                _isSelected = false;
                IsRecursive = false;
                OnPropertyChanged(() => IsSelected);
                if (!IsChildSelected) IsChildSelected = IsSubdirectorySelected;
            }
        }

        private void SetSelected(bool value)
        {
            if (value && HasParentRecursiveSelection) return;

            if (_isSelected)
            {
                if (IsRecursive)
                {
                    _isSelected = false;
                    OnPropertyChanged(() => IsSelected);
                    IsRecursive = false;
                }
                else
                {
                    IsRecursive = true;
                }
            }
            else if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged(() => IsSelected);
            }
        }

        private void Select()
        {
            var selected = _selectedFolders.FirstOrDefault(d => AreDirectoryEqual(d, this));
            if (selected != null)
            {
                selected.Recursive = IsRecursive;
            }
            else
            {
                selected = new SelectedFolder(Directory, IsRecursive);
                selected.PropertyChanged += SelectedFolder_PropertyChanged;
                _selectedFolders.Add(selected);
            }
        }

        private void Unselect()
        {
            var selected = _selectedFolders.FirstOrDefault(d => AreDirectoryEqual(d, this));
            if (selected != null)
            {
                _selectedFolders.Remove(selected);
                selected.PropertyChanged -= SelectedFolder_PropertyChanged;
            }
        }

        private void SelectedFolder_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Recursive":
                    IsRecursive = ((SelectedFolder)sender).Recursive;
                    break;

                case "Deleted":
                    SetUnselected();
                    break;
            }
        }

        private static bool GetIsSubdirectorySelected(DirectoryInfo directory, string path)
        {
            if (directory == null) return false;
            if (string.Compare(directory.FullName, path, true, CultureInfo.InvariantCulture) == 0) return true;
            if (path.Length >= directory.FullName.Length) return false;
            return GetIsSubdirectorySelected(directory.Parent, path);
        }

        public static bool AreDirectoryEqual(SelectedFolder d, Folder f)
        {
            return string.Compare(d.Directory.FullName, f.Directory.FullName, true, CultureInfo.InvariantCulture) == 0;
        }

        #endregion Methods
    }
}
