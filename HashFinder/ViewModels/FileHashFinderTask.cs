using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using HashFinder.Models;
using Microsoft.Win32.SafeHandles;
using ThreadPoolLibrary;

namespace HashFinder.ViewModels
{
    class FileHashFinderTask :ITask
    {
        public FileHashes TaskFile { get; private set; }

        public FileHashFinderTask(FileHashes task)
        {
            TaskFile = task;
        }
        public void Execute()
        {
            var sha256 = SHA384.Create();
            var md5 = MD5.Create();
            TaskFile.Sha256Hash = "Processing";
            using (var fileStream = new FileStream(TaskFile.Path, FileMode.Open))
            {
                sha256.ComputeHash(fileStream);
                TaskFile.Sha256Hash = BitConverter.ToString(sha256.Hash);
    
            }
            TaskFile.Md5Hash = "Processing";
            using (var fileStream = new FileStream(TaskFile.Path, FileMode.Open))
            {
                md5.ComputeHash(fileStream);
                TaskFile.Md5Hash = BitConverter.ToString(md5.Hash);
            }
            sha256.Dispose();
            md5.Dispose();
            
            
        }
    }
}
