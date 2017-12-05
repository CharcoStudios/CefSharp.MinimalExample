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
        public override bool ProcessRequestAsync(IRequest request, ICallback callback)
        {
            Task.Run(() =>
            {
                using (callback)
                {

                    var acceptRanges = request.Url.StartsWith("proxy.range");

                    var filePath = Path.GetFullPath(@"Resources\big-buck-bunny_trailer.webm");

                    var Streamed = false;

                    if (acceptRanges)
                    {
                        Headers.Add("Accept-Ranges", "bytes");

                        var range = (request.Headers.Get("Range"));

                        if (range != null)
                        {
                            var m = Regex.Match(range, @"bytes=(\d+)-(\d*)?");
                            var offset = long.Parse(m.Groups[1].Value);
                            var length = string.IsNullOrEmpty(m.Groups[2].Value) ? (long?)null : long.Parse(m.Groups[2].Value);

                            if (offset == 0) Stream = File.OpenRead(filePath);
                            else
                            {
                                var cropped = new MemoryStream();
                                using (var f = File.OpenRead(filePath))
                                {
                                    f.Seek(offset, SeekOrigin.Begin);
                                    f.CopyTo(cropped);
                                    cropped.Position = 0;
                                    Stream = cropped;
                                }
                            }
                            // https://developer.mozilla.org/en-US/docs/Web/HTTP/Configuring_servers_for_Ogg_media
                            Headers.Add("Range", "bytes=" + offset + "-");
                            StatusCode = (int)HttpStatusCode.PartialContent;
                            Streamed = true;
                        }                     
                    }

                    if (!Streamed)                    
                    {
                        Stream = File.OpenRead(filePath);
                        StatusCode = (int)HttpStatusCode.OK;
                    }

                    MimeType = "video/" + Path.GetExtension(filePath);
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

