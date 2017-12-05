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
    public class CustomResourceHandler : ResourceHandler
    {
        string getFilePath(string ext)
        {
            switch (ext)
            {
                case "mp4": return Path.GetFullPath(@"Resources\Coldplay - A Sky Full Of Stars (Official Video).mp4");
                case "webm": return Path.GetFullPath(@"Resources\Coldplay - A Sky Full Of Stars (Official Video).webm");
                    //  case "webm": return Path.GetFullPath(@"Resources\big-buck-bunny_trailer.webm");
            }
            return null;
        }
        public override bool ProcessRequestAsync(IRequest request, ICallback callback)
        {
            Task.Run(() =>
            {
                using (callback)
                {

                    var acceptRanges = request.Url.StartsWith("http://proxy.range");

                    var extension = Path.GetExtension(request.Url).TrimStart('.').ToLower();

                    var filePath = getFilePath(extension);

                    var Streamed = false;

                    if (acceptRanges)
                    {  
                        Headers.Add("Accept-Ranges", "bytes");
                        Headers.Add("Date", DateTime.Now.ToUniversalTime().ToString("r"));
                        //Headers.Add("Cache-Control", "max-age=2592000");
                        //Headers.Add("Connection", "Keep-Alive");
                        //Headers.Add("Keep-Alive","timeout=5,max=100");

                        var range = (request.Headers.Get("Range"));

                        if (range != null)
                        {
                            var m = Regex.Match(range, @"bytes=(\d+)-(\d*)?");
                            var offset = long.Parse(m.Groups[1].Value);
                            var length = string.IsNullOrEmpty(m.Groups[2].Value) ? (long?)null : long.Parse(m.Groups[2].Value);

                            if (offset == 0)
                            {
                                var stream = File.OpenRead(filePath); ;
                                Headers.Add("Content-Length", "" + stream.Length);
                                Headers.Add("Content-Range", "bytes=0-"+(stream.Length-1)+"/"+stream.Length);
                                Stream = stream;
                                //StatusCode = (int)HttpStatusCode.OK;
                                StatusCode = (int)HttpStatusCode.PartialContent;
                                StatusText = "Partial Content";
                            }
                            else
                            {
                                var cropped = new MemoryStream();
                                var total = 0L;
                                var partial = 0L;
                                //using (var f = File.OpenRead(filePath))
                                //{
                                //total = f.Length;
                                //f.Seek(offset, SeekOrigin.Begin);
                                //f.CopyTo(cropped);
                                //cropped.Position = 0;                                    
                                //Stream = cropped;
                                //partial = cropped.Length;                                   
                                //}
                                var f = File.OpenRead(filePath);
                                f.Position = offset;
                                total = f.Length;
                                partial = total - offset;

                                Headers.Add("Content-Length", ""+partial);
                                Headers.Add("Content-Range", "bytes=" + offset + "-" + (total - 1) + "/" + total);
                                StatusCode = (int)HttpStatusCode.PartialContent;
                                StatusText = "Partial Content";
                            }
                            // https://developer.mozilla.org/en-US/docs/Web/HTTP/Configuring_servers_for_Ogg_media
                            Streamed = true;
                            //StatusCode = (int)HttpStatusCode.PartialContent;
                            //Headers.Add("Content-Range", "bytes=" + offset + "-" + (Stream.Length - 1) + "/" + Stream.Length);
                        }
                    }

                    if (!Streamed)
                    {
                        Stream = File.OpenRead(filePath);
                        StatusCode = (int)HttpStatusCode.OK;
                        StatusText = "OK";
                    }

                    MimeType = "video/" + extension;
                    ResponseLength = Stream.Length;

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
            if (request.Url.StartsWith("http://proxy"))
                return new CustomResourceHandler();
            return null;
        }
    }



}

