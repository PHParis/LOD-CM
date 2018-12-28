using System;
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
        public List<T> domain { get; private set; }

        public InstanceLabel instanceClass { get; set; }

        public Dictionary<string, T> predicateToIntDict { get; private set; }
        public Dictionary<T, string> intToPredicateDict { get; private set; }
        public Dataset dataset { get; private set; }

        private TransactionList() { }

        public List<Transaction<T>> transactions { get; private set; }


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
                transactions.Select(x => string.Join(" ", x)));

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
            result.predicateToIntDict = new Dictionary<string, T>();
            result.intToPredicateDict = new Dictionary<T, string>();
            result.transactions = new List<Transaction<T>>();
            result.dataset = dataset;
            foreach (var instance in instances)
            {
                var predicates = await dataset.GetPredicates(instance);
                // transaction of the given instance
                var currentTransaction = new HashSet<T>();
                foreach (var predicate in predicates)
                {
                    T predicateId;
                    if (result.predicateToIntDict.ContainsKey(predicate))
                    {
                        predicateId = result.predicateToIntDict[predicate];
                    }
                    else
                    {
                        // FIXME: following line works only if T is of a number type... Thus, it's not real generic class...
                        predicateId = (T)Convert.ChangeType(result.predicateToIntDict.Count + 1, typeof(T));
                        result.predicateToIntDict[predicate] = predicateId;
                        result.intToPredicateDict[predicateId] = predicate;
                    }
                    currentTransaction.Add(predicateId);
                }
                result.transactions.Add(new Transaction<T>(currentTransaction.ToArray()));
            }
            result.domain = result.intToPredicateDict.Select(x => x.Key).ToList();
            return result;
        }



    }
}