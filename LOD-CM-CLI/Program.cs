using System;
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
            var configContent = await File.ReadAllLinesAsync(args[1]);
            var dsHdt = configContent[0]; // Path.Combine(@"E:\download", "dataset.hdt")
            var dsOnto = configContent[1];// Path.Combine(@"C:\dev\dotnet\LOD-CM\LOD-CM-CLI\examples", "dbpedia_2016-10.nt")
            var dsLabel = configContent[2]; // DBpedia
            var dsPropertyType = configContent[3]; // OntologyHelper.PropertyType;
            var dsOntologyNameSpace = configContent[4]; // "http://dbpedia.org/ontology/"
            var mainDir = configContent[5]; // @"E:\download"
            var sw = Stopwatch.StartNew();
            using (var ds = await Dataset.Create(dsHdt, dsOnto, serviceProvider)
                .LoadHdt())//.LoadHdt() await
            {
                ds.Label = dsLabel;
                ds.PropertyType = dsPropertyType;//OntologyHelper.PropertyType;
                ds.OntologyNameSpace = dsOntologyNameSpace;//"http://dbpedia.org/ontology/";
                await ComputeFpMfpImage(ds, mainDir);
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
            // foreach (var instanceClass in new[]{new InstanceClass("http://dbpedia.org/ontology/Film")})//classes)
            Parallel.ForEach(classes, instanceClass =>
            // foreach (var instanceClass in classes)
            {
                log.LogInformation($"class: {instanceClass.Label} ({count++}/{total})");
                // var transactions = await TransactionList<int>.GetTransactions(dataset, instanceClass);
                var transactions = TransactionList<int>.GetTransactions(dataset, instanceClass).Result;
                log.LogDebug($"transactions computed: {transactions.transactions.Count}");
                // ex: ${workingdirectory}/DBpedia/Film
                var instancePath = Path.Combine(
                    mainDirectory,
                    dataset.Label,
                    instanceClass.Label
                );
                Directory.CreateDirectory(instancePath);
                // await transactions.SaveToFiles(
                //     Path.Combine(instancePath, "transactions.txt"),
                //     Path.Combine(instancePath, "dictionary.txt")
                // );
                transactions.SaveToFiles(
                    Path.Combine(instancePath, "transactions.txt"),
                    Path.Combine(instancePath, "dictionary.txt")
                ).RunSynchronously();

                var fpSet = new List<FrequentPattern<int>>();

                // TODO: put back enumeration over range
                // foreach (var thresholdInt in new[] {80})//Enumerable.Range(1, 100))
                foreach (var thresholdInt in Enumerable.Range(50, 100))
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
                        // await fp.SaveFP(Path.Combine(imageFilePath, "fp.txt"));
                        fp.SaveFP(Path.Combine(imageFilePath, "fp.txt")).RunSynchronously();

                        // var igs = await ImageGenerator.GenerateTxtForUml(dataset,
                        //     instanceClass, threshold, fp);
                        var igs = ImageGenerator.GenerateTxtForUml(dataset,
                            instanceClass, threshold, fp).Result;

                        var counter = 0;
                        foreach (var ig in igs)
                        {
                            counter++;
                            // await ig.GetImageContent();
                            // await ig.SaveContentForPlantUML(Path.Combine(imageFilePath, $"plant_{counter}.txt"));
                            // await ig.SaveImage(Path.Combine(imageFilePath, $"img_{counter}.svg"));
                            ig.GetImageContent().RunSynchronously();
                            ig.SaveContentForPlantUML(Path.Combine(imageFilePath, $"plant_{counter}.txt")).RunSynchronously();
                            ig.SaveImage(Path.Combine(imageFilePath, $"img_{counter}.svg")).RunSynchronously();
                        }
                    }
                }

            }
            );

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