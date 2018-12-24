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

namespace LOD_CM_CLI
{
    class Program
    {
        const string WIKI_TYPE = "http://www.wikidata.org/prop/direct/P31";
        const string RDF_TYPE = "http://www.w3.org/1999/02/22-rdf-syntax-ns#type";

        static async Task Main(string[] args)
        {
            
            var sw = Stopwatch.StartNew();
            using (var ds = await Dataset.Create(Path.Combine(
                    @"E:\download", "dataset.hdt"
                )).LoadHdt())
            {  
                await ComputeFpMfpImage(ds, new InstanceClass
                {
                    Uri = "http://dbpedia.org/ontology/Film",
                    Label = "Film"
                }, 0.51);
            }
            sw.Stop();
            Console.WriteLine(ToPrettyFormat(sw.Elapsed));
        }

        public static async Task ComputeFpMfpImage(Dataset dataset, InstanceClass instanceClass, double threshold)
        {
            var transaction = await Transaction.GetTransactions(dataset, instanceClass);
            
            long baseId = 0;
            var transactionBis = transaction.transactions.Select(x =>
                new PatternDiscovery.Transaction<int>(x.ToArray())
                {
                    ID = baseId++
                }
            ).ToList();

            var fp = new FrequentPattern<int>();
            fp.GetFrequentPatternV2(transactionBis, threshold, transaction.domain);
            var ig = new ImageGenerator(dataset);
            var contentForUml = await ig.GenerateTxtForUml(instanceClass.Label, 
                threshold, transaction.transactions.Count(), fp.fis, 
                transaction.intToPredicateDict, null);
            
            var uri = PlantUMLUrl.SVG(contentForUml.ToString());
            using (var client = new HttpClient())
            {
                var svgFile = await client.GetStringAsync(uri);
                await File.WriteAllTextAsync(Path.Combine(@"C:\Users\PH\Downloads",
                $"test_{DateTime.Now.Ticks}.svg"), svgFile);
                Console.WriteLine(svgFile);
            }
        }
        public static string ToPrettyFormat(TimeSpan span) {

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
