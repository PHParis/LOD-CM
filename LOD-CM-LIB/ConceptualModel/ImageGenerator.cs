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

namespace LOD_CM_CLI.Uml
{
    public class ImageGenerator
    {
        private Dataset ds;

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


        public static async Task<ImageGenerator> GenerateTxtForUml(Dataset ds,
            InstanceClass instanceClass, double threshold,
            PatternDiscovery.ItemSets<int> mfps, Mining.Transaction transactions)
        {
            var result = new ImageGenerator();
            result.ds = ds;
            result.propertyMinsup = new Dictionary<int, int>();

            var cModel = new StringBuilder();

            cModel.AppendLine("@startuml");
            cModel.AppendLine("skinparam linetype ortho");

            var thresholdInt = Convert.ToInt32(threshold * 100);
            foreach (var line in mfps)
            {
                var currentSupportInt = Convert.ToInt32(line.Support * 100);
                if (line.Count == 1 && thresholdInt <= currentSupportInt) // TODO: but why take only line with only one property
                    result.propertyMinsup.Add(line[0], currentSupportInt);
            }

            var rdfType = ds.ontology.GetUriNode(new Uri(OntologyHelper.PropertyType));
            var owlObjectProp = ds.ontology.GetUriNode(new Uri(OntologyHelper.OwlObjectProperty));
            // in this loop we check for each property, if it is an
            // object property or not
            var objectProperties = result.propertyMinsup.Select(p =>
                ds.ontology.GetUriNode(new Uri(transactions.intToPredicateDict[p.Key])))
                .Where(p => p != null).Where(p => ds.ontology.ContainsTriple(
                    new Triple(p, rdfType, owlObjectProp)))
                    .Select(x => x.Uri.AbsoluteUri).ToList();
            var notObjectProperties = result.propertyMinsup.Select(p => transactions.intToPredicateDict[p.Key])
                .Except(objectProperties).ToList();

            var classes = new HashSet<string>();
            classes.Add(instanceClass.Uri);
            // we search domain and range for each object property
            // to iteratively create the class diagram
            foreach (var op in objectProperties)
            {

                var opIndex = transactions.predicateToIntDict[op];
                var propertySupport = result.propertyMinsup.GetValueOrDefault(opIndex);

                var domain = ds.ontology.Triples.Where(x =>
                    x.Subject.ToString().Equals(op) &&
                    x.Predicate.ToString().Equals(OntologyHelper.PropertyDomain))
                    .Select(x => x.Object.ToString())
                    .FirstOrDefault();


                var range = ds.ontology.Triples.Where(x =>
                    x.Subject.ToString().Equals(op) &&
                    x.Predicate.ToString().Equals(OntologyHelper.PropertyRange))
                    .Select(x => x.Object.ToString())
                    .FirstOrDefault();

                string dd;
                string rr;
                var dash = false;

                if (domain == null)
                {
                    dd = await result.FindRangeOrDomain(op, RangeOrDomain.Domain);
                    classes.Add(dd);
                    dash = true;
                }
                else
                    dd = domain; // FIXME: ToString to delete
                classes.Add(dd);

                if (range == null)
                {
                    rr = await result.FindRangeOrDomain(op, RangeOrDomain.Range);
                    classes.Add(rr);
                    dash = true;
                }
                else
                    rr = range;
                classes.Add(rr);

                var d = dd.GetUriFragment();
                var r = rr.GetUriFragment();
                var p = op.GetUriFragment();
                if (r.Equals(d))
                    continue;
                if (dash)
                    cModel.AppendLine(d + " .. " + r + " : " + p + " sup:" + propertySupport);
                else
                    cModel.AppendLine(d + " -- " + r + " : " + p + " sup:" + propertySupport);
            }

            cModel.AppendLine("class " + instanceClass.Label + "{");
            foreach (var dtp in notObjectProperties)
            {

                int cc = transactions.predicateToIntDict[dtp];//HashmapItem.Where(x => x.Value == dtp).Select(x => x.Key).FirstOrDefault();//.GetValueOrDefault(dtp);// getKey(HashmapItem, dtp);
                var sup = result.propertyMinsup.GetValueOrDefault(cc);
                var val = ds.ontology.Triples.Where(x =>
                    x.Subject.ToString().Equals(dtp) &&
                    x.Predicate.ToString().Equals(OntologyHelper.PropertyRange))
                    .Select(x => x.Object.ToString())
                    .FirstOrDefault();
                string p = dtp.GetUriFragment();
                if (val != null)
                {
                    string r = val.GetUriFragment();
                    cModel.AppendLine(p + ":" + r + " sup=" + sup);
                }
                else
                {
                    cModel.AppendLine(p + " sup=" + sup);
                }
            }
            cModel.AppendLine("}");

            var classesAndSuperClasses = new HashSet<string>();
            // get all super classes of the given class
            foreach (var c in classes)
            {
                classesAndSuperClasses.Add(c);
                var subclasses = result.FindSuperClasses(c, Level.All);
                foreach (var s in subclasses)
                    classesAndSuperClasses.Add(s);
            }
            // get the first super class for each class
            foreach (var c in classesAndSuperClasses)
            {
                var superClasses = result.FindSuperClasses(c, Level.First);

                if (!superClasses.Any())
                    continue;
                foreach (var sc in superClasses)
                {
                    if (c.Equals(sc))
                        continue;
                    var c1 = c.GetUriFragment();
                    var c2 = sc.GetUriFragment();
                    cModel.AppendLine(c2 + " <|-- " + c1);
                }
            }

            cModel.AppendLine("@enduml");
            result.contentForUml = cModel.ToString();
            return result;
        }

        
        public enum Level
        {
            First,
            All
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
            var results = (SparqlResultSet)ds.ontology.ExecuteQuery(query);
            var subClasses = new HashSet<string>();
            foreach (var result in results)
            {
                var superclass = result["superClass"].ToString();
                subClasses.Add(superclass);
            }
            return subClasses;
        }

        public enum RangeOrDomain
        {
            Range,
            Domain
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
                await ds.GetObjects("", property) :
                await ds.GetSubjects(property, "");
            // getting all types for each object
            var typesByObject = objectsTmp.AsParallel().Select(obj =>
            {
                var setTmp = ds.GetObjects(obj, ds.PropertyType).Result;
                return new { subject = obj, setTmp };
            }).ToDictionary(x => x.subject, x => x.setTmp);

            var useCounter = 0;
            var typeMap = new Dictionary<string, int>();
            // computation of the most used type
            foreach (var entry in typesByObject)
            {
                var types = entry.Value.Where(x =>
                    x.StartsWith(ds.OntologyNameSpace)).ToHashSet();

                foreach (var type in types)
                {
                    if (typeMap.ContainsKey(type))
                    {
                        var c = typeMap.GetValueOrDefault(type);
                        c++;
                        typeMap.Add(type, c);
                        if (useCounter < c)
                            useCounter = c;
                    }
                    else
                    {
                        typeMap.Add(type, 1);
                    }
                }
            }
            // return the most used type
            return typeMap.Where(x => x.Value == useCounter)
                .Select(x => x.Key).FirstOrDefault();
        }

    }
}