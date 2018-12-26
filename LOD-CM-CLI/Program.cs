﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HDTDotnet;
using Iternity.PlantUML;
using LOD_CM_CLI.Data;
using LOD_CM_CLI.Mining;
using LOD_CM_CLI.Uml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using VDS.RDF.Ontology;

namespace LOD_CM_CLI
{
    class Program
    {
        // const string WIKI_TYPE = "http://www.wikidata.org/prop/direct/P31";
        // const string RDF_TYPE = "http://www.w3.org/1999/02/22-rdf-syntax-ns#type";
        public static ServiceProvider serviceProvider { get; private set; }
        private static ILogger log;
        static async Task Main(string[] args)
        {
            // dotnet publish -r linux-x64 --self-contained -o out -c Release LOD-CM-CLI.csproj
            Configuration(args[0]);
            var sw = Stopwatch.StartNew();
            using (var ds = await Dataset.Create(
                    Path.Combine(@"E:\download", "dataset.hdt"),
                    Path.Combine(@"C:\dev\dotnet\LOD-CM\LOD-CM-CLI\examples", "dbpedia_2016-10.nt"),
                    serviceProvider)
                .LoadHdt())//.LoadHdt() await
            {
                ds.Label = "DBpedia";
                ds.PropertyType = OntologyHelper.PropertyType;
                ds.OntologyNameSpace = "http://dbpedia.org/ontology/";
                await ComputeFpMfpImage(ds, @"E:\download");
            }
            sw.Stop();
            log.LogInformation(ToPrettyFormat(sw.Elapsed));
        }

        public static async Task ComputeFpMfpImage(Dataset dataset, string mainDirectory)
        {
            log.LogInformation("Precomputation...");            
            await dataset.Precomputation();
            log.LogInformation("Getting classes...");
            var classes = await dataset.GetInstanceClasses();
            var total = classes.Count();
            log.LogInformation($"# classes: {total}");
            log.LogInformation("Looping on classes...");
            var count = 1;
            // TODO: parellize this loop
            foreach (var instanceClass in new[]{new InstanceClass("http://dbpedia.org/ontology/Film")})//classes)
            // foreach (var instanceClass in classes)
            {
                log.LogInformation($"class: {instanceClass.Label} ({count++}/{total})");
                var transactions = await TransactionList<int>.GetTransactions(dataset, instanceClass);

                // ex: ${workingdirectory}/DBpedia/Film
                var instancePath = Path.Combine(
                    mainDirectory,
                    dataset.Label,
                    instanceClass.Label
                );
                Directory.CreateDirectory(instancePath);
                await transactions.SaveToFiles(
                    Path.Combine(instancePath, "transactions.txt"),
                    Path.Combine(instancePath, "dictionary.txt")
                );

                var fpSet = new List<FrequentPattern<int>>();

                // TODO: put back enumeration over range
                foreach (var thresholdInt in new[] {80})//Enumerable.Range(1, 100))
                {
                    var threshold = thresholdInt / 100d;

                    var fp = new FrequentPattern<int>(serviceProvider);
                    fp.GetFrequentPatternV2(transactions, threshold);
                    fp.ComputeMFP();

                    var imageFilePath = Path.Combine(
                        instancePath,
                        thresholdInt.ToString());
                    Directory.CreateDirectory(imageFilePath);
                    var (alreadyProcessed, previousFP) = FrequentPattern<int>.Contained(fpSet, fp);
                    if (alreadyProcessed)
                    {
                        // fp are the same than in a previous computation
                        // We don't need to get image, just to copy it!
                        var previousThreshold = previousFP.minSupport;
                        var previousImageFilePath = Path.Combine(
                            instancePath,
                            (Convert.ToInt32(previousThreshold * 100)).ToString());
                        File.Copy(Path.Combine(previousImageFilePath, "fp.txt"), Path.Combine(imageFilePath, "fp.txt"));
                        File.Copy(Path.Combine(previousImageFilePath, "plant.txt"), Path.Combine(imageFilePath, "plant.txt"));
                        File.Copy(Path.Combine(previousImageFilePath, "img.svg"), Path.Combine(imageFilePath, "img.svg"));
                    }
                    else
                    {
                        fpSet.Add(fp);
                        await fp.SaveFP(Path.Combine(imageFilePath, "fp.txt"));

                        var igs = await ImageGenerator.GenerateTxtForUml(dataset,
                            instanceClass, threshold, fp);

                        var counter = 0;
                        foreach (var ig in igs)
                        {
                            await ig.GetImageContent();
                            counter++;
                            await ig.SaveContentForPlantUML(Path.Combine(imageFilePath, $"plant_{counter}.txt"));
                            await ig.SaveImage(Path.Combine(imageFilePath, $"img_{counter}.svg"));
                        }
                    }
                }

            }


        }
        public static string ToPrettyFormat(TimeSpan span)
        {

            if (span == TimeSpan.Zero) return "0 minutes";

            var sb = new StringBuilder();
            if (span.Days > 0)
                sb.AppendFormat("{0} day{1} ", span.Days, span.Days > 1 ? "s" : String.Empty);
            if (span.Hours > 0)
                sb.AppendFormat("{0} hour{1} ", span.Hours, span.Hours > 1 ? "s" : String.Empty);
            if (span.Minutes > 0)
                sb.AppendFormat("{0} minute{1} ", span.Minutes, span.Minutes > 1 ? "s" : String.Empty);
            if (span.Seconds > 0)
                sb.AppendFormat("{0} second{1} ", span.Seconds, span.Seconds > 1 ? "s" : String.Empty);
            if (span.Milliseconds > 0)
                sb.AppendFormat("{0} millisecond{1} ", span.Milliseconds, span.Milliseconds > 1 ? "s" : String.Empty);
            return sb.ToString();

        }
        private static void Configuration(string configurationFilePath)
        {
            if (!File.Exists(configurationFilePath))
                throw new FileNotFoundException("You must provide a valid nlog file!");
            serviceProvider = new ServiceCollection()
                .AddSingleton<ILoggerFactory, LoggerFactory>()
                .AddSingleton(typeof(ILogger<>), typeof(Logger<>))
                .BuildServiceProvider();

            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            //configure NLog
            loggerFactory.AddNLog(new NLogProviderOptions { CaptureMessageTemplates = true, CaptureMessageProperties = true });
            NLog.LogManager.LoadConfiguration(configurationFilePath);


            log = serviceProvider.GetService<ILogger<Program>>();
        }
    }
}