using System;
using System.Linq;
using System.IO;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace NotBox
{
    class Program
    {
        static string _path = string.Format(@"{0}\MyDocs",
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
        static CloudBlobContainer _container;

        [STAThread]
        static void Main(string[] args)
        {
            SetupBlobStorageAccess();

            SetupFileSynchronization();

            string consoleInput = string.Empty;
            while (!consoleInput.ToLower().Equals("x"))
            {
                Console.WriteLine("Contents of cloud container '{0}'\nSelect blob to share:", _container.Name);
                var blobs = _container.ListBlobs().ToArray<IListBlobItem>();
                for (int i = 0; i < blobs.Length;i++)
                {
                    Console.WriteLine("[{0}]: {1}", i, blobs[i].Uri);
                }
                Console.Write("[r] to refresh or ");
                Console.WriteLine("[x] to exit:");

                consoleInput = Console.ReadLine();

                int n = -1;
                if(int.TryParse(consoleInput, out n) && (n>0 && n<blobs.Length))
                {
                    var policy = new SharedAccessBlobPolicy()
                    {
                        Permissions = SharedAccessBlobPermissions.Read,
                        SharedAccessExpiryTime = DateTime.UtcNow + TimeSpan.FromMinutes(5)
                    };
                    var blob = blobs[n] as CloudBlockBlob;
                    string sas = blob.GetSharedAccessSignature(policy);
                    string sasUri = string.Format("{0}{1}", blob.Uri, sas);

                    Console.WriteLine(sasUri);

                    System.Windows.Clipboard.SetText(sasUri);
                }
            }
        }

        static void SetupBlobStorageAccess()
        {
            var accountName = "devgroup2401";
            var accountKey = @"vXldTVd2UjMRYPcdgU70uXNw6SJnrlsNOlwHaZGzHPzTofhqSD9HyRF7mjOMERNdcwwCKlet7KqE/7HEPM0t5w==";
            var credentials = new StorageCredentials(accountName, accountKey);

            CloudStorageAccount azureStorageAccount = new CloudStorageAccount(credentials, true);

            var blobClient = azureStorageAccount.CreateCloudBlobClient();
            _container = blobClient.GetContainerReference("mydocs");
            _container.CreateIfNotExists();
        }

        static void SetupFileSynchronization()
        {
            FileSystemEventHandler onFileCreatedOrChanged = (object sender, FileSystemEventArgs e) =>
            {
                var blob = _container.GetBlockBlobReference(e.Name);
                Console.WriteLine("Uploading '{0}'", e.Name);
                blob.UploadFromFileAsync(e.FullPath, FileMode.Open);
            };

            FileSystemEventHandler onFileDeleted = (object sender, FileSystemEventArgs e) =>
            {
                var blob = _container.GetBlockBlobReference(e.Name);
                Console.WriteLine("Deleting '{0}'", e.Name);
                blob.DeleteIfExists();
            };

            var watcher = new System.IO.FileSystemWatcher(_path);
            watcher.Created += onFileCreatedOrChanged;
            watcher.Changed += onFileCreatedOrChanged;
            watcher.Deleted += onFileDeleted;
            watcher.EnableRaisingEvents = true;

            Console.WriteLine("Watching {0}!", _path);
        }
    }
}
