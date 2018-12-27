using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LOD_CM_CLI.Data;
using VDS.RDF;
using VDS.RDF.Ontology;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using System.Threading.Tasks;
using System.Text;
using Iternity.PlantUML;
using System.Net.Http;
using LOD_CM_CLI.Utils;
using LOD_CM_CLI.Mining;

namespace LOD_CM_CLI.Uml
{
    public class ImageGenerator
    {

        private ImageGenerator() { }

        /// <summary>
        /// Key is the property id, value is its support
        /// </summary>
        /// <value></value>
        public Dictionary<int, int> propertyMinsup { get; private set; }

        /// <summary>
        /// Content sended to PlantUML for image generation
        /// </summary>
        /// <value></value>
        public string contentForUml { get; private set; }
        /// <summary>
        /// Content of the SVG file retrieved from PlantUML
        /// </summary>
        /// <value></value>
        public string svgFileContent { get; private set; }

        /// <summary>
        /// the key is a class uri and value contains all its direct super class,
        /// i.e. there is a gap of only one between classes from value and the key.
        /// For example, Actor will have only Artist (not Person because Person 
        /// is too far from Actor (distance of 2))
        /// </summary>
        private Dictionary<string, HashSet<string>> superClassesOfClass;
        public FrequentPattern<int> fp {get;private set;}

        /// <summary>
        /// Download SVG file content from PlantUML
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetImageContent()
        {
            var uri = PlantUMLUrl.SVG(contentForUml);
            using (var client = new HttpClient())
            {
                svgFileContent = await client.GetStringAsync(uri);
            }
            return svgFileContent;
        }

        public async Task SaveImage(string filePath)
        {
            await File.WriteAllTextAsync(filePath, svgFileContent);
        }
        public async Task SaveContentForPlantUML(string filePath)
        {
            await File.WriteAllTextAsync(filePath, contentForUml);
        }


        public IEnumerable<string> GetAllSuperClasses(string classUri)
        {
            var set = fp.transactions.dataset.superClassesOfClass[classUri];
            if (!set.Any()) yield return string.Empty;
            else
            {
                foreach (var superClass in set)
                {
                    var c = classUri.GetUriFragment();
                    var sc = superClass.GetUriFragment();
                    yield return c + " <|-- " + sc;
                    foreach (var res in GetAllSuperClasses(superClass))
                    {
                        yield return res;
                    }
                }
            }
            
            // return set.Union(set
            //     .SelectMany(uri => GetAllSuperClasses(uri))).ToHashSet();
        }


