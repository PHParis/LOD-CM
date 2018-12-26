using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Accord.MachineLearning.Rules;
using PatternDiscovery;
using PatternDiscovery.FrequentPatterns;

namespace LOD_CM_CLI.Mining
{
    /// <summary>
    /// Allow to compute Frequent patterns of a given dataset (https://github.com/cschen1205/cs-pattern-discovery)
    /// </summary>
    public class FrequentPattern<T> where T : System.IComparable<T>
    {
        public double minSupport { get; private set; }
        public ItemSets<T> fis { get; private set; }
        
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

        public PatternDiscovery.ItemSets<T> GetFrequentPatternV2(
            List<PatternDiscovery.Transaction<T>> transactions,
            double minSupport,
            List<T> domain)
        {
            if (minSupport > 1)
                throw new ArgumentException("Threshold must be between 0 and 1");
            this.minSupport = minSupport;
            // t = s / tlength;
            // t * tlengh = s;
            // var support = (int)Math.Floor(threshold * transactions.Count);
            // Console.WriteLine($"support: {support}");
            Console.WriteLine("Mining...");
            // var method = new FPGrowth<T>();
            // var fis = method.MinePatterns(transactions, 
            //     PatternDiscovery.Transaction<T>.ExtractDomain(transactions), minSupport);
            var method = new PatternDiscovery.FrequentPatterns.Apriori<T>();
            fis = method.MinePatterns(transactions, minSupport, domain);
            // var firstFis = fis.First();
            // var firstFisSet = firstFis.ToHashSet(); 
            #region MFP
            // var fisSet = fis.Select(x => x.ToHashSet()).ToHashSet();  
            // for (int i = 0; i < fis.Count; ++i)
            // {
            //     Console.WriteLine(string.Join(" ", fis[i]) + " #SUP: " + fis[i].TransactionCount
            //         + " " + IsMFPV2(fis[i].ToHashSet(), fisSet));
            // }

            // Console.WriteLine("Maximum");
            // foreach (var rule in GetMFPV2(fisSet))
            // {
            //     Console.WriteLine(string.Join(" ", rule));
            // }
            #endregion
            return fis;
        }
                
        /// <summary>
        /// Compute all Maximum Frequent Pattern from a Frequent Pattern list.
        /// </summary>
        /// <param name="rules"></param>
        /// <returns></returns>
        public void ComputeMFP()
        {
            var fisSet = fis.Select(x => x.ToHashSet()).ToHashSet();
            foreach (var itemSet in fis)
            {
                var set = itemSet.ToHashSet();
                itemSet.IsMaximal = IsMFP(set, fisSet);
            }
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