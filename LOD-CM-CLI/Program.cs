using System;
using System.Collections.Concurrent;
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
// using Iternity.PlantUML;
using LOD_CM_CLI.Data;
using LOD_CM_CLI.Mining;
using LOD_CM_CLI.Uml;
using LOD_CM_CLI.Utils;
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
            // TODO: replace SPARQL code everywhere by DotnetRDF API     
            // dotnet publish -r linux-x64 --self-contained -o out -c Release LOD-CM-CLI.csproj
            // dotnet publish -r linux-x64 --self-contained -o out -c Release LOD-CM-CLI/LOD-CM-CLI.csproj
            Configuration(args[0]);
            if (!File.Exists(args[1]))
                throw new FileNotFoundException("You must provide a valid configuration file!");
            var confContent = await File.ReadAllTextAsync(args[1]);
            var conf = JsonConvert.DeserializeObject<Conf>(confContent);
            var sw = Stopwatch.StartNew();
            foreach (var dataset in conf.datasets)
            {
                try
                {
                    dataset.SetLogger(serviceProvider);
                    log.LogInformation(dataset.Label);
                    using (var ds = await dataset.LoadHdt())
                    {
                        await ComputeFpMfpImage(ds, conf.mainDir, conf.precomputationOnly, conf.plantUmlJarPath, conf.LocalGraphvizDotPath);
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
            log.LogInformation(sw.Elapsed.ToPrettyFormat());
        }

        public static async Task ComputeFpMfpImage(Dataset dataset, string mainDirectory, bool precomputationOnly, string plantUmlJarPath, string localGraphvizDotPath)
        {
            log.LogInformation("Precomputation...");
            var jsonDatasetPath = Path.Combine(mainDirectory, dataset.Label);
            Directory.CreateDirectory(jsonDatasetPath);
            if (File.Exists(Path.Combine(jsonDatasetPath, "dataset.json")))
            {
                var content = await File.ReadAllTextAsync(Path.Combine(jsonDatasetPath, "dataset.json"));
                var datasetTmp = JsonConvert.DeserializeObject<Dataset>(content);
                dataset.dataTypeProperties = datasetTmp.dataTypeProperties;
                dataset.objectProperties = datasetTmp.objectProperties;
                // dataset.superClassesOfClass = datasetTmp.superClassesOfClass;
                dataset.classesDepths = datasetTmp.classesDepths;
                dataset.classes = datasetTmp.classes;
                dataset.properties = datasetTmp.properties;
            }
            else
            {
                dataset.Precomputation();
                log.LogInformation($"Saving after precomputation: {Path.Combine(jsonDatasetPath, "dataset.json")}");
                var json = JsonConvert.SerializeObject(dataset);
                await File.WriteAllTextAsync(Path.Combine(jsonDatasetPath, "dataset.json"), json);
            }
            if (!dataset.dataTypeProperties.Any() ||
                !dataset.objectProperties.Any() ||
                // !dataset.superClassesOfClass.Any() ||
                !dataset.classesDepths.Any() ||
                !dataset.classes.Any() ||
                !dataset.properties.Any())
                throw new Exception(@"Something went wrong during precomputation.
                    One of the dataset property is empty!");

            if (precomputationOnly)
            {
                log.LogInformation("pre-computation only, the program stop here");
                return;
            }

            log.LogInformation("Getting classes...");
            List<InstanceLabel> classes = dataset.classes.Select(x => x.Value).ToList();
            // var jsonClassListPath = Path.Combine(mainDirectory, dataset.Label, "classes.json");
            // if (File.Exists(jsonClassListPath))
            // {
            //     var content = await File.ReadAllTextAsync(jsonClassListPath);
            //     classes = JsonConvert.DeserializeObject<List<InstanceLabel>>(content);
            // }
            // else
            // {
            //     classes = await dataset.GetInstanceClasses();
            //     var json = JsonConvert.SerializeObject(classes);
            //     await File.WriteAllTextAsync(jsonClassListPath, json);
            // }
#if DEBUG
            classes = classes.Where(x => x.Label == "Film").ToList();
            // classes = classes.Take(1).ToList();
#endif
            var total = classes.Count();
            log.LogInformation($"# of classes: {total}");
            var classesProcessedPath = Path.Combine(mainDirectory, dataset.Label, "classesProcessed.txt");
            var classesProcessed = new ConcurrentBag<string>();
            if (File.Exists(classesProcessedPath))
            {
                var lines = await File.ReadAllLinesAsync(classesProcessedPath);
                foreach (var classProcessed in lines)
                {
                    classesProcessed.Add(classProcessed);
                }
                log.LogInformation($"# classes processed: {classesProcessed.Count}");
                classes = classes.Where(x => !classesProcessed.Contains(x.Uri)).ToList(); total = classes.Count();
                log.LogInformation($"new # of classes: {total}");
            }
            log.LogInformation("Looping on classes...");
            var count = 0;
            // this will be used after the parallel loop to re-download 
            // images when a problems occurred
            // var failedContentForUmlPath = new ConcurrentBag<string>();
            Parallel.ForEach(classes, instanceClass =>
            {
                Interlocked.Increment(ref count);
                log.LogInformation($"class: {instanceClass.Label} ({count}/{total})");

                FrequentPattern<int> fp;
                var instancePath = Path.Combine(
                    mainDirectory,
                    dataset.Label,
                    instanceClass.Label
                );
                Directory.CreateDirectory(instancePath);
                var fpFilePath = Path.Combine(instancePath, "fp.json");
                var notransactionsFilePath = Path.Combine(instancePath, "NO_TRANSACTIONS.txt"); // used to avoid computing again transactions when there is none to compute!
                if (File.Exists(fpFilePath)) 
                {
                    // fp has already been computed
                    var jsonContent = File.ReadAllText(fpFilePath);
                    fp = JsonConvert.DeserializeObject<FrequentPattern<int>>(jsonContent);
                    fp.SetServiceProvider(serviceProvider);
                }
                else if (File.Exists(notransactionsFilePath))
                {
                    log.LogTrace($"There is no transactions for class {instanceClass.Label}, there is no need to continue.");
                    return;
                }
                else
                {
                    var transactions = TransactionList<int>.GetTransactions(dataset, instanceClass).Result;
                    log.LogDebug($"transactions computed: {transactions.transactions.Count}");
                    if (!transactions.transactions.Any())
                    {
                        log.LogTrace($"There is no transactions for class {instanceClass.Label}, there is no need to continue.");
                        File.WriteAllTextAsync(notransactionsFilePath, "").Wait();
                        return;
                    }
                    // ex: ${workingdirectory}/DBpedia/Film

                    transactions.SaveToFiles(
                        Path.Combine(instancePath, "transactions.txt"),
                        Path.Combine(instancePath, "dictionary.txt")
                    ).Wait();                

                    fp = new FrequentPattern<int>(serviceProvider);
                    fp.GetFrequentPatternV2(transactions, 0.01);
                    fp.SaveFP(Path.Combine(instancePath, "fp.txt")).Wait();
                    
                    var jsonFP = JsonConvert.SerializeObject(fp);
                    File.WriteAllTextAsync(fpFilePath, jsonFP).Wait();
                }

                
                //fp.ComputeMFP(); // we must compute MFP for each threshold
                var thresholdRange = Enumerable.Range(1, 100);
#if DEBUG
                thresholdRange = new[] { 82 };
#endif
                foreach (var thresholdInt in thresholdRange)
                {
                    log.LogInformation($"class: {instanceClass.Label} // threshold: {thresholdInt})");
                    var threshold = thresholdInt / 100d;

                    var imageFilePath = Path.Combine(
                        instancePath,
                        thresholdInt.ToString());
                    Directory.CreateDirectory(imageFilePath);
                    var mfps = fp.ComputeMFP(threshold).ToList();
                    fp.SaveMFP(Path.Combine(imageFilePath, "mfp.txt"), mfps).Wait();
                    // fp.SaveDictionary(Path.Combine(imageFilePath, "dict.txt")).Wait();
                    var igs = ImageGenerator.GenerateTxtForUml(dataset,
                        instanceClass, threshold, fp, serviceProvider, 
                        plantUmlJarPath, localGraphvizDotPath, mfps).Result;

                    var counter = 0;
                    foreach (var ig in igs)
                    {
                        counter++;
                        // we save the content sended to PlantUML, thus if
                        // a problem occurs, we will be able to regenerate
                        // images.
                        ig.SaveUsedClassesAndProperties(
                            Path.Combine(imageFilePath, $"usedClasses_{counter}.json"),
                            Path.Combine(imageFilePath, $"usedProperties_{counter}.json")).Wait();
                        ig.SaveContentForPlantUML(Path.Combine(imageFilePath, $"plant_{counter}.txt")).Wait();
                        // var isDownloadOK = ig.GetImageContent().Result;
                        // if (isDownloadOK)
                        // {
                        //     ig.SaveImage(Path.Combine(imageFilePath, $"img_{counter}.svg")).Wait();
                        // }
                        // else
                        // {
                        //     failedContentForUmlPath.Add(Path.Combine(imageFilePath, $"plant_{counter}.txt"));
                        // }
                      }
                }
                classesProcessed.Add(instanceClass.Uri);
            }
            );
            log.LogInformation("main loop ended!");
            log.LogInformation("saving processed classes...");
            await File.WriteAllLinesAsync(classesProcessedPath, classesProcessed);
            // // after the loop we search for images not generated to generate them
            // log.LogInformation($"Re-downloading {failedContentForUmlPath.Count} failed images...");
            // var finalErrors = new List<string>();
            // foreach (var contentPath in failedContentForUmlPath)
            // {
            //     try
            //     {
            //         var contentForUml = await File.ReadAllTextAsync(contentPath);
            //         var svgFileContent = await ImageGenerator.GetImageContent(contentForUml, plantUmlJarPath, localGraphvizDotPath);
            //         var filePath = contentPath.Replace("plant_", "img_").Replace(".txt", ".svg");
            //         await File.WriteAllTextAsync(filePath, svgFileContent);
            //     }
            //     catch (Exception ex)
            //     {
            //         finalErrors.Add(contentPath);
            //         log.LogError($"{ex}");
            //     }
            // }
            // await File.WriteAllLinesAsync(Path.Combine(
            //     mainDirectory,
            //     dataset.Label,
            //     "imagesInError.txt"
            // ), finalErrors);
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