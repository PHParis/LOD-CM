using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HDTDotnet;
using Iternity.PlantUML;
using LOD_CM_CLI.Data;
using LOD_CM_CLI.Mining;
using LOD_CM_CLI.Uml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
            var confContent = await File.ReadAllTextAsync(args[1]);
            var conf = JsonConvert.DeserializeObject<Conf>(confContent);
            // var configContent = await File.ReadAllLinesAsync(args[1]);
            // var dsHdt = confContent[0]; // Path.Combine(@"E:\download", "dataset.hdt")
            // var dsOnto = confContent[1];// Path.Combine(@"C:\dev\dotnet\LOD-CM\LOD-CM-CLI\examples", "dbpedia_2016-10.nt")
            // var dsLabel = confContent[2]; // DBpedia
            // var dsPropertyType = confContent[3]; // OntologyHelper.PropertyType;
            // var dsOntologyNameSpace = confContent[4]; // "http://dbpedia.org/ontology/"
            // var mainDir = confContent[5]; // @"E:\download"
            var sw = Stopwatch.StartNew();
            foreach (var dataset in conf.datasets)
            {
                try
                {
                    dataset.SetLogger(serviceProvider);
                    log.LogInformation(dataset.Label);
                    using (var ds = await dataset
                        .LoadHdt())//.LoadHdt() await
                    {
                        // ds.Label = dsLabel;
                        // ds.PropertyType = dsPropertyType;//OntologyHelper.PropertyType;
                        // ds.OntologyNameSpace = dsOntologyNameSpace;//"http://dbpedia.org/ontology/";
                        await ComputeFpMfpImage(ds, conf.mainDir);
                    }
                }
                catch (Exception ex)
                {
                    log.LogError(ex.Message);
                    log.LogError(ex.StackTrace);
                    if (ex.InnerException != null)
                    {
                        log.LogError(ex.InnerException.Message);
                        log.LogError(ex.InnerException.StackTrace);
                    }
                }
            }

            sw.Stop();
            log.LogInformation(ToPrettyFormat(sw.Elapsed));
        }

        public static async Task ComputeFpMfpImage(Dataset dataset, string mainDirectory)
        {
            log.LogInformation("Precomputation...");
            var jsonDatasetPath = Path.Combine(mainDirectory, dataset.Label, "dataset.json");
            if (File.Exists(jsonDatasetPath))
            {
                var content = await File.ReadAllTextAsync(jsonDatasetPath);
                var datasetTmp = JsonConvert.DeserializeObject<Dataset>(content);
                dataset.dataTypeProperties = datasetTmp.dataTypeProperties;
                dataset.objectProperties = datasetTmp.objectProperties;
                dataset.superClassesOfClass = datasetTmp.superClassesOfClass;
                dataset.classesDepths = datasetTmp.classesDepths;
            }
            else
            {
                dataset.Precomputation();
                log.LogInformation($"Saving after precomputation: {jsonDatasetPath}");
                var json = JsonConvert.SerializeObject(dataset);
                await File.WriteAllTextAsync(jsonDatasetPath, json);
            }

            log.LogInformation("Getting classes...");
            List<InstanceClass> classes;
            var jsonClassListPath = Path.Combine(mainDirectory, dataset.Label, "classes.json");
            if (File.Exists(jsonClassListPath))
            {
                var content = await File.ReadAllTextAsync(jsonClassListPath);
                classes = JsonConvert.DeserializeObject<List<InstanceClass>>(content);
            }
            else
            {
                classes = await dataset.GetInstanceClasses();
                var json = JsonConvert.SerializeObject(classes);
                await File.WriteAllTextAsync(jsonClassListPath, json);
            }
#if DEBUG
            classes = classes.Take(1).ToList();
#endif
            var total = classes.Count();
            log.LogInformation($"# classes: {total}");
            log.LogInformation("Looping on classes...");
            var count = 0;
            // foreach (var instanceClass in new[]{new InstanceClass("http://dbpedia.org/ontology/Film")})//classes)
            Parallel.ForEach(classes, instanceClass =>
            // foreach (var instanceClass in classes)
            {
                Interlocked.Increment(ref count);
                log.LogInformation($"class: {instanceClass.Label} ({count}/{total})");
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
                ).Wait();

                // var fpSet = new List<FrequentPattern<int>>();

                // TODO: put back enumeration over range
                // foreach (var thresholdInt in new[] {80})//Enumerable.Range(1, 100))
                foreach (var thresholdInt in Enumerable.Range(1, 100))
                {
                    var threshold = thresholdInt / 100d;

                    var fp = new FrequentPattern<int>(serviceProvider);
                    fp.GetFrequentPatternV2(transactions, threshold);
                    fp.ComputeMFP();

                    var imageFilePath = Path.Combine(
                        instancePath,
                        thresholdInt.ToString());
                    Directory.CreateDirectory(imageFilePath);
                    // var (alreadyProcessed, previousFP) = FrequentPattern<int>.Contained(fpSet, fp);
                    // if (alreadyProcessed)
                    // {
                    //     // fp are the same than in a previous computation
                    //     // We don't need to get image, just to copy it!
                    //     var previousThreshold = previousFP.minSupport;
                    //     var previousImageFilePath = Path.Combine(
                    //         instancePath,
                    //         (Convert.ToInt32(previousThreshold * 100)).ToString());
                    //     File.Copy(Path.Combine(previousImageFilePath, "fp.txt"), Path.Combine(imageFilePath, "fp.txt"));
                    //     File.Copy(Path.Combine(previousImageFilePath, "plant.txt"), Path.Combine(imageFilePath, "plant.txt"));
                    //     File.Copy(Path.Combine(previousImageFilePath, "img.svg"), Path.Combine(imageFilePath, "img.svg"));
                    // }
                    // else
                    // {
                    // fpSet.Add(fp);
                    // await fp.SaveFP(Path.Combine(imageFilePath, "fp.txt"));
                    fp.SaveFP(Path.Combine(imageFilePath, "fp.txt")).Wait();

                    // var igs = await ImageGenerator.GenerateTxtForUml(dataset,
                    //     instanceClass, threshold, fp);
                    var igs = ImageGenerator.GenerateTxtForUml(dataset,
                        instanceClass, threshold, fp, serviceProvider).Result;

                    var counter = 0;
                    foreach (var ig in igs)
                    {
                        counter++;
                        // await ig.GetImageContent();
                        // await ig.SaveContentForPlantUML(Path.Combine(imageFilePath, $"plant_{counter}.txt"));
                        // await ig.SaveImage(Path.Combine(imageFilePath, $"img_{counter}.svg"));
                        var isDownloadOK = ig.GetImageContent().Result;
                        if (isDownloadOK)
                        {
                            ig.SaveContentForPlantUML(Path.Combine(imageFilePath, $"plant_{counter}.txt")).Wait();
                            ig.SaveImage(Path.Combine(imageFilePath, $"img_{counter}.svg")).Wait();
                        }
                    }
                    // }
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