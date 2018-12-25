using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LOD_CM_CLI.Data;

namespace LOD_CM_CLI.Mining
{
    public class Transaction
    {
        public List<int> domain { get; private set; }
        public Dictionary<string, int> predicateToIntDict { get; private set; }
        public Dictionary<int, string> intToPredicateDict { get; private set; }

        private Transaction() {}

        public List<HashSet<int>> transactions { get; private set; }

        /// <summary>
        /// Save transactions and dictionary to given file paths
        /// </summary>
        /// <param name="transactionsFilePath"></param>
        /// <param name="dictionaryFilePath"></param>
        /// <returns></returns>
        public async Task SaveToFiles(
            string transactionsFilePath,
            string dictionaryFilePath)
        {
            await File.WriteAllLinesAsync(transactionsFilePath,
                transactions.Select(x => string.Join(" ", x)));
            
            await File.WriteAllLinesAsync(dictionaryFilePath,
                intToPredicateDict.Select(x => x.Key + " " + x.Value));
        }

        /// <summary>
        /// Get transactions from given dataset about instances of given class.
        /// </summary>
        /// <param name="dataset"></param>
        /// <returns></returns>
        public static async Task<Transaction> GetTransactions(Dataset dataset, InstanceClass instanceClass)
        {
            var result = new Transaction();
            if (!dataset.IsOpen)
                await dataset.LoadHdt();
            var instances = await dataset.GetInstances(instanceClass);
            result.predicateToIntDict = new Dictionary<string, int>();
            result.intToPredicateDict = new Dictionary<int, string>();
            result.transactions = new List<HashSet<int>>();
            foreach (var instance in instances)
            {
                var predicates = await dataset.GetPredicates(instance);
                // transaction of the given instance
                var currentTransaction = new HashSet<int>();
                foreach (var predicate in predicates)
                {
                    int predicateId;
                    if (result.predicateToIntDict.ContainsKey(predicate))
                    {
                        predicateId = result.predicateToIntDict[predicate];
                    }
                    else
                    {
                        predicateId = result.predicateToIntDict.Count + 1;
                        result.predicateToIntDict.Add(predicate, predicateId);
                        result.intToPredicateDict.Add(predicateId, predicate);
                    }
                    currentTransaction.Add(predicateId);
                }
                result.transactions.Add(currentTransaction);
            }
            result.domain = result.intToPredicateDict.Select(x => x.Key).ToList();
            return result;
        }
    }
}