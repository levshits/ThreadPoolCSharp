using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashFinder.Models
{
    class FileHashes :INotifyPropertyChanged
    {
        private string _path;
        private string _sha256Hash;
        private string _md5Hash;

        public string Path
        {
            get { return _path; }
            set
            {
                _path = value;
                OnPropertyChanged("DirectoryPath");
            }
        }

        public string Sha256Hash
        {
            get
            {
                return _sha256Hash;
            }
            set
            {
                _sha256Hash = value;
                OnPropertyChanged("Sha256Hash");
            }
        }

        public string Md5Hash
        {
            get
            {
                return _md5Hash;
            }
            set
            {
                _md5Hash = value;
                OnPropertyChanged("Md5Hash");
            }
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion INotifyPropertyChanged
    }
}
