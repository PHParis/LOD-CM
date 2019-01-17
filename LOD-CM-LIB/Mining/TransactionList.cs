using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LOD_CM_CLI.Data;
using PatternDiscovery;

namespace LOD_CM_CLI.Mining
{
    public class TransactionList<T> where T : IComparable<T>, IConvertible
    {
        public List<T> domain { get; set; }

        public InstanceLabel instanceClass { get; set; }

        public Dictionary<string, T> predicateToIntDict { get; set; }
        public Dictionary<T, string> intToPredicateDict { get; set; }
        public Dataset dataset { get; set; }

        private TransactionList() { }

        public List<Transaction<T>> transactions { get; set; }


        /// <summary>
        /// Save transactions and dictionary to given file paths
        /// </summary>
        /// <param name="transactionsFilePath"></param>
        /// <param name="dictionaryFilePath"></param>
        /// <returns></returns>
        /// <example>For transactions: 
        /// 2 4 5 6 9 23</example>
        /// <example>For dictionary:
        /// 4 http://dbpedia.org/ontology/director</example>
        public async Task SaveToFiles(
            string transactionsFilePath,
            string dictionaryFilePath)
        {
            await File.WriteAllLinesAsync(transactionsFilePath,
                transactions.Select(x => x.ToString()).Where(x => !string.IsNullOrWhiteSpace(x)));

            await File.WriteAllLinesAsync(dictionaryFilePath,
                intToPredicateDict.Select(x => x.Key + " " + x.Value));
        }

        /// <summary>
        /// Get transactions from given dataset about instances of given class.
        /// </summary>
        /// <param name="dataset"></param>
        /// <returns></returns>
        public static async Task<TransactionList<T>> GetTransactions(Dataset dataset, InstanceLabel instanceClass)
        {
            
            var result = new TransactionList<T>();
            if (!dataset.IsOpen)
                await dataset.LoadHdt();
            var instances = await dataset.GetInstances(instanceClass);
            result.instanceClass = instanceClass;
            var predicateToIntDict = new ConcurrentDictionary<string, T>();
            var intToPredicateDict = new ConcurrentDictionary<T, string>();
            var bag = new ConcurrentBag<Transaction<T>>();
            result.dataset = dataset;
            int instanceNumber = 0;
            // foreach (var instance in instances)
            var degreeOfParallelism = 70;
            #if DEBUG
            degreeOfParallelism = 4;
            #endif
            instances.AsParallel().WithDegreeOfParallelism(degreeOfParallelism).ForAll(instance =>
            {
                instanceNumber++;
                var predicates = dataset.GetPredicates(instance).Result;
                // transaction of the given instance
                var currentTransaction = new HashSet<T>();
                var hasType = false;
                foreach (var predicate in predicates)
                {

                    T predicateId;
                    if (predicateToIntDict.ContainsKey(predicate))
                    {
                        predicateId = predicateToIntDict[predicate];
                        if (predicate.Contains("type"))
                            hasType = true;

                    }
                    else
                    {
                        // FIXME: following line works only if T is of a number type... Thus, it's not real generic class...
                        predicateId = (T)Convert.ChangeType(predicateToIntDict.Count + 1, typeof(T));
                        var addition1 = predicateToIntDict.TryAdd(predicate, predicateId);//[predicate] = predicateId;
                        var addition2 = intToPredicateDict.TryAdd(predicateId, predicate);// [predicateId] = predicate;
                        if (predicate.Contains("type"))
                            hasType = true;
                    }
                    currentTransaction.Add(predicateId);
                }
                if (!hasType)
                {
                    Console.WriteLine(instance + " " + instanceNumber);

                }



                bag.Add(new Transaction<T>(currentTransaction.ToArray()));
            }
            );
            result.predicateToIntDict = predicateToIntDict.ToDictionary(x => x.Key, x => x.Value);
            result.intToPredicateDict = intToPredicateDict.ToDictionary(x => x.Key, x => x.Value);
            result.transactions = bag.ToList();
            result.domain = result.intToPredicateDict.Select(x => x.Key).ToList();
            return result;
        }

    }
}