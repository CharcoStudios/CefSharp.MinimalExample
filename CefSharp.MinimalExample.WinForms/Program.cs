// Copyright © 2010-2015 The CefSharp Authors. All rights reserved.
//
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.





namespace CefSharp.MinimalExample.WinForms
{
    using Nancy;
    using Nancy.Hosting.Self;
    using System;
    using System.Globalization;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Windows.Forms;

    public class Program
    {
        [STAThread]
        public static void Main()
        {
            //For Windows 7 and above, best to include relevant app.manifest entries as well
            Cef.EnableHighDPISupport();

            var settings = new CefSettings()
            {
                //By default CefSharp will use an in-memory cache, you need to specify a Cache Folder to persist data
                CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\Cache"),

            };

            settings.RegisterScheme(new CefCustomScheme()
            {
                SchemeName = SchemeHandler.CustomSchemeHandlerFactory.SchemeName,
                SchemeHandlerFactory = new SchemeHandler.CustomSchemeHandlerFactory()
            });

            //Perform dependency check to make sure all relevant resources are in our output directory.
            Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);


            HostConfiguration hostConfigs = new HostConfiguration()
            {
                UrlReservations = new UrlReservations() { CreateAutomatically = true }
            };
            using (var host = new NancyHost(new Uri("http://localhost:9696"), new DefaultNancyBootstrapper(), hostConfigs))
            {
                host.Start();
                var browser = new BrowserForm();
                Application.Run(browser);
            }
        }

    }

    public class SampleModule : Nancy.NancyModule
    {
        public SampleModule()
        {
            Get["/{video}"] = Video;
        }

        private dynamic Video(dynamic parameters)
        {
            var ext = Path.GetExtension(parameters.video).ToLower().TrimStart('.');
            var path = Path.GetFullPath("Resources/elephants-dream."+ext);
            
            return NancyEx.FromPartialFile(Response, Request, path, "video/"+ext);
        }
    }
}

namespace Nancy
{
  
    using System;
    using System.IO;
    using System.Text.RegularExpressions;

    public static class NancyEx
    {
        // http://richardssoftware.net/Home/Post/61

        public static Response FromPartialFile(this IResponseFormatter f, Request req, string path, string contentType)
        {
            return f.FromPartialStream(req, new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read), contentType);
        }

        public static Response FromPartialStream(this IResponseFormatter f, Request req, Stream stream, string contentType)
        {
            // Store the len
            var len = stream.Length;
            // Create the response now
            var res = f.FromStream(stream, contentType)
                        .WithHeader("connection", "keep-alive")
                        .WithHeader("accept-ranges", "bytes");
            // Use the partial status code
            res.StatusCode = HttpStatusCode.PartialContent;
            long startI = 0;
            foreach (var s in req.Headers["Range"])
            {
                var start = s.Split('=')[1];
                var m = Regex.Match(start, @"(\d+)-(\d+)?");
                start = m.Groups[1].Value;
                var end = len - 1;
                if (m.Groups[2] != null && !string.IsNullOrWhiteSpace(m.Groups[2].Value))
                {
                    end = Convert.ToInt64(m.Groups[2].Value);
                }

                startI = Convert.ToInt64(start);
                var length = len - startI;
                res.WithHeader("content-range", "bytes " + start + "-" + end + "/" + len);
                res.WithHeader("content-length", length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            stream.Seek(startI, SeekOrigin.Begin);
            return res;
        }
    }
}
