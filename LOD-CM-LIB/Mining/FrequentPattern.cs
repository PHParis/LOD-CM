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
        /// <summary>
        /// Frequent patterns
        /// </summary>
        AssociationRule<T>[] rulesFP;

        public ItemSets<T> fis { get; private set; }

        public void GetFrequentPattern(T[][] transactions, double threshold)
        {
            if (threshold > 1)
                throw new ArgumentException("Threshold must be between 0 and 1");
            // t = s / tlength;
            // t * tlengh = s;
            var support = (int)Math.Floor(threshold * transactions.Length);
            Console.WriteLine($"support: {support}");
            Console.WriteLine("Mining...");
            // Create a new A-priori learning algorithm with the requirements
            var apriori = new Accord.MachineLearning.Rules.Apriori<T>(threshold: support, confidence: 0.7);
            Console.WriteLine($"# apriori.Frequent: {apriori.Frequent.Count}");
            // Use apriori to generate a n-itemset generation frequent pattern
            var classifier = apriori.Learn(transactions);
            Console.WriteLine($"# apriori.Frequent: {apriori.Frequent.Count}");

            // Generate association rules from the itemsets:
            rulesFP = classifier.Rules;
            foreach (var rule in rulesFP)
            {
                // 1 5 6 7 #SUP: 67755
                Console.WriteLine(string.Join(" ", rule.X) + " #SUP: " + rule.Support);
            }
            // Console.WriteLine("Maximum");
            // foreach (var rule in GetMFP(rulesFP))
            // {
            //     Console.WriteLine(rule);
            // }
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
            // var oldFP = set.Select(x =>
            // {
            //     var currentSetOfSet = currentFP.fis.Select(z => z.ToHashSet()).ToHashSet();
            //     return setOfSet.All(y => currentSetOfSet.Any(z => z.SetEquals(y))) ?
            //         currentFP : null;
            // }).FirstOrDefault();
            // // var res = set.Any(x =>
            // // {
            // //     var currentSetOfSet = currentFP.fis.Select(z => z.ToHashSet()).ToHashSet();
            // //     return setOfSet.All(y => currentSetOfSet.Any(z => z.SetEquals(y)));
            // // });
            var oldFP = set.FirstOrDefault(x =>
            {
                var currentSetOfSet = currentFP.fis.Select(z => z.ToHashSet()).ToHashSet();
                return setOfSet.All(y => currentSetOfSet.Any(z => z.SetEquals(y)));
            });
            return (oldFP != null, oldFP);  
        }

        public async Task SaveFP(string fpFilePath)
        {
            await File.WriteAllLinesAsync(fpFilePath,
                fis.Select(x => string.Join(" ", x) + " #SUP: " + x.TransactionCount)
            );
        }

        public double minSupport { get; private set; }

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
        /// Return all Maximum Frequent Pattern from a Frequent Pattern list.
        /// </summary>
        /// <param name="rules"></param>
        /// <returns></returns>
        public IEnumerable<AssociationRule<T>> GetMFP(
            IEnumerable<AssociationRule<T>> rules)
        {
            foreach (var rule in rules)
            {
                if (IsMFP(rule, rules))
                    yield return rule;
            }
        }
        public IEnumerable<HashSet<T>> GetMFPV2(
            HashSet<HashSet<T>> fis)
        {
            foreach (var rule in fis)
            {
                if (IsMFPV2(rule, fis))
                    yield return rule;
            }
        }

        /// <summary>
        /// Return true if the rule is a Maximum Frequent Pattern
        /// </summary>
        /// <param name="rule"></param>
        /// <param name="rules"></param>
        /// <returns></returns>
        public bool IsMFP(AssociationRule<T> rule,
            IEnumerable<AssociationRule<T>> rules)
        {
            return !rules.Select(x => x.X).Any(x => x.IsProperSupersetOf(rule.X));
        }
        public bool IsMFPV2(HashSet<T> rule,
            HashSet<HashSet<T>> fis)
        {
            return !fis.Any(x => x.IsProperSupersetOf(rule));
            // throw new NotImplementedException();
        }

    }
}