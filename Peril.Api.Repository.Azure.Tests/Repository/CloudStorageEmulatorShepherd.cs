using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Diagnostics;
using System.IO;

namespace Peril.Api.Repository.Azure.Tests
{
    public class CloudStorageEmulatorShepherd
    {
        public void Start()
        {
            try
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.DevelopmentStorageAccount;

                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference("test");
                container.CreateIfNotExists(
                            new BlobRequestOptions()
                            {
                                RetryPolicy = new Microsoft.WindowsAzure.Storage.RetryPolicies.NoRetry(),
                                ServerTimeout = new TimeSpan(0, 0, 0, 1)
                            });
            }
            catch(StorageException exception)
            {
                if(exception.HResult == -2146233088)
                {
                    TryToStartStorageEmulator();
                }
            }
            catch (TimeoutException)
            {
                TryToStartStorageEmulator();
            }
        }

        private void TryToStartStorageEmulator()
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo()
            {
                FileName = Path.Combine(@"C:\Program Files (x86)\Microsoft SDKs\Azure\Storage Emulator", "AzureStorageEmulator.exe"),
                Arguments = "start",
                WorkingDirectory = @"C:\Program Files (x86)\Microsoft SDKs\Azure\Storage Emulator"
            };

            using (Process process = Process.Start(processStartInfo))
            {
                process.WaitForExit();
            }
        }
    }
}
