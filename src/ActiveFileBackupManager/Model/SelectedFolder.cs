using System.Diagnostics;
using System.IO;

namespace ActiveFileBackupManager.Model
{
    [DebuggerDisplay("{Directory.FullPath}   ({Recursive})")]
    public sealed class SelectedFolder : Observable
    {
        public SelectedFolder(DirectoryInfo directory, bool recursive)
        {
            Directory = directory;
            _recursive = recursive;
        }
        
        public DirectoryInfo Directory { get; private set; }
        
        public bool Recursive
        {
            get { return _recursive; }
            set
            {
                if (_recursive == value) return;
                _recursive = value;
                OnPropertyChanged(() => Recursive);
            }
        }
        private bool _recursive;

        internal void RaiseDeletedNotification()
        {
            OnPropertyChanged("Deleted");
        }
    }
}
