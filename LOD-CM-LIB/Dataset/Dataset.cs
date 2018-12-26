using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HDTDotnet;
using VDS.RDF;
using VDS.RDF.Ontology;
using VDS.RDF.Parsing;

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
        public bool IsOpen { get; private set; }

        /// <summary>
        /// Used to search type of an instance within data (not in ontology).
        /// </summary>
        /// <value></value>
        /// <example>With DBpedia its is rdf:type, with Wikidata its is P31</example>
        public string PropertyType { get; set; }
        /// <summary>
        /// For Dbpedia: http://dbpedia.org/ontology/
        /// </summary>
        /// <value></value>
        public string OntologyNameSpace { get; set; }

        private HDT hdt;
        public string hdtFilePath { get; set; }
        public string ontologyFilePath { get; set; }
        public bool IsOntologyAvailable { get; private set; }

        public string Label { get; set; }

        private IGraph _ontology;
        public IGraph ontology
        {
            get
            {
                if (_ontology == null)
                {
                    _ontology = new Graph();
                    FileLoader.Load(_ontology, Path.Combine(ontologyFilePath));
                }
                return _ontology;
            }
        }

        private Dataset(string hdtFilePath, string ontologyFilePath)
        {
            IsOpen = false;
            this.hdtFilePath = hdtFilePath;
            if (!File.Exists(hdtFilePath))
                throw new FileNotFoundException("You must provide an existing HDT file path!", hdtFilePath);
            this.ontologyFilePath = ontologyFilePath;
            // we just check if an ontology file is provided
            this.IsOntologyAvailable = ontologyFilePath != null &&
                File.Exists(ontologyFilePath);
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

        public static Dataset Create(string hdtFilePath, string ontologyFilePath)
        {
            return new Dataset(hdtFilePath, ontologyFilePath);
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
            // check if we have an ontology. If yes use it ! If not, use HDT
            if (IsOntologyAvailable)
            {
                var rdfType = ontology.GetUriNode(new Uri(OntologyHelper.PropertyType));
                var owlClass = ontology.GetUriNode(new Uri(OntologyHelper.OwlClass));
                return ontology.GetTriplesWithPredicateObject(rdfType, owlClass)
                    .Select(x => new InstanceClass(x.Subject.ToString()));
            }
            var classUris = hdt.search("", OntologyHelper.PropertyType, "")
                .Select(x => x.getObject()).ToHashSet();
            return classUris.Select(x => new InstanceClass(x));
        }
    }
}