using Microsoft.Office.Interop.Outlook;
using System.Net.FtpClient;

namespace FLCHub.API.Services.Generic
{
    public class FtpHelper
    {
        //Unchanging information for FTP
        public static string InitialDirectory = "/Image Downloads/";
        private static string Host = "flc.files.com";
        private static string User = "Scrubber";
        private static string Password = "LidsUImageScrubber!23";


        public static FtpClient CreateFilesFtpClient()
        {
            //---Creates FtpClient and sets up timeout using unchanging info above---
            FtpClient client = new FtpClient();
            client.Host = Host;
            client.Credentials = new System.Net.NetworkCredential(User, Password);
            client.DataConnectionReadTimeout = (300 * 1000);
            return client;
        }

        public static Dictionary<string, string> GenerateVendor_PathDictionary(FtpClient client)
        {
            //---Grabs all of the vendor folders turning them into key value pairs---
            // makes it easy for front-end to display Name, but send back-end full path
            Dictionary<string, string> vendor_path = new Dictionary<string, string>();
            FtpListItem[] ftpListItems = client.GetListing(InitialDirectory);

            foreach (FtpListItem ftpItem in ftpListItems)
            {
                string fullName = ftpItem.FullName;
                string vendorName = ftpItem.Name;
                vendor_path[vendorName] = fullName;
            }
            return vendor_path;
        }

        public static List<string> GrabAllItemsInVendorFolder(FtpClient client, string vendorPath)
        {
            //---Grabs every item in vendor directory, itterates through any folders within vendor directory
            FtpListItem[] ftpListItems = client.GetListing(vendorPath);
            //These two folder holders used to take turns holding folders
            //until all folders have been opened for vendor
            List<string> folderHolder1 = new List<string>();
            List<string> folderHolder2 = new List<string>();
            List<string> files = new List<string>();

            SortFtpItems(files, folderHolder1, ftpListItems);
            if (folderHolder1.Count > 0)
            {
                ScrubFolders(files, folderHolder1, folderHolder2, client);
            }
            client.Disconnect();

            return FilterImagesFromItems(files);
        }

        private static void SortFtpItems(List<string> files, List<string> targetFolder, FtpListItem[] items)
        {
            //---Itterates through FtpListItem[] putting files and folders into corresponding lists---
            foreach (FtpListItem ftpListItem in items)
            {
                if (ftpListItem.Type == FtpFileSystemObjectType.Directory)
                {
                    targetFolder.Add(ftpListItem.FullName);
                }
                else
                {
                    files.Add(ftpListItem.FullName);
                }
            }
        }

        private static void ScrubFolders(List<string> files, List<string> folderHolder1, 
            List<string> folderHolder2, FtpClient client)
        {
            //---Method continues to find folders and sort their contents till there are no more folders---
            bool hasFolders = true;
            while (hasFolders)
            {
                //opens folders in holder, sorts items,
                //places folders into other holder, then clears initial holder
                SortFtpFolders(files, folderHolder1, folderHolder2, client);
                SortFtpFolders(files, folderHolder2, folderHolder1, client);
                if (folderHolder1.Count == 0 && folderHolder2.Count == 0)
                {
                    hasFolders = false;
                }
            }
        }

        private static void SortFtpFolders(List<string> files, List<string> sourceFolder,
            List<string> targetFolder, FtpClient client)
        {
            //---Itterates through a folderholder throwing folders in the other holder and files in the files list---
            foreach (string file in sourceFolder)
            {
                FtpListItem[] items = client.GetListing(file);
                SortFtpItems(files, targetFolder, items);
            }
            //clears folderholder after being ran through to keep from repeating 
            sourceFolder = new List<string>();
        }

        private static List<string> FilterImagesFromItems(List<string> files)
        {
            //---Checks every item in files list and filters out anything that isn't an image---
            List<string> images = new List<string>();
            foreach (string file in files)
            {
                if (Path.GetExtension(file) == ".jpg" || Path.GetExtension(file) == ".png" ||
                    Path.GetExtension(file) == ".jpeg")
                {
                    images.Add(file);
                }
            }
            return images;
        }

    }
}
