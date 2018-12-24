using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HDTDotnet;
using VDS.RDF.Ontology;

namespace LOD_CM_CLI.Data
{
    /// <summary>
    /// Contains all information about one dataset
    /// </summary>
    public class Dataset : IDisposable
    {
        /// <summary>
        /// If true, then the HDT file has been opened and loaded in memory.
        /// </summary>
        /// <value></value>
        public bool IsOpen {get; private set;}

        private HDT hdt;
        public string hdtFilePath {get;set;}

        private Dataset(string hdtFilePath)
        {
            IsOpen = false;
            this.hdtFilePath = hdtFilePath;
        }
        
        /// <summary>
        /// Get instances of the given class
        /// </summary>
        /// <param name="instanceClass"></param>
        /// <returns></returns>
        public async Task<List<string>> GetInstances(InstanceClass instanceClass)
        {
            if (!IsOpen) await LoadHdt();
            return hdt.search("", OntologyHelper.PropertyType, instanceClass.Uri)
                .Select(x => x.getSubject()).ToList();
        }

        /// <summary>
        /// Returns predicates used by given instance
        /// </summary>
        /// <param name="instanceUri"></param>
        /// <returns></returns>
        public async Task<HashSet<string>> GetPredicates(string instanceUri)
        {
            if (!IsOpen) await LoadHdt();
            return hdt.search(instanceUri, "", "")
                .Select(x => x.getPredicate()).ToHashSet();
        }

        public async Task<HashSet<string>> GetSubjects(string predicate, string obj)
        {            
            if (!IsOpen) await LoadHdt();
            return hdt.search("", predicate, obj)
                .Select(x => x.getSubject()).ToHashSet();
        }

        public async Task<HashSet<string>> GetObjects(string subject, string predicate)
        {
            if (!IsOpen) await LoadHdt();
            return hdt.search(subject, predicate, "")
                .Select(x => x.getObject()).ToHashSet();
        }

        public static Dataset Create(string hdtFilePath) {
            return new Dataset(hdtFilePath);
        }

        /// <summary>
        /// Open the HDT file and load it. This operation might be very time
        /// consuming.
        /// </summary>
        /// <returns></returns>
        public async Task<Dataset> LoadHdt()
        {
            await Task.Run(() => hdt = HDTManager.LoadHDT(hdtFilePath));
            IsOpen = true;
            // hdt = HDTManager.LoadHDT(hdtFilePath)
            return this;
        }
        public void Dispose()
        {
            if (hdt != null)
            {
                hdt.Dispose();
            }
            if (IsOpen)
            {
                IsOpen = false;
            }
        }

        /// <summary>
        /// Return all classes (types)
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<InstanceClass>> GetInstanceClasses()
        {
            if (!IsOpen) await LoadHdt();
            var classUris = hdt.search("", OntologyHelper.PropertyType, "")
                .Select(x => x.getObject()).ToHashSet();
            return classUris.Select(x => new InstanceClass
            {
                Uri = x,
                Label = x.Substring(x.LastIndexOf("/") + 1)
            });
        }
    }
}