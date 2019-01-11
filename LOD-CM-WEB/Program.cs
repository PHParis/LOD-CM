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
        public static string plantUmlJarPath { get; private set; }
        public static string localGraphvizDotPath { get; private set; }
        public static void Main(string[] args)
        {
            if (args.Length < 3)
                throw new ArgumentException("You must provide a valid main directory, the PlantUml jar path and the Graphviz Dot path. (in that order)");
            mainDir = args[0];
            plantUmlJarPath = args[1];
            localGraphvizDotPath = args[2];
            if (!Directory.Exists(mainDir))
                throw new DirectoryNotFoundException($"{mainDir} doesn't exist!");
            if (!File.Exists(plantUmlJarPath))
                throw new FileNotFoundException($"{plantUmlJarPath} doesn't exist!");
            if (!File.Exists(localGraphvizDotPath))
                throw new FileNotFoundException($"{localGraphvizDotPath} doesn't exist!");
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
