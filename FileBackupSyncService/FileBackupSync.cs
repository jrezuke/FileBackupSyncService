using System;
using System.Collections.Generic;
using System.IO;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf.Logging;

namespace FileBackupSyncService
{
    public class FileBackupSync
    {
        private FileSystemWatcher _watcher;
        private static readonly LogWriter Log = HostLogger.Get<FileBackupSync>();
        private string _destinationBasePath;
        private string _baseFolder;
        private string _sourceBasePath;

        public bool Start()
        {
            Log.InfoFormat("FileBackupSync.Start");

            //from app.config appSettings
            _destinationBasePath = ConfigurationManager.AppSettings["DestinationBasePath"].ToLower();
            _baseFolder = ConfigurationManager.AppSettings["BaseFolder"].ToLower();
            _sourceBasePath = ConfigurationManager.AppSettings["SourceBasePath"].ToLower();

            var sourceFolderWatched = Path.Combine(_sourceBasePath, _baseFolder);

            _watcher = new FileSystemWatcher(sourceFolderWatched);
            //_watcher.Filter = "*.*";  //this is the default
            _watcher.Created += OnNewFile;
            _watcher.Renamed += OnRenamed;
            _watcher.IncludeSubdirectories = true;
            _watcher.EnableRaisingEvents = true;
            
            return true;
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This function responds to any new file/folder 
        /// if it's a folder - do nothing
        /// if it's a file - create the folder if it doesn't exist
        /// copy the source file to the destination folder
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">
        /// FileSystemEventArgs e gives you path information about the new file/folder
        /// </param>
        public void OnNewFile(object sender, FileSystemEventArgs e)
        {
            Log.InfoFormat("FileBackupSync.OnNewFile:");
            Log.InfoFormat("source fullPath: '{0}'", e.FullPath);

            //source full path (includes the name of the file/folder (e.Name = the name of the new file/folder)
            var sourceFullPath = e.FullPath;

            //this is used to distinguish between a file or folder (for the end point)
            FileAttributes attr = System.IO.File.GetAttributes(sourceFullPath);

            //if it's a folder then do nothing now
            //new folders will get created when there is a new file added to them 
            if ((attr & FileAttributes.Directory) != 0)
            {
                Log.InfoFormat("It's a folder ({0}) so doing nothing", e.Name);
                return;
            }

            //at this point we are dealing with files only

            //this removes the file name leaving only the path
            var sourcePath = Path.GetDirectoryName(sourceFullPath);
            Log.InfoFormat("source path: '{0}'", sourcePath);

            //this gets the destination folder (it's created if it doesn't exist)  
            var destinationFolder = GetDestinationPath(sourcePath, true);
            Log.InfoFormat("destination path: '{0}'", destinationFolder);

            var fileName = Path.GetFileName(e.FullPath);
            Log.InfoFormat("file name: '{0}'", fileName);

            //copy the file to destination
            if (fileName != null)
            {
                try
                {
                    File.Copy(e.FullPath, Path.Combine(destinationFolder, fileName));
                    Log.WarnFormat("{0} was copied to {1}.", fileName, destinationFolder);
                }
                catch (IOException ex)
                {
                    var message = ex.Message;
                    Log.ErrorFormat("Error message {0} ", message);
                }
            }
        }

        /// <summary>
        /// This will return the destination path based on the source path passed in
        /// Optional - will create the destination path if the second parameter is true
        /// </summary>
        /// <param name="srcDirName"></param>
        /// <param name="createDestinationFolder"></param>
        /// <returns>The destination path</returns>
        private string GetDestinationPath(string srcDirName, bool createDestinationFolder = false)
        {
            Log.InfoFormat("FileBackupSync.GetDestinationPath:");

            //if the file is in destination base folder then use dest base path 
            if (srcDirName.EndsWith(_baseFolder))
                return Path.Combine(_destinationBasePath, _baseFolder);

            //if there are there any subfolders after the base folder - grab them and form the destination folder
            var pos = 0;
            var foldersAfterBase = "";

            pos = srcDirName.IndexOf(_baseFolder, StringComparison.CurrentCulture);
            foldersAfterBase = srcDirName.Substring(_baseFolder.Length + pos + 1);

            var destinationFolder = Path.Combine(_destinationBasePath, _baseFolder, foldersAfterBase);

            //this will create the folder if it doesn't exist otherwise just returns directory info on the existing folder
            //returns directory info on the created folder as well
            if (createDestinationFolder)
            {
                var directoryInfo = Directory.CreateDirectory(destinationFolder);
            }

            return destinationFolder;
        }

        public bool Pause()
        {
            _watcher.EnableRaisingEvents = false;
            return true;
        }

        public bool Continue()
        {
            _watcher.EnableRaisingEvents = true;
            return true;
        }

        public bool Stop()
        {
            _watcher.Dispose();
            return true;
        }

    }
}
