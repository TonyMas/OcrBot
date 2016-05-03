using Abbyy.CloudOcrSdk;
using Microsoft.Bot.Connector;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;

namespace OCRBot
{
    public class Recognizer
    {
        public Recognizer(ReplyMessageSender _messageSender)
        {
            messageSender = _messageSender;
        }

        private ReplyMessageSender messageSender { get; set; }

        public void RecognizeAttachments(IList<Attachment> attachments)
        {
            foreach (Attachment a in attachments)
            {
                var attachName = getFileNameFromUrl(a.ContentUrl);
                if (!a.ContentType.StartsWith("image/"))
                {
                    Message replyMessage = messageSender.CreateMessage($"Wrong type of file **{attachName}**: *{a.ContentType}*");
                    messageSender.SendMessage(replyMessage);
                }
                else
                {
                    recognizeImage(a.ContentUrl, attachName);
                }
            }
        }

        private string getFileNameFromUrl(string contentUrl)
        {
            const string fileString = "?file=";
            var fileIndex = contentUrl.IndexOf(fileString, StringComparison.InvariantCultureIgnoreCase);
            if ( fileIndex != -1 )
            {
                var result = contentUrl.Substring(fileIndex + fileString.Length);
                if( result.Length > 0 )
                {
                    return result;
                }
            }
            return "<unknown_file>";
        }

        private void recognizeImage(string contentUrl, string attachName)
        {
            messageSender.SendMessage($"Begin recognition of image **{attachName}**");

            return;

            RestServiceClient restClient = new RestServiceClient();
            restClient.Proxy.Credentials = CredentialCache.DefaultCredentials;

            restClient.ApplicationId = ConfigurationManager.AppSettings["OCRAppId"];
            restClient.Password = ConfigurationManager.AppSettings["OCRAppPass"];

            string tmpFileName = Path.GetTempFileName();

            restClient.DownloadUrl(contentUrl, tmpFileName);

            ProcessingSettings settings = buildSettings("English", "docx", null);

            Task task = restClient.ProcessImage(tmpFileName, settings);

            task = waitForTask(restClient, task);

            messageSender.SendMessage($"Image **{attachName}** recognition completed");

            for (int i = 0; i < settings.OutputFormats.Count; i++)
            {
                string resultLnk = downloadResult(task.DownloadUrls[i], restClient);
                var replyMessage = messageSender.CreateMessage($"Image **{attachName}** result: [link]({resultLnk})");
                messageSender.SendMessage(replyMessage);
            }

            File.Delete(tmpFileName);
        }

        private string downloadResult(string downloadLnk, RestServiceClient restClient)
        {
            string tmpFileName = Path.GetTempFileName();
            restClient.DownloadUrl(downloadLnk, tmpFileName);

            string storageName = ConfigurationManager.AppSettings["StorageName"];
            string storagePass = ConfigurationManager.AppSettings["StoragePass"];
            string storageBlob = ConfigurationManager.AppSettings["StorageBlob"];

            StorageCredentials credentials = new StorageCredentials(storageName, storagePass);
            CloudStorageAccount storageAccount = new CloudStorageAccount(credentials, true);

            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve a reference to a container.
            CloudBlobContainer container = blobClient.GetContainerReference(storageBlob);

            // Create the container if it doesn't already exist.
            container.CreateIfNotExists();

            container.SetPermissions(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });

            // Retrieve reference to a blob named "myblob".
            string blobName = Guid.NewGuid().ToString() + ".docx";
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);

            // Create or overwrite the "myblob" blob with contents from a local file.
            using (var fileStream = System.IO.File.OpenRead(tmpFileName))
            {
                blockBlob.UploadFromStream(fileStream);
            }

            File.Delete(tmpFileName);

            return blockBlob.Uri.ToString();
        }

        private Task waitForTask(RestServiceClient restClient, Task task)
        {
            Console.WriteLine(String.Format("Task status: {0}", task.Status));
            while (task.IsTaskActive())
            {
                // Note: it's recommended that your application waits
                // at least 2 seconds before making the first getTaskStatus request
                // and also between such requests for the same task.
                // Making requests more often will not improve your application performance.
                // Note: if your application queues several files and waits for them
                // it's recommended that you use listFinishedTasks instead (which is described
                // at http://ocrsdk.com/documentation/apireference/listFinishedTasks/).
                System.Threading.Thread.Sleep(5000);
                task = restClient.GetTaskStatus(task.Id);
                Console.WriteLine(String.Format("Task status: {0}", task.Status));
            }
            return task;
        }

        private static ProcessingSettings buildSettings(string language,
            string outputFormat, string profile)
        {
            ProcessingSettings settings = new ProcessingSettings();
            settings.SetLanguage(language);
            switch (outputFormat.ToLower())
            {
                case "txt": settings.SetOutputFormat(OutputFormat.txt); break;
                case "rtf": settings.SetOutputFormat(OutputFormat.rtf); break;
                case "docx": settings.SetOutputFormat(OutputFormat.docx); break;
                case "xlsx": settings.SetOutputFormat(OutputFormat.xlsx); break;
                case "pptx": settings.SetOutputFormat(OutputFormat.pptx); break;
                case "pdfsearchable": settings.SetOutputFormat(OutputFormat.pdfSearchable); break;
                case "pdftextandimages": settings.SetOutputFormat(OutputFormat.pdfTextAndImages); break;
                case "xml": settings.SetOutputFormat(OutputFormat.xml); break;
                default:
                    throw new ArgumentException("Invalid output format");
            }
            if (profile != null)
            {
                switch (profile.ToLower())
                {
                    case "documentconversion":
                        settings.Profile = Profile.documentConversion;
                        break;
                    case "documentarchiving":
                        settings.Profile = Profile.documentArchiving;
                        break;
                    case "textextraction":
                        settings.Profile = Profile.textExtraction;
                        break;
                    default:
                        throw new ArgumentException("Invalid profile");
                }
            }

            return settings;
        }
    }
}