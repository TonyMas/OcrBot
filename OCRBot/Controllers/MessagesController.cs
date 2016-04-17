using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using System.Configuration;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Utilities;
using Newtonsoft.Json;
using System.Web.Http.Results;
using System.Web;
using System.Collections.Generic;
using Abbyy.CloudOcrSdk;
using System.IO;
using Microsoft.WindowsAzure.Storage;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Auth;

namespace OCRBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<Message> Post([FromBody]Message message)
        {
            if (message.Type == "Message")
            {
                int attachCount = message.Attachments.Count;

                if( attachCount == 0)
                {
                    return message.CreateReplyMessage($"*Send me some image files to begin recognition*");
                }

                System.Threading.Timer timer = null;
                timer = new System.Threading.Timer((obj) =>
                {
                    ProcessAttaches(message);
                    timer.Dispose();
                }, null, 1, System.Threading.Timeout.Infinite);

                return message.CreateReplyMessage($"Your files will be processed shortly");
            }
            else
            {
                return HandleSystemMessage(message);
            }
        }

        private void ProcessAttaches(Message message)
        {
            int attachCount = message.Attachments.Count;

#if EMULATOR
            var connector = new ConnectorClient(new Uri("http://localhost:9000"), new ConnectorClientCredentials());
#else
            var connector = new ConnectorClient();
#endif

            var i = 1;
            foreach (Attachment a in message.Attachments)
            {
                if (!a.ContentType.StartsWith("image/"))
                {
                    Message replyMessage = message.CreateReplyMessage($"Wrong type of file {i}: {a.ContentType}");
                    connector.Messages.SendMessage(replyMessage);
                }
                else
                {
                    RecognizeUrl(a.ContentUrl, i, connector, message);
                }
                i++;
            }
        }

        private void RecognizeUrl(string contentUrl, int index, ConnectorClient connector, Message message)
        {
            connector.Messages.SendMessage(message.CreateReplyMessage($"Begin recognition of image {index}"));

            RestServiceClient restClient = new RestServiceClient();
            restClient.Proxy.Credentials = CredentialCache.DefaultCredentials;

            restClient.ApplicationId = ConfigurationManager.AppSettings["OCRAppId"];
            restClient.Password = ConfigurationManager.AppSettings["OCRAppPass"];

            string tmpFileName = Path.GetTempFileName();

            restClient.DownloadUrl(contentUrl, tmpFileName);

            ProcessingSettings settings = buildSettings("English", "docx", null);

            Abbyy.CloudOcrSdk.Task task = restClient.ProcessImage(tmpFileName, settings);

            task = waitForTask(restClient, task);

            connector.Messages.SendMessage(message.CreateReplyMessage($"Image {index} recognition completed"));

            for (int i = 0; i < settings.OutputFormats.Count; i++)
            {
                string resultLnk = DownloadResult(task.DownloadUrls[i], restClient);
                var replyMessage = message.CreateReplyMessage($"Image {index} result: [link]({resultLnk})");
                connector.Messages.SendMessage(replyMessage);
            }

            File.Delete(tmpFileName);
        }

        private string DownloadResult(string downloadLnk, RestServiceClient restClient)
        {
            string tmpFileName = Path.GetTempFileName();
            restClient.DownloadUrl(downloadLnk, tmpFileName);

            string storageName = ConfigurationManager.AppSettings["StorageName"];
            string storagePass = ConfigurationManager.AppSettings["StoragePass"];
            string storageBlob = ConfigurationManager.AppSettings["StorageBlob"];

            StorageCredentials credentials = new StorageCredentials(storageName, storagePass);
            CloudStorageAccount storageAccount = new CloudStorageAccount(credentials,true);

            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve a reference to a container.
            CloudBlobContainer container = blobClient.GetContainerReference(storageBlob);

            // Create the container if it doesn't already exist.
            container.CreateIfNotExists();

            container.SetPermissions( new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });

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

        private Abbyy.CloudOcrSdk.Task waitForTask(RestServiceClient restClient, Abbyy.CloudOcrSdk.Task task)
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

        private Message HandleSystemMessage(Message message)
        {
            if (message.Type == "Ping")
            {
                Message reply = message.CreateReplyMessage();
                reply.Type = "Ping";
                return reply;
            }
            else if (message.Type == "DeleteUserData")
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == "BotAddedToConversation")
            {
                return message.CreateReplyMessage($"Hello! I am OCR Bot, send me some document images.");
            }
            else if (message.Type == "BotRemovedFromConversation")
            {
                return message.CreateReplyMessage($"Bye!");
            }
            else if (message.Type == "UserAddedToConversation")
            {
            }
            else if (message.Type == "UserRemovedFromConversation")
            {
            }
            else if (message.Type == "EndOfConversation")
            {
            }

            return null;
        }
    }
}