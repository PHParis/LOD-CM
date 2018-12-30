using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LOD_CM
{
    public class Program
    {
        /// <summary>
        /// Main directory containing datasets informations.
        /// </summary>
        /// <value></value>
        public static string mainDir { get; private set; }
        public static void Main(string[] args)
        {
            if (args.Length < 1)
                throw new ArgumentException("You must provide a valid main directory.");
            mainDir = args[0];
            if (!Directory.Exists(mainDir))
                throw new DirectoryNotFoundException($"{mainDir} doesn't exist!");
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
