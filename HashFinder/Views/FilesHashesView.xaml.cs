using System;
using System.Windows;
using HashFinder.ViewModels;

namespace HashFinder
{
    /// <summary>
    /// Interaction logic for FilesHasvesView.xaml
    /// </summary>
    public partial class FilesHashesView : Window
    {

        public FilesHashesView()
        {
            InitializeComponent();
            var fileHashesViewModel = this.DataContext as FileHashesViewModel;
            if (fileHashesViewModel != null)
            {
                Closing += fileHashesViewModel.OnClosing;
            }
            else
            {
                throw new Exception("Start failure");
            }
        }
    }
}
