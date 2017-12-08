using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CefSharp.MinimalExample.WinForms
{

    class Resources
    {
        public static string GetFilePath(string ext)
        {
            switch (ext)
            {
                case "mp4": return Path.GetFullPath(@"Resources\elephants-dream.mp4");
                case "webm": return Path.GetFullPath(@"Resources\elephants-dream.webm");
                    //  case "webm": return Path.GetFullPath(@"Resources\big-buck-bunny_trailer.webm");
            }
            return null;
        }
    }
    public class ProxyResourceHandler : ResourceHandler
    {
        public override bool ProcessRequestAsync(IRequest request, ICallback callback)
        {
            Task.Run(() =>
            {
                using (callback)
                {
                    var extension = Path.GetExtension(request.Url).TrimStart('.').ToLower();

                    var filePath = Resources.GetFilePath(extension);

                    Stream = File.OpenRead(filePath);
                    StatusCode = (int)HttpStatusCode.OK;
                    StatusText = "OK";
                    MimeType = "video/" + extension;
                    ResponseLength = Stream.Length;

                    callback.Continue();
                }
            });

            return true;
        }
    }

    public class ProxyPartialRepsonseResourceHandler : ResourceHandler
    {
        // https://developer.mozilla.org/en-US/docs/Web/HTTP/Configuring_servers_for_Ogg_media

        public override bool ProcessRequestAsync(IRequest request, ICallback callback)
        {
            Task.Run(() =>
            {
                using (callback)
                {

                    var extension = Path.GetExtension(request.Url).TrimStart('.').ToLower();

                    var filePath = Resources.GetFilePath(extension);
                    var stream = File.OpenRead(filePath);
                    var len = stream.Length;
                    var startI = 0L;

                    Headers.Add("accept-ranges", "bytes");
                    Headers.Add("connection", "keep-alive");

                    var range = (request.Headers.Get("Range"));

                    if (range != null)
                    {
                        var start = range.Split('=')[1];
                        var m = Regex.Match(start, @"(\d+)-(\d+)?");
                        start = m.Groups[1].Value;
                        var end = len - 1;
                        if (m.Groups[2] != null && !string.IsNullOrWhiteSpace(m.Groups[2].Value))
                        {
                            end = Convert.ToInt64(m.Groups[2].Value);
                        }

                        startI = Convert.ToInt64(start);
                        var length = len - startI;
                        Headers.Add("content-range", "bytes " + start + "-" + end + "/" + len);
                        Headers.Add("content-length", length.ToString(System.Globalization.CultureInfo.InvariantCulture));

                        stream.Seek(startI, SeekOrigin.Begin);
                        len = length;
                    }

                    StatusCode = (int)HttpStatusCode.PartialContent;
                    StatusText = "Partial Content";
                    MimeType = "video/" + extension;
                    ResponseLength = len;

                    callback.Continue();
                }
            });

            return true;
        }
    }

    public class CustomResourceHandlerFactory : IResourceHandlerFactory
    {
        bool IResourceHandlerFactory.HasHandlers
        {
            get { return true; }
        }

        IResourceHandler IResourceHandlerFactory.GetResourceHandler(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request)
        {
            System.Diagnostics.Debug.WriteLine(request.Url);
            if (request.Url.StartsWith("http://proxy.range")) return new ProxyPartialRepsonseResourceHandler();
            else if (request.Url.StartsWith("http://proxy")) return new ProxyResourceHandler();
            return null;
        }
    }



}

