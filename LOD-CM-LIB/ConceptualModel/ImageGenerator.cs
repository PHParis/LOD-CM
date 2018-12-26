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

namespace LOD_CM_CLI.Uml
{
    public class ImageGenerator
    {
        private Dataset ds;

        private ImageGenerator() { }
        public Dictionary<int, string> propertyMinsup {get;private set;}

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
            result.propertyMinsup = new Dictionary<int, string>();
            var attributes = "";
            var cModel = new StringBuilder();
            var finalclass = new HashSet<string>();

            cModel.AppendLine("@startuml");
            cModel.AppendLine("skinparam linetype ortho");

            var classes = new HashSet<string>();
            var classesWithSubclass = new HashSet<string>();

            double support;

            classes.Add(instanceClass.Uri);

            var numberOfTransactions = transactions.transactions.Count;
            foreach (var line in mfps)
            {
                support = line.TransactionCount;

                var supp = (int)((support / numberOfTransactions) * 100);
                var thre = Convert.ToInt32(threshold * 100);
                if (line.Count == 1 && thre <= supp) // TODO: but why take only line with only one property
                    result.propertyMinsup.Add(line[0], supp.ToString());
            }

            var rdfType = ds.ontology.GetUriNode(new Uri(OntologyHelper.PropertyType));
            var owlObjectProp = ds.ontology.GetUriNode(new Uri(OntologyHelper.OwlObjectProperty));
            // in this loop we check for each property, if it is an
            // object property or not
            var objectProperties = result.propertyMinsup.Select(p => ds.ontology.GetUriNode(new Uri(p.Value)))
                .Where(p => p != null).Where(p => ds.ontology.ContainsTriple(
                    new Triple(p, rdfType, owlObjectProp)))
                    .Select(x => x.Uri.AbsolutePath).ToList();
            var notObjectProperties = result.propertyMinsup.Select(p => p.Value)
                .Except(objectProperties).ToList();

            foreach (string opp in objectProperties)
            {

                int cc = transactions.predicateToIntDict[opp];//HashmapItem.Where(x => x.Value == opp).Select(x => x.Key).FirstOrDefault();//getKey(HashmapItem, opp);
                string vv = result.propertyMinsup.GetValueOrDefault(cc);

                string dd = "";
                string rr = "";
                bool dash = false;
                var domain = ds.ontology.Triples.Where(x =>
                    x.Subject.ToString().Equals(opp) &&
                    x.Predicate.ToString().Equals(OntologyHelper.PropertyDomain))
                    .Select(x => x.Object.ToString())
                    .FirstOrDefault();


                var range = ds.ontology.Triples.Where(x =>
                    x.Subject.ToString().Equals(opp) &&
                    x.Predicate.ToString().Equals(OntologyHelper.PropertyRange))
                    .Select(x => x.Object.ToString())
                    .FirstOrDefault();


                if (domain == null)
                {
                    dd = await result.FindRangeOrDomain(opp, RangeOrDomain.Domain);
                    classes.Add(dd);
                    dash = true;
                }
                else if (domain != null)
                    dd = domain.ToString();
                classes.Add(dd.ToString());

                if (range == null)
                {
                    rr = await result.FindRangeOrDomain(opp, RangeOrDomain.Range);
                    classes.Add(rr);
                    dash = true;
                }
                else if (range != null)
                    rr = range.ToString();
                classes.Add(rr.ToString());

                string d = dd.Substring(dd.LastIndexOf("/") + 1);
                string r = rr.Substring(rr.LastIndexOf("/") + 1);
                string p = opp.Substring(opp.LastIndexOf("/") + 1);
                if (d.Contains("#"))
                    d = d.Substring(d.LastIndexOf("#") + 1);
                if (r.Contains("#"))
                    r = r.Substring(r.LastIndexOf("#") + 1);
                if (r.Equals(d))
                    continue;
                if (dash)
                    cModel.AppendLine(d + " .. " + r + " : " + p + " sup:" + vv);
                else
                    cModel.AppendLine(d + " -- " + r + " : " + p + " sup:" + vv);
            }

            cModel.AppendLine("class " + instanceClass.Label + "{");
            foreach (string dtp in notObjectProperties)
            {

                int cc = transactions.predicateToIntDict[dtp];//HashmapItem.Where(x => x.Value == dtp).Select(x => x.Key).FirstOrDefault();//.GetValueOrDefault(dtp);// getKey(HashmapItem, dtp);
                string vv = result.propertyMinsup.GetValueOrDefault(cc);
                var val = ds.ontology.Triples.Where(x =>
                    x.Subject.ToString().Equals(dtp) &&
                    x.Predicate.ToString().Equals(OntologyHelper.PropertyRange))
                    .Select(x => x.Object.ToString())
                    .FirstOrDefault();
                string p = dtp.Substring(dtp.LastIndexOf("/") + 1);
                if (p.Contains("#"))
                    p = p.Substring(p.LastIndexOf("#") + 1);
                if (val != null)
                {
                    string r = val.Substring(val.LastIndexOf("/") + 1);
                    cModel.AppendLine(p + ":" + r + " sup=" + vv);
                }
                else
                {
                    cModel.AppendLine(p + " sup=" + vv);
                }
            }
            attributes = attributes + "}";
            cModel.AppendLine(attributes);

            HashSet<string> subclasses = new HashSet<string>();
            foreach (string c in classes)
            {
                classesWithSubclass.Add(c);
                subclasses = result.findSubclassAll(c);
                foreach (string s in subclasses)
                    classesWithSubclass.Add(s);
            }
            foreach (string c in classesWithSubclass)
            {
                subclasses = result.findSubclass(c);

                if (subclasses.Count == 0)
                    continue;
                foreach (string sc in subclasses)
                {
                    string c1 = c.Substring(c.LastIndexOf("/") + 1);
                    string c2 = sc.Substring(sc.LastIndexOf("/") + 1);
                    if (c1.Equals(c2))
                        continue;
                    if (c1.Contains("#"))
                        c1 = c1.Substring(c1.LastIndexOf("#") + 1);
                    if (c2.Contains("#"))
                        c2 = c2.Substring(c2.LastIndexOf("#") + 1);
                    cModel.AppendLine(c2 + " <|-- " + c1);
                    if (c1.Contains("Thing"))
                        continue;
                    else
                        finalclass.Add("\"" + c1 + "\"");
                    if (c2.Contains("Thing"))
                        continue;
                    else
                        finalclass.Add("\"" + c2 + "\"");
                }
            }

            cModel.AppendLine("@enduml");
            result.contentForUml = cModel.ToString();
            return result;
        }
        
        
        public HashSet<string> findSubclassAll(string aClass)
        {
            var subClasses = new HashSet<string>();
            var query = "PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#> " + "SELECT * WHERE { " + "<" + aClass
                    + "> rdfs:subClassOf* ?superClass . " + " } ";
            var results = (SparqlResultSet)ds.ontology.ExecuteQuery(query);
            foreach (var result in results)
            {
                var superclass = result["superClass"].ToString();
                subClasses.Add(superclass);
            }
            return subClasses;
        }
        public HashSet<string> findSubclass(string aClass)
        {
            var subClasses = new HashSet<string>();
            var query = "PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#> " + "SELECT * WHERE { " + "<" + aClass
                    + "> rdfs:subClassOf ?superClass . " + " } ";
            var results = (SparqlResultSet)ds.ontology.ExecuteQuery(query);
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