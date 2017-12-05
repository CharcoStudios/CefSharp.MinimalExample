using CefSharp;
using System;
using System.IO;
using System.Net;
using System.Web;

namespace CefSharp.SchemeHandler
{
    /// <summary>
    /// FolderSchemeHandlerFactory is a very simple scheme handler that allows you
    /// to map requests for urls to a folder on your file system. For example
    /// creating a setting the rootFolder to c:\projects\CefSharp\CefSharp.Example\Resources
    /// registering the scheme handler
    /// </summary>
    public class CustomSchemeHandlerFactory : ISchemeHandlerFactory
    {
        public CustomSchemeHandlerFactory()
        {
        }

        public static readonly string SchemeName = "proxy";

        IResourceHandler ISchemeHandlerFactory.Create(IBrowser browser, IFrame frame, string schemeName, IRequest request)
        {

            var filePath = Path.GetFullPath( @"Resources\big-buck-bunny_trailer.webm");

            if (File.Exists(filePath))
            {
                var fileExtension = Path.GetExtension(filePath);
                var mimeType = ResourceHandler.GetMimeType(fileExtension);
                return ResourceHandler.FromFilePath(filePath, mimeType);
            }

            var fileNotFoundResourceHandler = ResourceHandler.FromString("File Not Found - " + filePath);
            fileNotFoundResourceHandler.StatusCode = (int)HttpStatusCode.NotFound;

            return fileNotFoundResourceHandler;
        }
    }
}