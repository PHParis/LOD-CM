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
using VDS.RDF.Ontology;

namespace LOD_CM_CLI
{
    class Program
    {
        // const string WIKI_TYPE = "http://www.wikidata.org/prop/direct/P31";
        // const string RDF_TYPE = "http://www.w3.org/1999/02/22-rdf-syntax-ns#type";

        static async Task Main(string[] args)
        {

            var sw = Stopwatch.StartNew();
            using (var ds = await Dataset.Create(
                    Path.Combine(@"E:\download", "dataset.hdt"),
                    Path.Combine(@"C:\dev\dotnet\LOD-CM\LOD-CM-CLI\examples", "dbpedia_2016-10.nt"))
                .LoadHdt())//.LoadHdt() await
            {
                ds.Label = "DBpedia";
                ds.PropertyType = OntologyHelper.PropertyType;
                ds.OntologyNameSpace = "http://dbpedia.org/ontology/";
                await ComputeFpMfpImage(ds, @"E:\download");
            }
            sw.Stop();
            Console.WriteLine(ToPrettyFormat(sw.Elapsed));
        }

        public static async Task ComputeFpMfpImage(Dataset dataset, string mainDirectory)
        {
            Console.WriteLine("Getting classes...");
            var classes = await dataset.GetInstanceClasses();
            var total = classes.Count();
            Console.WriteLine($"# classes: {total}");
            Console.WriteLine("Looping on classes...");
            var count = 1;
            // TODO: parellize this loop
            foreach (var instanceClass in new[]{new InstanceClass("http://dbpedia.org/ontology/Film")})//classes)
            {
                Console.WriteLine($"class: {instanceClass.Label} ({count++}/{total})");
                var transactions = await Transaction.GetTransactions(dataset, instanceClass);

                long baseId = 0;
                var transactionPatternDiscovery = transactions.transactions.Select(x =>
                    new PatternDiscovery.Transaction<int>(x.ToArray())
                    {
                        ID = baseId++
                    }
                ).ToList();

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

                    var fp = new FrequentPattern<int>();
                    fp.GetFrequentPatternV2(transactionPatternDiscovery, threshold, transactions.domain);
                    // previousFP.GetMFPV2()
                    // TODO: compute MFP here. We don't use FP anymore

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

                        // var ig = new ImageGenerator(dataset);
                        var ig = await ImageGenerator.GenerateTxtForUml(dataset,
                            instanceClass, threshold, fp.fis, transactions);

                        await ig.GetImageContent();
                        await ig.SaveContentForPlantUML(Path.Combine(imageFilePath, "plant.txt"));
                        await ig.SaveImage(Path.Combine(imageFilePath, "img.svg"));
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
    }
}

#region old

// StringBuilder contentBis;
// using (var ds = await Dataset.Create(Path.Combine(
//         @"E:\download", "dataset.hdt"
//     )).LoadHdt())
// {            
//     var HashmapItemFilePath = Path.Combine(@"C:\Users\PH\Downloads",
//         "itemHashmap.txt");
//     var fpPath = Path.Combine(@"C:\Users\PH\Downloads",
//     "fpgrowth_60.txt"); // fpgrowth_60 schema_minsup60
//     var HashmapItemLines = await File.ReadAllLinesAsync(HashmapItemFilePath);
//     var HashmapItem = HashmapItemLines.Where(x => x.Contains(" => ")).Select(x =>
//     {
//         var table = x.Split(" => ", StringSplitOptions.RemoveEmptyEntries);
//         return new
//         {
//             id = Convert.ToInt32(table[0]),
//             uri = table[1]
//         };
//     }).ToDictionary(x => x.id, x => x.uri);

//     var mfpsLines = await File.ReadAllLinesAsync(fpPath);
//     var ci = new CultureInfo("en-US");
//     var sep = " #SUP: "; // "("
//     var mfps = mfpsLines.Where(x => x.Contains(sep)).Select(x =>
//     {
//         var table = x.Split(sep, StringSplitOptions.RemoveEmptyEntries);
//         var props = table[0].Split(" ", StringSplitOptions.RemoveEmptyEntries);
//         return new Tuple<HashSet<int>, double>
//         (
//             props.Select(y => Convert.ToInt32(y)).ToHashSet(),
//             Convert.ToDouble(table[1].Replace(")", ""), ci)
//         );
//     }).ToList();

//     var ig = new ImageGenerator(ds);
//     contentBis = await ig.GenerateTxtForUml("Film", 0.6, 111938, null, HashmapItem, mfps);
//     // Console.WriteLine(contentBis.ToString());
// }

// var content =
//     @"@startuml
//     skinparam linetype ortho
//     Work -- Actor : starring sup:82
//     Work -- Person : writer sup:63
//     Film -- Person : director sup:82
//     class Film{
//     name sup=98
//     label sup=99
//     runtime:XMLSchema#double sup=73
//     type sup=100
//     }
//     Thing <|-- Agent
//     Artist <|-- Actor
//     Agent <|-- Person
//     Thing <|-- Work
//     Person <|-- Artist
//     Work <|-- Film
//     @enduml
// ";
// Console.WriteLine(content);
// content = contentBis.ToString();
// Console.WriteLine(content);
// var uri = PlantUMLUrl.SVG(content);
// using (var client = new HttpClient())
// {
//     var svgFile = await client.GetStringAsync(uri);
//     await File.WriteAllTextAsync(Path.Combine(@"C:\Users\PH\Downloads",
//     $"test_{DateTime.Now.Ticks}.svg"), svgFile);
//     Console.WriteLine(svgFile);
// }

// var sw = new Stopwatch();
// var transactionFilePath = Path.Combine(@"C:\Users\PH\Downloads",
//     "transactions.txt");
// Console.WriteLine("Reading file...");
// var transactionsLines = await File.ReadAllLinesAsync(transactionFilePath);

// Console.WriteLine("Preparing data");
// // var dataset = transactionsLines.Select(x => 
// //     x.Split(" ", StringSplitOptions.RemoveEmptyEntries)).ToArray();
// long baseId = 1;
// var dataset = transactionsLines.Select(x =>
//     new PatternDiscovery.Transaction<string>(
//         x.Split(" ", StringSplitOptions.RemoveEmptyEntries))
//     {
//         ID = baseId++
//     }
// ).ToList();
// var domain = transactionsLines.SelectMany(x =>
//     x.Split(" ", StringSplitOptions.RemoveEmptyEntries))
//     .Distinct()
//     .OrderBy(x => x)
//     .ToList();
// // string[][] dataset =
// // {
// //     new string[] { "1", "2", "5" },
// //     new string[] { "2", "4" },
// //     new string[] { "2", "3" },
// //     new string[] { "1", "2", "4" },
// //     new string[] { "1", "3" },
// //     new string[] { "2", "3" },
// //     new string[] { "1", "3" },
// //     new string[] { "1", "2", "3", "5" },
// //     new string[] { "1", "2", "3" },
// // };
// sw.Start();
// var fp = new FrequentPattern<string>();
// // fp.GetFrequentPattern(dataset, 0.6);
// fp.GetFrequentPatternV2(dataset, 0.6, domain);
// sw.Stop();
// Console.WriteLine(sw.Elapsed);

// string hdtFilePath = Path.Combine(@"C:\dev\dotnet\DotnetHDT\hdt", "67aeedb1a6d3c25f250ed4b2ce0ca50ban.hdt");
// HashSet<string> types;
// using (var hdt = HDTManager.LoadHDT(hdtFilePath))
// {
//     types = hdt.search("", RDF_TYPE, "")
//         .Select(x => x.getObject()).ToHashSet();
// }
// Console.WriteLine($"# types: {types.Count}");
#endregion
