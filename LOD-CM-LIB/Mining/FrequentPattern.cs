using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PatternDiscovery;
using PatternDiscovery.FrequentPatterns;

namespace LOD_CM_CLI.Mining
{
    /// <summary>
    /// Allow to compute Frequent patterns of a given dataset (https://github.com/cschen1205/cs-pattern-discovery)
    /// </summary>
    public class FrequentPattern<T> where T : System.IComparable<T>, IConvertible
    {
        private ILogger log;
        public TransactionList<T> transactions { get; set; }
        public double minSupport { get; set; }
        public ItemSets<T> fis { get; set; }

        public FrequentPattern()
        {
        }

        public FrequentPattern(ServiceProvider serviceProvider)
        {
            log = serviceProvider.GetService<ILogger<FrequentPattern<T>>>();
        }

        public void SetServiceProvider(ServiceProvider serviceProvider)
        {
            log = serviceProvider.GetService<ILogger<FrequentPattern<T>>>();
        }


        /// <summary>
        /// Return true if the FP is contained in the set (i.e.
        /// if it has already been computed).
        /// </summary>
        /// <param name="set"></param>
        /// <param name="currentFP"></param>
        /// <returns></returns>
        public static (bool, FrequentPattern<T>) Contained(List<FrequentPattern<T>> set, FrequentPattern<T> currentFP)
        {
            if (!set.Any()) return (false, null);
            var setOfSet = currentFP.fis.Select(x => x.ToHashSet()).ToHashSet();
            var oldFP = set.FirstOrDefault(x =>
            {
                var currentSetOfSet = currentFP.fis.Select(z => z.ToHashSet()).ToHashSet();
                return setOfSet.All(y => currentSetOfSet.Any(z => z.SetEquals(y)));
            });
            return (oldFP != null, oldFP);
        }

        /// <summary>
        /// Save FP in given file. The pattern is:
        /// properties #SUP: transactionsCounter
        /// </summary>
        /// <param name="fpFilePath"></param>
        /// <returns></returns>
        /// <example>1 3 5 6 #SUP: 2345</example>
        public async Task SaveFP(string fpFilePath)
        {
            await File.WriteAllLinesAsync(fpFilePath,
                fis.Select(x => string.Join(" ", x) + " #SUP: " + x.TransactionCount)
            );
        }
        public async Task SaveMFP(string fpFilePath, IEnumerable<PatternDiscovery.ItemSet<T>> mfps)
        {
            await File.WriteAllLinesAsync(fpFilePath,
                mfps.Select(x => string.Join(" ", x) + " #SUP: " + x.TransactionCount)
            );
        }

        // public async Task SaveDictionary(string dictionaryFilePath)
        // {
        //     await File.WriteAllLinesAsync(dictionaryFilePath,
        //         transactions.intToPredicateDict.Select(x => $"{x.Key} {x.Value}")
        //     );
        // }

        public PatternDiscovery.ItemSets<T> GetFrequentPatternV2(
            TransactionList<T> transactions, double minSupport)
        {
            if (minSupport > 1)
                throw new ArgumentException("Threshold must be between 0 and 1");
            this.transactions = transactions;
            this.minSupport = minSupport;
            // this.objectPropertiesInfo = new List<(HashSet<string> classes, int propertySupport, bool dash)>();
            var domain = transactions.domain;
            log.LogInformation("Mining...");
            var method = new PatternDiscovery.FrequentPatterns.Apriori<T>();
            fis = method.MinePatterns(transactions.transactions, minSupport, domain);
            #region MFP
            // var fisSet = fis.Select(x => x.ToHashSet()).ToHashSet();  
            // for (int i = 0; i < fis.Count; ++i)
            // {
            //     log.LogInformation(string.Join(" ", fis[i]) + " #SUP: " + fis[i].TransactionCount
            //         + " " + IsMFPV2(fis[i].ToHashSet(), fisSet));
            // }

            // log.LogInformation("Maximum");
            // foreach (var rule in GetMFPV2(fisSet))
            // {
            //     log.LogInformation(string.Join(" ", rule));
            // }
            #endregion
            return fis;
        }

        /// <summary>
        /// Compute all Maximum Frequent Pattern from a Frequent Pattern list.
        /// </summary>
        /// <param name="rules"></param>
        /// <returns></returns>
        public IEnumerable<PatternDiscovery.ItemSet<T>> ComputeMFP(double threshold)
        {
#if DEBUG
            var sw = Stopwatch.StartNew();
#endif
            var fisSet = fis.Where(x => x.Support >= threshold)
                .Select(x => x.ToHashSet()).ToHashSet();
            var degreeOfParallelism = 70;
#if DEBUG
            degreeOfParallelism = 4;
            sw.Stop();
            log.LogInformation($"1 ({threshold}): {sw.Elapsed.ToPrettyFormat()}");
            var e2 = new List<long>();
            var e3 = new List<long>();
            var e4 = new List<long>();
            sw.Restart();
#endif
            // foreach (var itemSet in fis.Where(x => x.Support >= threshold))
            var result = fis.AsParallel().WithDegreeOfParallelism(degreeOfParallelism).Where(x => x.Support >= threshold).Where(itemSet =>
            {
#if DEBUG
                sw.Stop();
                e2.Add(sw.ElapsedMilliseconds);
                sw.Restart();
#endif
                var set = itemSet.ToHashSet();
#if DEBUG
                sw.Stop();
                e3.Add(sw.ElapsedMilliseconds);
                sw.Restart();
#endif
                var isMFP = IsMFP(set, fisSet);
#if DEBUG
                sw.Stop();
                e4.Add(sw.ElapsedMilliseconds);
                sw.Restart();
#endif
                return isMFP;
                // if (isMFP)
                // {
                //     yield return itemSet;
                // }
                // itemSet.IsMaximal = IsMFP(set, fisSet);
            }
            ).ToList();
#if DEBUG
            log.LogInformation($"2 ({threshold}): avg={TimeSpan.FromMilliseconds(e2.Average())} // min={TimeSpan.FromMilliseconds(e2.Min())} // max={TimeSpan.FromMilliseconds(e2.Max())} // ");
            log.LogInformation($"3 ({threshold}): avg={TimeSpan.FromMilliseconds(e3.Average())} // min={TimeSpan.FromMilliseconds(e3.Min())} // max={TimeSpan.FromMilliseconds(e3.Max())} // ");
            log.LogInformation($"4 ({threshold}): avg={TimeSpan.FromMilliseconds(e4.Average())} // min={TimeSpan.FromMilliseconds(e4.Min())} // max={TimeSpan.FromMilliseconds(e4.Max())} // ");
#endif
            return result;
        }

        /// <summary>
        /// Return true if the rule is a Maximum Frequent Pattern
        /// </summary>
        /// <param name="rule"></param>
        /// <param name="rules"></param>
        /// <returns></returns>
        public bool IsMFP(HashSet<T> rule,
            HashSet<HashSet<T>> fis)
        {
            return !fis.Any(x => x.IsProperSupersetOf(rule));
        }

    }
}