        public static async Task<List<ImageGenerator>> GenerateTxtForUml(Dataset ds,
            InstanceClass instanceClass, double threshold,
            FrequentPattern<int> fp)
        {
            // result.propertyMinsup = new Dictionary<int, int>();

            // var cModel = new StringBuilder();

            // cModel.AppendLine("@startuml");
            // cModel.AppendLine("skinparam linetype ortho");

            var thresholdInt = Convert.ToInt32(threshold * 100);
            var maximalSets = fp.fis.Where(x => x.IsMaximal).OrderByDescending(x => x.Count)
                .ThenByDescending(x => x.TransactionCount).ToList();
            var finalResults = new List<ImageGenerator>();
            foreach (var mfp in maximalSets)
            {
                var result = new ImageGenerator();
                result.fp = fp;
                finalResults.Add(result);
                var cModel = new StringBuilder();

                cModel.AppendLine("@startuml");
                cModel.AppendLine("skinparam linetype ortho");
                
                var propertySupport = Convert.ToInt32(mfp.Support * 100);
                var usedProp = new HashSet<int>();
                var classes = new HashSet<string>();
                // first loop for object properties
                foreach (var id in mfp)
                {
                    var property = fp.transactions.intToPredicateDict[id];
                    if (fp.transactions.dataset.objectProperties.ContainsKey(property))
                    {
                        var p = property.GetUriFragment();
                        var domainAndRange = fp.transactions.dataset.objectProperties[property];
                        var dash = domainAndRange.dash;
                        // foreach (var domain in domainAndRange.domain)
                        // {
                            var domain = domainAndRange.domain;
                            var d = domain.GetUriFragment();
                            // foreach (var range in domainAndRange.ranges)
                            // {       
                                var range = domainAndRange.range;
                                if (domain.Equals(range)) continue;      
                                var r = range.GetUriFragment();                   
                                if (dash)
                                    cModel.AppendLine(d + " .. " + r + " : " + p + " sup:" + propertySupport);
                                else
                                    cModel.AppendLine(d + " -- " + r + " : " + p + " sup:" + propertySupport);
                                usedProp.Add(id);
                                classes.Add(domain);
                                classes.Add(range);
                            // }
                        // }
                    }

                }
                cModel.AppendLine("class " + instanceClass.Label + "{");
                // second loop for datatype properties
                foreach (var id in mfp)
                {
                    var property = fp.transactions.intToPredicateDict[id];
                    if (fp.transactions.dataset.dataTypeProperties.ContainsKey(property))
                    {
                        var p = property.GetUriFragment();
                        var datatype = fp.transactions.dataset.dataTypeProperties.GetValueOrDefault(property);
                        var r = datatype.GetUriFragment();
                        cModel.AppendLine(p + ":" + r + " sup=" + propertySupport);
                        usedProp.Add(id);                            
                    }
                }
                // third loop for properties without info
                foreach (var id in mfp.Except(usedProp))
                {
                    var property = fp.transactions.intToPredicateDict[id];
                    var p = property.GetUriFragment();
                    cModel.AppendLine(p + " sup=" + propertySupport);
                }
                cModel.AppendLine("}");
                // loop for current class hierarchy
                foreach (var line in result.GetAllSuperClasses(instanceClass.Uri))
                {
                    cModel.AppendLine(line);
                }
                // loop for related classes hierarchy
                foreach (var classUri in classes)
                {
                    var c = classUri.GetUriFragment();
                    foreach (var superClass in fp.transactions.dataset.superClassesOfClass[classUri])
                    {
                        var sc = superClass.GetUriFragment();
                        cModel.AppendLine(c + " <|-- " + sc);
                    }
                }
                cModel.AppendLine("@enduml");
                result.contentForUml = cModel.ToString();
            }
            return finalResults;
            // #region To do before this function!
            // var properties = maximalSets.SelectMany(x => x).ToHashSet();
            // var rangeDomainClasses = new HashSet<string>(); // TODO: fill it with folowing loop
            // foreach (var property in properties)
            // {
            //     // var tmp = GetPropertyDomainRangeOrDataType(property);
            //     throw new NotImplementedException();
            // }
            // result.superClassesOfClass = new Dictionary<string, HashSet<string>>();
            // foreach (var @class in rangeDomainClasses)
            // {
            //     if (!result.superClassesOfClass.ContainsKey(@class))
            //     {
            //         var set = result.FindSuperClasses(@class, Level.First);
            //         result.superClassesOfClass.Add(@class, set);
            //     }
            // }
            // #endregion
            // // computation of super classes etc.
            // // creations of models for each mfp in maximalSets
            // var selectedLine = mfps.Where(x => x.IsMaximal).OrderByDescending(x => x.Count)
            //     .ThenByDescending(x => x.TransactionCount)
            //     // .Select(x => new {itemSet = x, support = Convert.ToInt32(x.Support * 100)})
            //     .FirstOrDefault(); // TODO: loop on all mfp and create coresponding images
            // result.propertyMinsup = selectedLine.ToDictionary(x => x,
            //     x => Convert.ToInt32(selectedLine.Support * 100));
            // // foreach (var line in mfps)// TODO: use real MFP here.
            // // {
            // //     var currentSupportInt = Convert.ToInt32(line.Support * 100);
            // //     if (line.Count == 1 && thresholdInt <= currentSupportInt) // TODO: but why take only line with only one property
            // //         result.propertyMinsup.Add(line[0], currentSupportInt);
            // // }

            // var rdfType = ds.ontology.GetUriNode(new Uri(OntologyHelper.PropertyType));
            // var owlObjectProp = ds.ontology.GetUriNode(new Uri(OntologyHelper.OwlObjectProperty));
            // // in this loop we check for each property, if it is an
            // // object property or not
            // var objectProperties = result.propertyMinsup.Select(p =>
            //     ds.ontology.GetUriNode(new Uri(transactions.intToPredicateDict[p.Key])))
            //     .Where(p => p != null).Where(p => ds.ontology.ContainsTriple(
            //         new Triple(p, rdfType, owlObjectProp)))
            //         .Select(x => x.Uri.AbsoluteUri).ToList();
            // var notObjectProperties = result.propertyMinsup.Select(p => transactions.intToPredicateDict[p.Key])
            //     .Except(objectProperties).ToList();

            // var classes = new HashSet<string> { instanceClass.Uri };
            // // we search domain and range for each object property
            // // to iteratively create the class diagram
            // foreach (var op in objectProperties)
            // {

            //     var opIndex = transactions.predicateToIntDict[op];
            //     var propertySupport = result.propertyMinsup.GetValueOrDefault(opIndex);

            //     var domain = ds.ontology.Triples.Where(x =>
            //         x.Subject.ToString().Equals(op) &&
            //         x.Predicate.ToString().Equals(OntologyHelper.PropertyDomain))
            //         .Select(x => x.Object.ToString())
            //         .FirstOrDefault();


            //     var range = ds.ontology.Triples.Where(x =>
            //         x.Subject.ToString().Equals(op) &&
            //         x.Predicate.ToString().Equals(OntologyHelper.PropertyRange))
            //         .Select(x => x.Object.ToString())
            //         .FirstOrDefault();

            //     string dd;
            //     string rr;
            //     var dash = false;

            //     if (domain == null)
            //     {
            //         dd = await result.FindRangeOrDomain(op, RangeOrDomain.Domain);
            //         classes.Add(dd);
            //         dash = true;
            //     }
            //     else
            //         dd = domain;
            //     classes.Add(dd);

            //     if (range == null)
            //     {
            //         rr = await result.FindRangeOrDomain(op, RangeOrDomain.Range);
            //         classes.Add(rr);
            //         dash = true;
            //     }
            //     else
            //         rr = range;
            //     classes.Add(rr);

            //     var d = dd.GetUriFragment();
            //     var r = rr.GetUriFragment();
            //     var p = op.GetUriFragment();
            //     if (r.Equals(d))
            //         continue;
            //     if (dash)
            //         cModel.AppendLine(d + " .. " + r + " : " + p + " sup:" + propertySupport);
            //     else
            //         cModel.AppendLine(d + " -- " + r + " : " + p + " sup:" + propertySupport);
            // }

            // cModel.AppendLine("class " + instanceClass.Label + "{");
            // foreach (var dtp in notObjectProperties)
            // {

            //     int cc = transactions.predicateToIntDict[dtp];//HashmapItem.Where(x => x.Value == dtp).Select(x => x.Key).FirstOrDefault();//.GetValueOrDefault(dtp);// getKey(HashmapItem, dtp);
            //     var sup = result.propertyMinsup.GetValueOrDefault(cc);
            //     var val = ds.ontology.Triples.Where(x =>
            //         x.Subject.ToString().Equals(dtp) &&
            //         x.Predicate.ToString().Equals(OntologyHelper.PropertyRange))
            //         .Select(x => x.Object.ToString())
            //         .FirstOrDefault();
            //     string p = dtp.GetUriFragment();
            //     if (val != null)
            //     {
            //         string r = val.GetUriFragment();
            //         cModel.AppendLine(p + ":" + r + " sup=" + sup);
            //     }
            //     else
            //     {
            //         cModel.AppendLine(p + " sup=" + sup);
            //     }
            // }
            // cModel.AppendLine("}");

            // var classesAndSuperClasses = new HashSet<string>();
            // // get all super classes of the given class
            // foreach (var c in classes)
            // {
            //     classesAndSuperClasses.Add(c);
            //     var subclasses = result.FindSuperClasses(c, Level.All);
            //     foreach (var s in subclasses)
            //         classesAndSuperClasses.Add(s);
            // }
            // // get the first super class for each class
            // foreach (var c in classesAndSuperClasses)
            // {
            //     var superClasses = result.FindSuperClasses(c, Level.First);

            //     if (!superClasses.Any())
            //         continue;
            //     foreach (var sc in superClasses)
            //     {
            //         if (c.Equals(sc))
            //             continue;
            //         var c1 = c.GetUriFragment();
            //         var c2 = sc.GetUriFragment();
            //         cModel.AppendLine(c2 + " <|-- " + c1);
            //     }
            // }

            // cModel.AppendLine("@enduml");
            // result.contentForUml = cModel.ToString();
            // return result;
        }

    }
}