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
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Threading;

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
        [JsonIgnore]
        public bool IsOpen { get; private set; }

        /// <summary>
        /// Property used to find the label of an instance class or a property.
        /// If null, the fragment will be used
        /// </summary>
        /// <value></value>
        public string propertyForLabel { get; set; }

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
        public bool IsOntologyAvailable { get; set; }

        public string Label { get; set; }

        private IGraph _ontology;
        public string PropertySubClassOf { get; set; }

        public Dictionary<string, InstanceLabel> properties { get; set; }

        public Dictionary<string, InstanceLabel> classes { get; set; }

        [JsonIgnore]
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

        public Dataset() { }

        private Dataset(string hdtFilePath, string ontologyFilePath, ServiceProvider serviceProvider)
        {
            this.hdtFilePath = hdtFilePath;
            if (!File.Exists(hdtFilePath))
                throw new FileNotFoundException("You must provide an existing HDT file path!", hdtFilePath);
            this.ontologyFilePath = ontologyFilePath;
            // we just check if an ontology file is provided
            this.IsOntologyAvailable = ontologyFilePath != null &&
                File.Exists(ontologyFilePath);
            SetLogger(serviceProvider);
        }

        public void SetLogger(ServiceProvider serviceProvider)
        {
            IsOpen = false;
            objectProperties = new Dictionary<string, (string domains, string ranges, bool dash)>();
            dataTypeProperties = new Dictionary<string, string>();
            // objectPropertiesRange = new Dictionary<string, HashSet<string>>();
            log = serviceProvider.GetService<ILogger<Dataset>>();
            // superClassesOfClass = new Dictionary<string, HashSet<string>>();
            classesDepths = new Dictionary<string, Dictionary<string, int>>();
        }

        /// <summary>
        /// Get instances of the given class
        /// </summary>
        /// <param name="instanceClass"></param>
        /// <returns></returns>
        public async Task<List<string>> GetInstances(InstanceLabel instanceClass)
        {
            if (!IsOpen) await LoadHdt();
            return hdt.search("", PropertyType, instanceClass.Uri)
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
        public async Task<List<InstanceLabel>> GetInstanceClasses()
        {
            // check if we have an ontology. If yes use it ! If not, use HDT
            if (IsOntologyAvailable)
            {
                var rdfType = ontology.GetUriNode(new Uri(OntologyHelper.PropertyType));
                var owlClass = ontology.GetUriNode(new Uri(OntologyHelper.OwlClass));
                return ontology.GetTriplesWithPredicateObject(rdfType, owlClass)
                    .Select(x => new InstanceLabel(x.Subject.ToString(), propertyForLabel, this)).ToList();
            }
            if (!IsOpen) await LoadHdt();
            return hdt.search("", PropertyType, "")
                .Select(x => x.getObject()).Distinct()
                .Select(x => new InstanceLabel(x, propertyForLabel, this)).ToList();
            // return classUris.Select(x => new InstanceClass(x)).ToList();
        }

        /// <summary>
        /// The key is a subclass, and the value is composed of its super classes.
        /// For each super class we have the relative distance
        /// </summary>
        /// <value></value>
        public Dictionary<string, Dictionary<string, int>> classesDepths { get; set; }
        public void Precomputation()
        {
            log.LogInformation($"Computing class depth");
            classesDepths = GetClassesDepth();
            log.LogInformation($"class depth done: {classesDepths.Count}");
            log.LogInformation($"classes...");
            // superClassesOfClass = new Dictionary<string, HashSet<string>>();
            classes = ontology.Triples.Where(x =>
                x.Predicate.ToString().Equals(OntologyHelper.PropertyType)
                && (x.Object.ToString().Equals(OntologyHelper.OwlClass) ||
                x.Object.ToString().Equals(OntologyHelper.RdfsClass)))
                .Select(x => x.Subject.ToString()).Distinct()
                .AsParallel()
                .Select(x => new InstanceLabel(x, this.propertyForLabel, this))
                .ToDictionary(x => x.Uri, x => x);
            log.LogInformation($"# classes: {classes.Count}");
            // var superClassesOfClassConcurrent = new ConcurrentDictionary<string, HashSet<string>>();
            // // foreach (var @class in classes)
            // Parallel.ForEach(classes.Values, @class =>
            // {
            //     var set = FindSuperClasses(@class.Uri, Level.First);
            //     superClassesOfClassConcurrent.TryAdd(@class.Uri, set);
            // });
            // superClassesOfClass = superClassesOfClassConcurrent.ToDictionary(x => x.Key, x => x.Value);
            log.LogInformation($"classes done");
            log.LogInformation($"properties");
            // TODO: add logger everywhere 
            properties = ontology.Triples.Where(x =>
                x.Predicate.ToString().Equals(OntologyHelper.PropertyType)
                && (x.Object.ToString().Equals(OntologyHelper.OwlDatatypeProperty) ||
                x.Object.ToString().Equals(OntologyHelper.OwlObjectProperty) ||
                x.Object.ToString().Equals(OntologyHelper.RdfProperty)))
                .Select(x => x.Subject.ToString()).Distinct()
                .AsParallel()
                .Select(x => new InstanceLabel(x, this.propertyForLabel, this))
                .ToDictionary(x => x.Uri, x => x);
            log.LogInformation($"# properties: {properties.Count}");
            GetPropertyDomainRangeOrDataType(properties.Values.Select(x => x.Uri).ToList());
            log.LogInformation($"properties done");
        }

        private Dictionary<string, Dictionary<string, int>> ComputeSuperClasses(Dictionary<string, Dictionary<string, int>> dict)
        {
            if (!dict.Any()) return dict;
            var newDict = new Dictionary<string, Dictionary<string, int>>(); // we clone the dictionary, otherwise the loop will fail when hading new items
            foreach (var classUri in dict.Keys) // for each element in main dictionary
            {
                var classDict = dict[classUri];
                var newClassDict = new Dictionary<string, int>();
                foreach (var superClassPair in classDict) // get its super classes dict
                {
                    if (dict.ContainsKey(superClassPair.Key)) // if its super class is itself recorded in main dict
                    {
                        var superClassessOfSuperClass = dict[superClassPair.Key];
                        foreach (var superClassOfSuperClass in superClassessOfSuperClass)
                        {
                            if (!newClassDict.ContainsKey(superClassOfSuperClass.Key)) // and is not present in sub class
                            {
                                newClassDict.Add(superClassOfSuperClass.Key, superClassOfSuperClass.Value + 1); // then we had it
                            }
                        }

                    }
                }
                foreach (var pair in classDict)
                {
                    if (!newClassDict.ContainsKey(pair.Key))
                    {
                        newClassDict.Add(pair.Key, pair.Value);
                    }
                }
                newDict.Add(classUri, newClassDict);
            }
            // we compare size of the clone et of the original to check if there has been modification and if not we recursively start again
            var cloneSize = newDict.SelectMany(x => x.Value).Count();
            var originalSize = dict.SelectMany(x => x.Value).Count();
            if (cloneSize == originalSize)
                return newDict;
            else
                return ComputeSuperClasses(newDict);
        }

        /// <summary>
        /// For each class, we compute relative distances with its super classes
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, Dictionary<string, int>> GetClassesDepth()
        {
            var dict = ontology.Triples
                .Where(x => x.Predicate.ToString().Equals(OntologyHelper.PropertySubClassOf))
                .GroupBy(x => x.Subject.ToString())
                .ToDictionary(x => x.Key, x => x.ToDictionary(y => y.Object.ToString(), y => 1));
            if (!dict.Any())
            {
                log.LogInformation("We can not use ontology for subclasses.");
                if (!IsOpen) LoadHdt().Wait();
                dict = hdt.search("", PropertySubClassOf, "")
                    .GroupBy(x => x.getSubject())
                    .ToDictionary(x => x.Key, x => x.ToDictionary(y => y.getObject(), y => 1));
            }
            log.LogInformation("Recursively compute super classes");
            dict = ComputeSuperClasses(dict);
            log.LogInformation("Recursion ended");
            return dict;
            // var query = "PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#> select ?sub ?super (count(?mid) as ?distance) WHERE { { SELECT DISTINCT ?mid WHERE { ?s rdfs:subClassOf ?mid . } } ?sub rdfs:subClassOf* ?mid . ?mid rdfs:subClassOf+ ?super . } group by ?sub ?super  order by ?sub ?super";
            // var result = new Dictionary<string, Dictionary<string, int>>();
            // var sparqlResultSet = (SparqlResultSet)ontology.ExecuteQuery(query);
            // foreach (var sparqlResult in sparqlResultSet)
            // {
            //     var sub = sparqlResult["sub"] as UriNode;
            //     var super = sparqlResult["super"] as UriNode;
            //     var distance = sparqlResult["distance"] as LiteralNode;
            //     int distanceValue;
            //     if (!int.TryParse(distance.Value, out distanceValue)) distanceValue = 0;
            //     var subUri = sub.Uri.ToString();
            //     var superUri = super.Uri.ToString();
            //     Dictionary<string, int> dict;
            //     if (result.Keys.Contains(subUri))
            //     {
            //         dict = result[subUri];
            //     }
            //     else
            //     {
            //         dict = new Dictionary<string, int>();
            //         result.Add(subUri, dict);
            //     }
            //     if (!dict.Keys.Contains(superUri))
            //     {
            //         dict.Add(superUri, distanceValue);
            //     }
            // }
            // return result;
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
            if (!classesDepths.ContainsKey(aClass)) return new HashSet<string>();
            var superClasses = classesDepths[aClass];
            if (level == Level.First)
                return new HashSet<string>
                {
                    superClasses.Where(x => x.Value == 1).Select(x => x.Key).FirstOrDefault()
                };
            return new HashSet<string>
            (
                superClasses.Select(x => x.Key)
            );
            // var multipleLevels = level == Level.All ? "*" : string.Empty;
            // var query = @"
            //     PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#> 
            //     SELECT * WHERE { 
            //         <" + aClass + @"> rdfs:subClassOf" + multipleLevels + @" ?superClass . 
            //     }";
            // var results = (SparqlResultSet)ontology.ExecuteQuery(query);
            // var subClasses = new HashSet<string>();
            // foreach (var result in results)
            // {
            //     var superclass = result["superClass"].ToString();
            //     subClasses.Add(superclass);
            // }
            // return subClasses;
        }

        /// <summary>
        /// The key is the property uri, the value are its domain(s) and range(s) and the dash
        /// </summary>
        /// <value></value>
        public Dictionary<string, (string domain, string range, bool dash)> objectProperties { get; set; }
        // public Dictionary<string, HashSet<string>> objectPropertiesRange {get;private set;}
        /// <summary>
        /// The key is the property uri, the value is its datatype
        /// </summary>
        /// <value></value>
        public Dictionary<string, string> dataTypeProperties { get; set; }

        public void GetPropertyDomainRangeOrDataType(List<string> properties)
        {
            var objectPropertiesConcurrent = new ConcurrentDictionary<string, (string domain, string range, bool dash)>();
            var dataTypePropertiesConcurrent = new ConcurrentDictionary<string, string>();
            var count = 0;
            Parallel.ForEach(properties, propertyUri =>
            // Parallel.For(0, properties.Count, i =>
            {
                // var propertyUri = properties[i];
                if (string.IsNullOrWhiteSpace(propertyUri)) return;
                var propNode = ontology.GetUriNode(
                    new Uri(propertyUri));
                var rdfType = ontology.GetUriNode(new Uri(OntologyHelper.PropertyType));
                var owlObjectProp = ontology.GetUriNode(new Uri(OntologyHelper.OwlObjectProperty));
                if (ontology.ContainsTriple(
                    new Triple(propNode, rdfType, owlObjectProp)))
                {
                    // it is an object property
                    var domainsAndRanges = GetPropertyDomainRange(propertyUri).Result;
                    objectPropertiesConcurrent.TryAdd(propertyUri, domainsAndRanges);
                }
                else
                {
                    // it is NOT an object property
                    var dataType = GetPropertyDataType(propertyUri);
                    if (!string.IsNullOrWhiteSpace(dataType))
                        dataTypePropertiesConcurrent.TryAdd(propertyUri, dataType);
                }
                Interlocked.Increment(ref count);
                log.LogInformation($"property {count}/{properties.Count}");
            });
            objectProperties = objectPropertiesConcurrent.ToDictionary(x => x.Key, x => x.Value);
            dataTypeProperties = dataTypePropertiesConcurrent.ToDictionary(x => x.Key, x => x.Value);
        }

        private async Task<(string domain, string range, bool dash)> GetPropertyDomainRange(string propertyUri)
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
            var result = (GetDeepest(domains), GetDeepest(ranges), dash);
            return result;
        }

        public string GetDeepest(IEnumerable<string> classes)
        {
            if (!classes.Any()) return "http://www.w3.org/2002/07/owl#Thing";
            if (classes.Count() == 1) return classes.First();
            var result = "http://www.w3.org/2002/07/owl#Thing";
            var max = 0;
            foreach (var cls in classes.Where(x => !string.IsNullOrWhiteSpace(x)
                && x.StartsWith(OntologyNameSpace)))
            {
                var dict = classesDepths.GetValueOrDefault(cls);
                if (dict == null || !dict.Any()) continue;
                var currentMax = dict.Select(x => x.Value).Max();
                if (max < currentMax)
                {
                    max = currentMax;
                    result = cls; //dict.Where(x => x.Value == currentMax).Select(x => x.Key).FirstOrDefault();
                }
            }
            return result;
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
            if (!objectsTmp.Any()) return string.Empty;
            // getting all types for each object
            var typesByObject = objectsTmp
                .SelectMany(x => hdt.search(x, PropertyType, "").Select(y => y.getObject()))
                .GroupBy(x => x).Select(x => new { type = x.Key, count = x.Count() })
                .OrderByDescending(x => x.count).ToList();
            if (!typesByObject.Any()) return string.Empty;
            if (typesByObject.Count() == 1) return typesByObject.Select(x => x.type).FirstOrDefault();
            if (typesByObject.Any(x => x.type.StartsWith(OntologyNameSpace)))
            {
                var maxCount = typesByObject.Where(x => x.type.StartsWith(OntologyNameSpace)).Max(x => x.count);
                return GetDeepest(typesByObject.Where(x => x.type.StartsWith(OntologyNameSpace))
                    .Where(x => x.count == maxCount).Select(x => x.type));
            }
            else
            {
                var maxCount = typesByObject.Max(x => x.count);
                return GetDeepest(typesByObject.Where(x => x.count == maxCount).Select(x => x.type));
            }
            
            // .AsParallel().Select(obj =>
            // {
            //     var setTmp = GetObjects(obj, PropertyType).Result;
            //     return new { subject = obj, setTmp };
            // }).ToDictionary(x => x.subject, x => x.setTmp);

            // var orderedTypes = typesByObject.SelectMany(x => x.Value)
            //     .GroupBy(x => x).Select(x => new { type = x.Key, count = x.Count() })
            //     .OrderByDescending(x => x.count).ToList();

            // var useCounter = 0;
            // var typeMap = new Dictionary<string, int>();
            // // computation of the most used type
            // foreach (var entry in typesByObject)
            // {
            //     var types = entry.Value.Where(x =>
            //         x.StartsWith(OntologyNameSpace)).ToHashSet();

            //     foreach (var type in types)
            //     {
            //         if (typeMap.ContainsKey(type))
            //         {
            //             var c = typeMap.GetValueOrDefault(type);
            //             c++;
            //             typeMap[type] = c;
            //             if (useCounter < c)
            //                 useCounter = c;
            //         }
            //         else
            //         {
            //             typeMap[type] = 1;
            //         }
            //     }
            // }
            // // return the deepest used type 
            // if (typeMap.Count(x => x.Value == useCounter) > 1)
            // {
            //     // several types are used a lot (i.e. with the same number of use). We must choose the deepest
            //     var deepest = GetDeepest(typeMap.Where(x => x.Value == useCounter)
            //         .Select(x => x.Key));
            //     return deepest;
            // }
            // if (!typeMap.Any(x => x.Value == useCounter))
            // {// TODO: check if HDT project allow multiple iterator 
            //     // FIXME: most of time, the counter get stuck to 0....
            //     var deepest = GetDeepest(typeMap.Where(x => x.Value == useCounter)
            //         .Select(x => x.Key));
            //     return deepest;
            // }
            // return typeMap.Where(x => x.Value == useCounter)
            //     .Select(x => x.Key).FirstOrDefault();
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
        // public Dictionary<string, HashSet<string>> superClassesOfClass { get; set; }

    }
}