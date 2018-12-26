using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HDTDotnet;
using VDS.RDF.Query;
using VDS.RDF;
using VDS.RDF.Ontology;
using VDS.RDF.Parsing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LOD_CM_CLI.Data
{
    /// <summary>
    /// Contains all information about one dataset
    /// </summary>
    public class Dataset : IDisposable
    {
        private ILogger log;
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

        private Dataset(string hdtFilePath, string ontologyFilePath, ServiceProvider serviceProvider)
        {
            IsOpen = false;
            this.hdtFilePath = hdtFilePath;
            if (!File.Exists(hdtFilePath))
                throw new FileNotFoundException("You must provide an existing HDT file path!", hdtFilePath);
            this.ontologyFilePath = ontologyFilePath;
            // we just check if an ontology file is provided
            this.IsOntologyAvailable = ontologyFilePath != null &&
                File.Exists(ontologyFilePath);
            objectProperties = new Dictionary<string, (HashSet<string> domains, HashSet<string> ranges, bool dash)>();
            dataTypeProperties = new Dictionary<string, string>();
            // objectPropertiesRange = new Dictionary<string, HashSet<string>>();
            log = serviceProvider.GetService<ILogger<Dataset>>();
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

        public static Dataset Create(string hdtFilePath, string ontologyFilePath, ServiceProvider serviceProvider)
        {
            return new Dataset(hdtFilePath, ontologyFilePath, serviceProvider);
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


        public async Task Precomputation()
        {            
            // TODO: add logger everywhere 
            var properties = ontology.Triples.Where(x => 
                x.Predicate.ToString().Equals(OntologyHelper.PropertyType)
                && (x.Object.ToString().Equals(OntologyHelper.OwlDatatypeProperty) ||
                x.Object.ToString().Equals(OntologyHelper.OwlObjectProperty)||
                x.Object.ToString().Equals(OntologyHelper.RdfProperty)))
                .Select(x => x.Subject.ToString()).Distinct().ToList();
            log.LogInformation($"# properties: {properties.Count}");
            foreach (var property in properties)
            {
                await GetPropertyDomainRangeOrDataType(property);
            }
            log.LogInformation($"classes...");
            superClassesOfClass = new Dictionary<string, HashSet<string>>();
            var classes = ontology.Triples.Where(x => 
                x.Predicate.ToString().Equals(OntologyHelper.PropertyType)
                && (x.Object.ToString().Equals(OntologyHelper.OwlClass) ||
                x.Object.ToString().Equals(OntologyHelper.RdfsClass)))
                .Select(x => x.Subject.ToString()).Distinct().ToList();
            log.LogInformation($"# classes: {classes.Count}");
            foreach (var @class in classes)
            {
                var set = FindSuperClasses(@class, Level.First);
                superClassesOfClass[@class] = set;
            }
            log.LogInformation($"classes done");
        }

        /// <summary>
        /// Return one subclass if level is set to First, and
        /// all super classes if level is set to all.
        /// </summary>
        /// <param name="aClass"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public HashSet<string> FindSuperClasses(string aClass, Level level)
        {
            var multipleLevels = level == Level.All ? "*" : string.Empty;
            var query = @"
                PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#> 
                SELECT * WHERE { 
                    <" + aClass + @"> rdfs:subClassOf" + multipleLevels + @" ?superClass . 
                }";
            var results = (SparqlResultSet)ontology.ExecuteQuery(query);
            var subClasses = new HashSet<string>();
            foreach (var result in results)
            {
                var superclass = result["superClass"].ToString();
                subClasses.Add(superclass);
            }
            return subClasses;
        }

        /// <summary>
        /// The key is the property uri, the value are its domain(s) and range(s) and the dash
        /// </summary>
        /// <value></value>
        public Dictionary<string, (HashSet<string> domains, HashSet<string> ranges, bool dash)> objectProperties{get;private set;}
        // public Dictionary<string, HashSet<string>> objectPropertiesRange {get;private set;}
        /// <summary>
        /// The key is the property uri, the value is its datatype
        /// </summary>
        /// <value></value>
        public Dictionary<string, string> dataTypeProperties {get;private set;}

        public async Task GetPropertyDomainRangeOrDataType(string propertyUri)
        {
            if (string.IsNullOrWhiteSpace(propertyUri)) return;
            var propNode = ontology.GetUriNode(
                new Uri(propertyUri));            
            var rdfType = ontology.GetUriNode(new Uri(OntologyHelper.PropertyType));
            var owlObjectProp = ontology.GetUriNode(new Uri(OntologyHelper.OwlObjectProperty));
            if (ontology.ContainsTriple(
                new Triple(propNode, rdfType, owlObjectProp)))
            {
                // it is an object property
                var domainsAndRanges = await GetPropertyDomainRange(propertyUri);
                objectProperties[propertyUri] = domainsAndRanges;
                // objectPropertiesDomain.Add(propertyUri, domainsAndRanges.domains);
                // objectPropertiesRange.Add(propertyUri, domainsAndRanges.ranges);
            }
            else
            {
                // it is NOT an object property
                var dataType = GetPropertyDataType(propertyUri);
                if (!string.IsNullOrWhiteSpace(dataType))
                    dataTypeProperties[propertyUri] = dataType;
            }
        }

        private async Task<(HashSet<string> domains, HashSet<string> ranges, bool dash)> GetPropertyDomainRange(string propertyUri)
        {
            var domains = ontology.Triples.Where(x =>
                x.Subject.ToString().Equals(propertyUri) &&
                x.Predicate.ToString().Equals(OntologyHelper.PropertyDomain))
                .Select(x => x.Object.ToString())
                .ToHashSet();


            var ranges = ontology.Triples.Where(x =>
                x.Subject.ToString().Equals(propertyUri) &&
                x.Predicate.ToString().Equals(OntologyHelper.PropertyRange))
                .Select(x => x.Object.ToString())
                .ToHashSet();
            var dash = false;
            if (!domains.Any())
            {
                domains.Add(await FindRangeOrDomain(propertyUri, RangeOrDomain.Domain));
                dash = true;
            }
            if (!ranges.Any())
            {
                ranges.Add(await FindRangeOrDomain(propertyUri, RangeOrDomain.Range));
                dash = true;
            }

            return (domains, ranges, dash);
        }

        private string GetPropertyDataType(string dtp)
        {
            return ontology.Triples.Where(x =>
                x.Subject.ToString().Equals(dtp) &&
                x.Predicate.ToString().Equals(OntologyHelper.PropertyRange))
                .Select(x => x.Object.ToString())
                .FirstOrDefault();
        }         


        /// <summary>
        /// If the range is not available within the ontology, we search for the
        /// most used type among instances
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public async Task<string> FindRangeOrDomain(string property, RangeOrDomain rangeOrDomain)
        {
            // get all objects being the range or domain of the given property
            var objectsTmp = rangeOrDomain == RangeOrDomain.Range ?
                await GetObjects("", property) :
                await GetSubjects(property, "");
            // getting all types for each object
            var typesByObject = objectsTmp.AsParallel().Select(obj =>
            {
                var setTmp = GetObjects(obj, PropertyType).Result;
                return new { subject = obj, setTmp };
            }).ToDictionary(x => x.subject, x => x.setTmp);

            var useCounter = 0;
            var typeMap = new Dictionary<string, int>();
            // computation of the most used type
            foreach (var entry in typesByObject)
            {
                var types = entry.Value.Where(x =>
                    x.StartsWith(OntologyNameSpace)).ToHashSet();

                foreach (var type in types)
                {
                    if (typeMap.ContainsKey(type))
                    {
                        var c = typeMap.GetValueOrDefault(type);
                        c++;
                        typeMap[type] = c;
                        if (useCounter < c)
                            useCounter = c;
                    }
                    else
                    {
                        typeMap[type] = 1;
                    }
                }
            }
            // return the most used type
            return typeMap.Where(x => x.Value == useCounter)
                .Select(x => x.Key).FirstOrDefault();
        }
        public enum Level
        {
            First,
            All
        }        
        public enum RangeOrDomain
        {
            Range,
            Domain
        }
        public Dictionary<string, HashSet<string>> superClassesOfClass {get; private set;}

    }
}