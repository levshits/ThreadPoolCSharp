using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using HashFinder.Models;
using ThreadPoolLibrary;
using System.IO;
using System.Threading;
using System.Windows.Documents;
using ThreadPool = ThreadPoolLibrary.ThreadPool;

namespace HashFinder.ViewModels
{
    class FileHashesViewModel :INotifyPropertyChanged, ILogger
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion INotifyPropertyChanged

        private string _directoryPath;

        public string DirectoryPath
        {
            get
            {
                return _directoryPath;
            }
            set
            {
                _directoryPath = value;
                OnPropertyChanged("DirectoryPath");
            }
        }

        private ThreadPool _pool;
        
        public ObservableCollection<FileHashes> FilesHashes { get; private set; }

        public ICommand LoadFilesCommand { get; private set; }
        public ICommand StartFindingHashesCommand { get; private set; }
        public FileHashesViewModel()
        {
            FilesHashes = new ObservableCollection<FileHashes>();
            _pool = new ThreadPool(0, 10, 3000);
            _pool.Logger = this;
            _pool.Run();
            LoadFilesCommand = new LoadFiles(this);
            StartFindingHashesCommand = new StartFindingHashes(this);
        }
        private object locker = new object();
        private const string LogFilePath = "log";

        public void Log(string msg)
        {
            
            lock (locker)
            {
                if (File.Exists(LogFilePath) || File.Create(LogFilePath) != null)
                {
                    try
                    {
                        var sw = File.AppendText(LogFilePath);
                        sw.WriteLine(String.Format("{0} : {1}", DateTime.Now, msg));
                        sw.Close();
                    }
                    catch (Exception)
                    {
                        //do nothing
                    }
                }

            }
        }
        public bool CanLoadFiles
        {
            get
            {
                var result = false;
                if (DirectoryPath != null)
                {
                    result = Directory.Exists(DirectoryPath);
                }
                return result;
            }
            
        }

        public void OnClosing(object sender, CancelEventArgs e) 
        {
            var pool = _pool;
            if (pool != null)
            {
                _pool.Stop();
            }
        }
        class LoadFiles :ICommand
        {
            #region ICommand
            public bool CanExecute(object parameter)
            {
                return _viewModel.CanLoadFiles;
            }

            public void Execute(object parameter)
            {
                var path = _viewModel.DirectoryPath;
                _viewModel.FilesHashes.Clear();
                foreach (var file in Directory.GetFiles(path))
                {
                    _viewModel.FilesHashes.Add(new FileHashes(){Path = file});
                }
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }
            #endregion ICommand

            private readonly FileHashesViewModel _viewModel;
            public LoadFiles(FileHashesViewModel viewModel)
            {
                _viewModel = viewModel;
            }

        }

        
        class StartFindingHashes :ICommand
        {
            #region ICommand
            public bool CanExecute(object parameter)
            {
                return _viewModel.FilesHashes != null && _viewModel.FilesHashes.Count > 0;
            }

            public void Execute(object parameter)
            {
                foreach (var file in _viewModel.FilesHashes)
                {
                    _viewModel._pool.QueueTask(new FileHashFinderTask(file));
                }
                
            }

            public event EventHandler CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }

            #endregion ICommand

            private FileHashesViewModel _viewModel;
            public StartFindingHashes(FileHashesViewModel viewModel)
            {
                _viewModel = viewModel;
            }
        }
    }
}
