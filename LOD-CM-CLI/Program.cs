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
                        await ComputeFpMfpImage(ds, conf);
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

        public static async Task ComputeFpMfpImage(Dataset dataset, Conf conf)
        {
            string mainDirectory = conf.mainDir;
            bool precomputationOnly = conf.precomputationOnly;
            string plantUmlJarPath = conf.plantUmlJarPath;
            string localGraphvizDotPath = conf.LocalGraphvizDotPath;
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
                dataset.Precomputation(conf.getPropertiesFromOntology, conf.classesToCompute);
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
#if DEBUG
            File.Delete(classesProcessedPath);
#endif
            var classesProcessed = new ConcurrentBag<string>();
            if (File.Exists(classesProcessedPath))
            {
                var lines = await File.ReadAllLinesAsync(classesProcessedPath);
                foreach (var classProcessed in lines)
                {
                    classesProcessed.Add(classProcessed);
                }
                log.LogInformation($"# classes processed: {classesProcessed.Count}");
                classes = classes.Where(x => !classesProcessed.Contains(x.Uri)).ToList();
                total = classes.Count();
                log.LogInformation($"new # of classes: {total}");
            }
            log.LogInformation("Looping on classes...");
            var count = 0;
            // var objLock = new Object();
            // this will be used after the parallel loop to re-download 
            // images when a problems occurred
            // var failedContentForUmlPath = new ConcurrentBag<string>();
            // Parallel.ForEach(classes, instanceClass =>

            var listOfTrans = new List<(TransactionList<int>, InstanceLabel)>();
            
            log.LogInformation("Transactions computation...");
            foreach (var instanceClass in classes)
            {
                // Interlocked.Increment(ref count);
                count++;
                log.LogInformation($"class: {instanceClass.Label} ({count}/{total})");

                var instancePath = Path.Combine(
                    mainDirectory,
                    dataset.Label,
                    instanceClass.Label
                );
                Directory.CreateDirectory(instancePath);
                var transactionsFilePath = Path.Combine(instancePath, "transactions.json");
                var notransactionsFilePath = Path.Combine(instancePath, "NO_TRANSACTIONS.txt"); // used to avoid computing again transactions when there is none to compute!
                if (File.Exists(transactionsFilePath))
                {
                    // fp has already been computed
                    var jsonContent = File.ReadAllText(transactionsFilePath);
                    var transactions = JsonConvert.DeserializeObject<TransactionList<int>>(jsonContent);
                    listOfTrans.Add((transactions, instanceClass));
                }
                else if (File.Exists(notransactionsFilePath))
                {
                    log.LogTrace($"There is no transactions for class {instanceClass.Label}, there is no need to continue.");
                    continue;//return;
                }
                else
                {
                    log.LogDebug($"computation of transactions...");
                    var transactions = TransactionList<int>.GetTransactions(dataset, instanceClass).Result;
                    log.LogDebug($"transactions computed: {transactions.transactions.Count}");
                    if (!transactions.transactions.Any())
                    {
                        log.LogTrace($"There is no transactions for class {instanceClass.Label}, there is no need to continue.");
                        File.WriteAllTextAsync(notransactionsFilePath, "").Wait();
                        continue;//return;
                    }
                    // ex: ${workingdirectory}/DBpedia/Film

                    transactions.SaveToFiles(
                        Path.Combine(instancePath, "transactions.txt"),
                        Path.Combine(instancePath, "dictionary.txt")
                    ).Wait();
                    listOfTrans.Add((transactions, instanceClass));
                    var jsonFP = JsonConvert.SerializeObject(transactions);
                    File.WriteAllTextAsync(transactionsFilePath, jsonFP).Wait();
                }

            }

            var maxDegreeOfParallelism = 70;
#if DEBUG
            maxDegreeOfParallelism = 1;
#endif
            log.LogInformation("FPs computation...");
            count = 0;
            //foreach (var trans in listOfTrans){
            var fps = listOfTrans.OrderBy(x => x.Item1.transactions.Count).AsParallel().AsOrdered().WithExecutionMode(ParallelExecutionMode.ForceParallelism).WithDegreeOfParallelism(maxDegreeOfParallelism).Select(tuple =>
            {
                Interlocked.Increment(ref count);
                var transactions = tuple.Item1;
                var instanceClass = tuple.Item2;
                var instancePath = Path.Combine(
                    mainDirectory,
                    dataset.Label,
                    instanceClass.Label
                );
                // Directory.CreateDirectory(instancePath);
                var fpFilePath = Path.Combine(instancePath, "fp.json");
                var fp = new FrequentPattern<int>(serviceProvider);
                if (File.Exists(fpFilePath))
                {
                    log.LogDebug($"FP already computed... {instanceClass.Label}: {count}/{listOfTrans.Count}");
                    // fp has already been computed
                    var jsonContent = File.ReadAllText(fpFilePath);
                    fp = JsonConvert.DeserializeObject<FrequentPattern<int>>(jsonContent);
                    fp.SetServiceProvider(serviceProvider);
                    for (int i = 0; i < fp.fis.Count; i++)
                    {
                        for (int j = 0; j < fp.fis[i].TransactionIDList.Count; j++)
                        {
                            fp.fis[i].Add(fp.fis[i].TransactionIDList[j]);
                        }
                    }
                    fp.transactions = transactions;
                }
                else
                {
                    log.LogDebug($"computation of fp... {instanceClass.Label}: {count}/{listOfTrans.Count}");
                    fp.GetFrequentPatternV2(transactions, 0.01);
                    fp.SaveFP(Path.Combine(instancePath, "fp.txt")).Wait();

                    for (int i = 0; i < fp.fis.Count; i++)
                    {
                        for (int j = 0; j < fp.fis[i].Count; j++)
                        {
                            fp.fis[i].TransactionIDList.Add(fp.fis[i][j]);
                        }
                    }
                    fp.transactions = null;
                    var jsonFP = JsonConvert.SerializeObject(fp);
                    File.WriteAllTextAsync(fpFilePath, jsonFP).Wait();
                    fp.transactions = transactions;
                    log.LogDebug($"fp computed {instanceClass.Label}: {count}/{listOfTrans.Count}");
                }
                return (fp, instanceClass);
            }).Where(x => !string.IsNullOrWhiteSpace(x.instanceClass.Uri)).ToList();
            //}
            var thresholdRange = Enumerable.Range(50, 51).OrderByDescending(x => x).AsEnumerable();
            // var maxDegreeOfParallelism = 50;
#if DEBUG
            thresholdRange = new[] { 95 };
            // maxDegreeOfParallelism = 1;
#endif
            // ThreadPool.SetMinThreads(maxDegreeOfParallelism, maxDegreeOfParallelism);
            log.LogInformation("MFP and images computation...");
            count = 1;
            foreach (var (fp, instanceClass) in fps)
            {                
                log.LogInformation($"MFP and images computation {count++}/{fps.Count}");
                foreach (var thresholdInt in thresholdRange)
                // Parallel.ForEach(thresholdRange, new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism }, thresholdInt =>
                // thresholdRange.AsParallel().ForAll(thresholdInt =>

                //Parallel.ForEach(thresholdRange, thresholdInt =>
                {
                    log.LogInformation($"class: {instanceClass.Label} // threshold: {thresholdInt})");
                    var threshold = thresholdInt / 100d;
                    var instancePath = Path.Combine(
                        mainDirectory,
                        dataset.Label,
                        instanceClass.Label
                    );
                    var imageFilePath = Path.Combine(
                        instancePath,
                        thresholdInt.ToString());
                    Directory.CreateDirectory(imageFilePath);

                    var sw = Stopwatch.StartNew();
                    var mfps = fp.ComputeMFP(threshold).ToList();
                    fp.SaveMFP(Path.Combine(imageFilePath, "mfp.txt"), mfps).Wait();
                    sw.Stop();
                    log.LogInformation($"{mfps.Count} mfps computed in ({thresholdInt}): {sw.Elapsed.ToPrettyFormat()}");
                    sw.Restart();
                    var igs = ImageGenerator.GenerateTxtForUml(dataset,
                        instanceClass, threshold, fp, serviceProvider,
                        plantUmlJarPath, localGraphvizDotPath, mfps);


                    // );

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
                    }
                    log.LogInformation($"images computed in ({thresholdInt}): {sw.Elapsed.ToPrettyFormat()}");

                    classesProcessed.Add(instanceClass.Uri);
                    // lock (objLock)
                    // {
                    log.LogInformation("saving processed classes...");
                    File.WriteAllLinesAsync(classesProcessedPath, classesProcessed).Wait();
                    log.LogInformation("processed classes saved");
                }
                // }
            }
            // );
            log.LogInformation("main loop ended!");
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