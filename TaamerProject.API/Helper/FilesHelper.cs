﻿//using System.Web.Hosting;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace TaamerProject.API.Helper
{
    public class FilesHelper
    {

        String DeleteURL = null;
        String DeleteType = null;
        String StorageRoot = null;
        String UrlBase = null;
        String tempPath = null;
        //ex:"~/Files/something/";
        String serverMapPath = null;
        public FilesHelper(String DeleteURL, String DeleteType, String StorageRoot, String UrlBase, String tempPath, String serverMapPath)
        {
            this.DeleteURL = DeleteURL;
            this.DeleteType = DeleteType;
            this.StorageRoot = StorageRoot;
            this.UrlBase = UrlBase;
            this.tempPath = tempPath;
            this.serverMapPath = serverMapPath;
        }


        public String getUrl()
        {
            return this.UrlBase;
        }
        public String getserverMapPath()
        {
            return this.serverMapPath;
        }

        public void DeleteFiles(String pathToDelete)
        {
            string path = Path.Combine(pathToDelete);
            System.Diagnostics.Debug.WriteLine(path);
            if (Directory.Exists(path))
            {
                DirectoryInfo di = new DirectoryInfo(path);
                foreach (FileInfo fi in di.GetFiles())
                {
                    System.IO.File.Delete(fi.FullName);
                    System.Diagnostics.Debug.WriteLine(fi.Name);
                }
                di.Delete(true);
            }
        }

        public String DeleteFile(String file)
        {
            System.Diagnostics.Debug.WriteLine("DeleteFile");
            //    var req = HttpContext.Current;
            System.Diagnostics.Debug.WriteLine(file);
            String fullPath = Path.Combine(StorageRoot, file);
            System.Diagnostics.Debug.WriteLine(fullPath);
            System.Diagnostics.Debug.WriteLine(System.IO.File.Exists(fullPath));
            String thumbPath = "/" + file + "80x80.jpg";
            String partThumb1 = Path.Combine(StorageRoot, "thumbs");
            String partThumb2 = Path.Combine(partThumb1, Path.GetFileNameWithoutExtension(file) + "80x80.jpg");

            System.Diagnostics.Debug.WriteLine(partThumb2);
            System.Diagnostics.Debug.WriteLine(System.IO.File.Exists(partThumb2));
            if (System.IO.File.Exists(fullPath))
            {
                //delete thumb 
                if (System.IO.File.Exists(partThumb2))
                {
                    System.IO.File.Delete(partThumb2);
                }
                System.IO.File.Delete(fullPath);
                String succesMessage = "Ok";
                return succesMessage;
            }
            String failMessage = "Error Delete";
            return failMessage;
        }
        public JsonFiles GetFileList()
        {
            var r = new List<ViewDataUploadFilesResult>();
            String fullPath = Path.Combine(StorageRoot);
            if (Directory.Exists(fullPath))
            {
                DirectoryInfo dir = new DirectoryInfo(fullPath);
                foreach (FileInfo file in dir.GetFiles())
                {
                    int SizeInt = unchecked((int)file.Length);
                    r.Add(UploadResult(file.Name, SizeInt, file.FullName));
                }

            }
            JsonFiles files = new JsonFiles(r);
            return files;
        }

        //public void UploadAndShowResults(HttpContext ContentBase, List<ViewDataUploadFilesResult> resultList)
        //{
        //    //var httpRequest = ContentBase.Request;
        //    //System.Diagnostics.Debug.WriteLine(Directory.Exists(tempPath));
        //    //String fullPath = Path.Combine(StorageRoot);
        //    //Directory.CreateDirectory(fullPath);
        //    //// Create new folder for thumbs
        //    //Directory.CreateDirectory(fullPath + "/thumbs/");
        //    //foreach (String inputTagName in httpRequest.Files)
        //    //{
        //    //    var headers = httpRequest.Headers;
        //    //    var file = httpRequest.Files[inputTagName];
        //    //    System.Diagnostics.Debug.WriteLine(file.FileName);
        //    //    if (string.IsNullOrEmpty(headers["X-File-Name"]))
        //    //    {
        //    //        UploadWholeFile(ContentBase, resultList);
        //    //    }
        //    //    else
        //    //    {
        //    //        UploadPartialFile(headers["X-File-Name"], ContentBase, resultList);
        //    //    }
        //    //}
        //}

        //private void UploadWholeFile(HttpContent requestContext, List<ViewDataUploadFilesResult> statuses)
        //{
        //    //var request = requestContext.Request;
        //    //for (int i = 0; i < request.Files.Count; i++)
        //    //{
        //    //    var file = request.Files[i];
        //    //    String pathOnServer = Path.Combine(StorageRoot);
        //    //    var fullPath = Path.Combine(pathOnServer, Path.GetFileName(file.FileName));
        //    //    file.SaveAs(fullPath);
        //    //    //Create thumb
        //    //    string[] imageArray = file.FileName.Split('.');
        //    //    if (imageArray.Length != 0)
        //    //    {
        //    //        String extansion = imageArray[imageArray.Length - 1].ToLower();
        //    //        if (extansion != "jpg" && extansion != "png" && extansion != "jpeg") //Do not create thumb if file is not an image
        //    //        {

        //    //        }
        //    //        else
        //    //        {
        //    //            var ThumbfullPath = Path.Combine(pathOnServer, "thumbs");
        //    //            //String fileThumb = file.FileName + ".80x80.jpg";
        //    //            String fileThumb = Path.GetFileNameWithoutExtension(file.FileName) + "80x80.jpg";
        //    //            var ThumbfullPath2 = Path.Combine(ThumbfullPath, fileThumb);
        //    //            using (MemoryStream stream = new MemoryStream(System.IO.File.ReadAllBytes(fullPath)))
        //    //            {
        //    //                var thumbnail = new System.Web.Helpers.WebImage(stream).Resize(80, 80);
        //    //                thumbnail.Save(ThumbfullPath2, "jpg");
        //    //            }
        //    //        }
        //    //    }
        //    //    statuses.Add(UploadResult(file.FileName, file.ContentLength, file.FileName));
        //    //}
        //}
        //private void UploadPartialFile(string fileName, HttpContent requestContext, List<ViewDataUploadFilesResult> statuses)
        //{
        //    //var request = requestContext.Request;
        //    //if (request.Files.Count != 1) throw new Exception("Attempt to upload chunked file containing more than one fragment per request");
        //    //var file = request.Files[0];
        //    //var inputStream = file.InputStream;
        //    //String patchOnServer = Path.Combine(StorageRoot);
        //    //var fullName = Path.Combine(patchOnServer, Path.GetFileName(file.FileName));
        //    //var ThumbfullPath = Path.Combine(fullName, Path.GetFileName(file.FileName + "80x80.jpg"));
        //    //ImageHandler handler = new ImageHandler();
        //    //var ImageBit = ImageHandler.LoadImage(fullName);
        //    //handler.Save(ImageBit, 80, 80, 10, ThumbfullPath);
        //    //using (var fs = new FileStream(fullName, FileMode.Append, FileAccess.Write))
        //    //{
        //    //    var buffer = new byte[1024];
        //    //    var l = inputStream.Read(buffer, 0, 1024);
        //    //    while (l > 0)
        //    //    {
        //    //        fs.Write(buffer, 0, l);
        //    //        l = inputStream.Read(buffer, 0, 1024);
        //    //    }
        //    //    fs.Flush();
        //    //    fs.Close();
        //    //}
        //    //statuses.Add(UploadResult(file.FileName, file.ContentLength, file.FileName));
        //}


        public void UploadAndShowResults(HttpContext context, List<ViewDataUploadFilesResult> resultList, List<IFormFile> files)
        {
            string fullPath = Path.Combine(StorageRoot);
            Directory.CreateDirectory(fullPath);
            //var thumpa = ;
            string basePath = "Files/ProjectFiles";
            string subdirectory = "thumbs";

            // Combine the base path and subdirectory
            string fullPaths = Path.Combine(basePath, subdirectory);

            // Create the directory if it doesn't exist
            Directory.CreateDirectory(fullPaths);
            foreach (var file in files)
            {
                if (string.IsNullOrEmpty(context.Request.Headers["X-File-Name"]))
                {
                    UploadWholeFile(file, resultList);
                }
                else
                {
                    UploadPartialFile(context.Request.Headers["X-File-Name"], file, resultList);
                }
            }
        }

        private void UploadWholeFile(IFormFile file, List<ViewDataUploadFilesResult> statuses)
        {
            //string pathOnServer = Path.Combine(StorageRoot);
            string pathOnServer = System.IO.Path.Combine("Files/", "ProjectFiles/");
            var fullPath = System.IO.Path.Combine(pathOnServer, Path.GetFileName(file.FileName));
            using (System.IO.FileStream stream = new System.IO.FileStream(System.IO.Path.Combine(pathOnServer, Path.GetFileName(file.FileName)), System.IO.FileMode.Create))
            {
                file.CopyTo(stream);
            }

            //using (var stream = new FileStream(fullPath, FileMode.Create))
            //{
            //    file.CopyTo(stream);
            //}

            // Create thumb
            string[] imageArray = file.FileName.Split('.');
            if (imageArray.Length != 0)
            {
                string extension = imageArray[^1].ToLower();
                if (extension == "jpg" || extension == "png" || extension == "jpeg")
                {
                    var thumbFullPath = Path.Combine(pathOnServer, "thumbs");
                    string fileThumb = Path.GetFileNameWithoutExtension(file.FileName) + "80x80.jpg";
                    var thumbFullPath2 = Path.Combine(thumbFullPath, fileThumb);

                    using (var image = Image.Load(fullPath))
                    {
                        image.Mutate(x => x.Resize(80, 80));
                        image.Save(thumbFullPath2, new JpegEncoder());
                    }
                }
            }
            int SizeInt = unchecked((int)file.Length);
            statuses.Add(UploadResult(file.FileName, SizeInt, file.FileName));
        }

        private void UploadPartialFile(string fileName, IFormFile file, List<ViewDataUploadFilesResult> statuses)
        {
            //string pathOnServer = Path.Combine(StorageRoot);
            string pathOnServer = System.IO.Path.Combine("Files/", "ProjectFiles/");

            var fullPath = Path.Combine(pathOnServer, Path.GetFileName(file.FileName));

            using (var fs = new FileStream(fullPath, FileMode.Append, FileAccess.Write))
            {
                file.CopyTo(fs);
            }
            int SizeInt = unchecked((int)file.Length);

            statuses.Add(UploadResult(file.FileName, SizeInt, file.FileName));
        }


        public ViewDataUploadFilesResult UploadResult(String FileName, int fileSize, String FileFullPath)
        {
            String getType = "";// System.Web.MimeMapping.GetMimeMapping(FileFullPath);
            var result = new ViewDataUploadFilesResult()
            {
                name = FileName,
                size = fileSize,
                type = getType,
                url = UrlBase + "/" + FileName,
                deleteUrl = DeleteURL + FileName,
                thumbnailUrl = CheckThumb(getType, FileName),
                deleteType = DeleteType,
            };
            return result;
        }

        public String CheckThumb(String type, String FileName)
        {
            var splited = type.Split('/');
            if (splited.Length == 2)
            {
                string extansion = splited[1].ToLower();
                if (extansion.Equals("jpeg") || extansion.Equals("jpg") || extansion.Equals("png") || extansion.Equals("gif"))
                {
                    String thumbnailUrl = UrlBase + "thumbs/" + Path.GetFileNameWithoutExtension(FileName) + "80x80.jpg";
                    return thumbnailUrl;
                }
                else
                {
                    if (extansion.Equals("octet-stream")) //Fix for exe files
                    {
                        return "/Content/Free-file-icons/48px/exe.png";

                    }
                    if (extansion.Contains("zip")) //Fix for exe files
                    {
                        return "/Content/Free-file-icons/48px/zip.png";
                    }
                    String thumbnailUrl = "/Content/Free-file-icons/48px/" + extansion + ".png";
                    return thumbnailUrl;
                }
            }
            else
            {
                return UrlBase + "/thumbs/" + Path.GetFileNameWithoutExtension(FileName) + "80x80.jpg";
            }

        }
        public List<String> FilesList()
        {
            List<String> Filess = new List<String>();
            string path = Path.Combine(serverMapPath);
            System.Diagnostics.Debug.WriteLine(path);
            if (Directory.Exists(path))
            {
                DirectoryInfo di = new DirectoryInfo(path);
                foreach (FileInfo fi in di.GetFiles())
                {
                    Filess.Add(fi.Name);
                    System.Diagnostics.Debug.WriteLine(fi.Name);
                }
            }
            return Filess;
        }
    }
    public class ViewDataUploadFilesResult
    {
        public string name { get; set; }
        public int size { get; set; }
        public string type { get; set; }
        public string url { get; set; }
        public string deleteUrl { get; set; }
        public string thumbnailUrl { get; set; }
        public string deleteType { get; set; }
    }
    public class JsonFiles
    {
        public ViewDataUploadFilesResult[] files;
        public string TempFolder { get; set; }
        public JsonFiles(List<ViewDataUploadFilesResult> filesList)
        {
            files = new ViewDataUploadFilesResult[filesList.Count];
            for (int i = 0; i < filesList.Count; i++)
            {
                files[i] = filesList.ElementAt(i);
            }
        }
    }
}
