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
        Dataset ds;

        public ImageGenerator(Dataset ds)
        {
            this.ds = ds;
            propertyMinsup = new Dictionary<int, string>();
        }
        public Dictionary<int, string> propertyMinsup;// = new Dictionary<int, string>();

        // public static Dictionary<int, string> HashmapItem = new Dictionary<int, string>();
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
        // private static IGraph _dbpOnt;
        // public static IGraph dbpOnt
        // {
        //     get
        //     {
        //         if (_dbpOnt == null)
        //         {
        //             _dbpOnt = new Graph();
        //             FileLoader.Load(_dbpOnt, Path.Combine(
        //                 @"C:\dev\dotnet\LOD-CM\LOD-CM-CLI\examples",
        //                 "dbpedia_2016-10.nt")); // dbpedia_2016-10.nt
        //                                         //dbpedia_2014.owl
        //             Console.WriteLine("DBpedia onto loaded");
        //         }
        //         return _dbpOnt;
        //     }
        // }

        public async Task GetImageContent()
        {
            var uri = PlantUMLUrl.SVG(contentForUml);
            using (var client = new HttpClient())
            {
                svgFileContent = await client.GetStringAsync(uri);
            }
        }

        // public static Uri RDFType = new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#type");
        public async Task SaveImage(string filePath)
        {
            await File.WriteAllTextAsync(filePath, svgFileContent);
        }
        public async Task SaveContentForPlantUML(string filePath)
        {
            await File.WriteAllTextAsync(filePath, contentForUml);
        }

        public async Task<string> GenerateTxtForUml(string type, double threshold,
            int numberofTransactions, PatternDiscovery.ItemSets<int> mfps,
            Dictionary<int, string> HashmapItem, List<Tuple<HashSet<int>, double>> mfpsV2)
        {
            // log.debug("entering CreateTxtFile...");
            // log.debug("type: " + type);
            // log.debug("threshold: " + threshold);
            // log.debug("numberofTransactions: " + numberofTransactions);
            string attributes = "";
            var CModel = new StringBuilder();
            HashSet<string> finalclass = new HashSet<string>();
            CModel.AppendLine("@startuml");
            CModel.AppendLine("skinparam linetype ortho");

            HashSet<string> classes = new HashSet<string>();
            HashSet<string> classesWithSubclass = new HashSet<string>();
            // BufferedReader reader = null;

            string pattern;
            double support;

            List<string> ObjectProperties = new List<string>();
            List<string> NotObjectProperties = new List<string>();
            classes.Add("http://dbpedia.org/ontology/" + type);

            // string line;
            // readHashmap();
            // File file = new File(this.mfpPathFile);
            // reader = new BufferedReader(new FileReader(file));

            if (mfps != null)
            {
                foreach (var line in mfps)
                {
                    // pattern = line.Substring(0, line.indexOf(" #"));
                    support = line.TransactionCount;//Double.parseDouble(line.Substring(line.indexOf("#") + 5));
                    int[] properties = line.ToArray();//pattern.split(" ");
                    int supp = (int)((support / numberofTransactions) * 100);
                    int thre = Convert.ToInt32(threshold * 100);
                    if (properties.Length == 1 && thre <= supp)
                        propertyMinsup.Add(properties[0], supp.ToString());
                }
            }
            else
            {
                foreach (var line in mfpsV2)
                {
                    // pattern = line.Substring(0, line.indexOf(" #"));
                    support = line.Item2;//Double.parseDouble(line.Substring(line.indexOf("#") + 5));
                    string[] properties = line.Item1.Select(x => x.ToString()).ToArray();//pattern.split(" ");
                    // int supp = Convert.ToInt32(Math.Floor(support * 100));
                    int supp = (int)((support / numberofTransactions) * 100);
                    int thre = Convert.ToInt32(threshold * 100);
                    if (properties.Length == 1 && thre <= supp)
                        propertyMinsup.Add(Convert.ToInt32(properties[0]), supp.ToString());
                }
            }

            // reader.close();
            var rdfType = ds.ontology.GetUriNode(new Uri(OntologyHelper.PropertyType));
            foreach (int name in propertyMinsup.Keys)
            {

                int key = name;

                string property = HashmapItem[key];
                var propertyNode = ds.ontology.GetUriNode(new Uri(property));
                if (propertyNode == null) continue;
                bool NOObjectProperty = true;
                var listTypes = ds.ontology.GetTriplesWithSubjectPredicate(
                    propertyNode, rdfType).Select(x => x.Object as IUriNode)
                    .ToHashSet();
                // HashSet<RDFNode> listTypes = dbpOnt.listStatements(ResourceFactory.createResource(property),
                //         ResourceFactory.createProperty("http://www.w3.org/1999/02/22-rdf-syntax-ns#type"), (RDFNode)null)
                //         .toList().stream().map(Statement::getObject).collect(Collectors.toSet());
                foreach (var typ1 in listTypes)
                {
                    if (typ1.Uri.AbsoluteUri.Contains("#ObjectProperty"))
                    {
                        ObjectProperties.Add(property);
                        NOObjectProperty = false;
                        break;
                    }
                }
                if (NOObjectProperty)
                    NotObjectProperties.Add(property);
            }

            foreach (string opp in ObjectProperties)
            {

                int cc = getKey(HashmapItem, opp);
                string vv = propertyMinsup.GetValueOrDefault(cc);

                string dd = "";
                string rr = "";
                bool dash = false;
                var domain = ds.ontology.Triples.Where(x =>
                    x.Subject.ToString().Equals(opp) &&
                    x.Predicate.ToString().Equals(OntologyHelper.PropertyDomain))
                    .Select(x => x.Object.ToString())
                    .FirstOrDefault();
                // RDFNode domain = dbpOnt.getProperty(ResourceFactory.createResource(opp), RDFS.domain) != null
                //         ? dbpOnt.getProperty(ResourceFactory.createResource(opp), RDFS.domain).getObject()
                //         in null;

                var range = ds.ontology.Triples.Where(x =>
                    x.Subject.ToString().Equals(opp) &&
                    x.Predicate.ToString().Equals(OntologyHelper.PropertyRange))
                    .Select(x => x.Object.ToString())
                    .FirstOrDefault();
                // RDFNode range = dbpOnt.getProperty(ResourceFactory.createResource(opp), RDFS.range) != null
                //         ? dbpOnt.getProperty(ResourceFactory.createResource(opp), RDFS.range).getObject()
                //         in null;

                if (domain == null)
                {
                    dd = await FindDomain(opp);
                    classes.Add(dd);
                    dash = true;
                }
                else if (domain != null)
                    dd = domain.ToString();
                classes.Add(dd.ToString());

                if (range == null)
                {
                    rr = await FindRange(opp);
                    classes.Add(rr);
                    dash = true;
                }
                else if (range != null)
                    rr = range.ToString();
                classes.Add(rr.ToString());

                string d = dd.Substring(dd.LastIndexOf("/") + 1);
                // string d = ResourceFactory.createResource(dd).ToString()
                //         .Substring(ResourceFactory.createResource(dd).ToString().LastIndexOf("/") + 1);
                string r = rr.Substring(rr.LastIndexOf("/") + 1);
                // string r = ResourceFactory.createResource(rr).ToString()
                //         .Substring(ResourceFactory.createResource(rr).ToString().LastIndexOf("/") + 1);
                string p = opp.Substring(opp.LastIndexOf("/") + 1);
                // string p = ResourceFactory.createProperty(opp).ToString()
                //         .Substring(ResourceFactory.createProperty(opp).ToString().LastIndexOf("/") + 1);
                if (d.Contains("#"))
                    d = d.Substring(d.LastIndexOf("#") + 1);
                if (r.Contains("#"))
                    r = r.Substring(r.LastIndexOf("#") + 1);
                if (r.Equals(d))
                    continue;
                if (dash)
                    CModel.AppendLine(d + " .. " + r + " : " + p + " sup:" + vv);
                else
                    CModel.AppendLine(d + " -- " + r + " : " + p + " sup:" + vv);
            }
            // attributes = "class " + type + "{\n";
            CModel.AppendLine("class " + type + "{");
            foreach (string dtp in NotObjectProperties)
            {

                int cc = getKey(HashmapItem, dtp);
                string vv = propertyMinsup.GetValueOrDefault(cc);
                var val = ds.ontology.Triples.Where(x =>
                    x.Subject.ToString().Equals(dtp) &&
                    x.Predicate.ToString().Equals(OntologyHelper.PropertyRange))
                    .Select(x => x.Object.ToString())
                    .FirstOrDefault();
                // RDFNode val = dbpOnt.getProperty(ResourceFactory.createResource(dtp), RDFS.range) != null
                //         ? dbpOnt.getProperty(ResourceFactory.createResource(dtp), RDFS.range).getObject()
                //         in null;
                string p = dtp.Substring(dtp.LastIndexOf("/") + 1);
                // string p = ResourceFactory.createProperty(dtp).ToString()
                //         .Substring(ResourceFactory.createProperty(dtp).ToString().LastIndexOf("/") + 1);
                if (p.Contains("#"))
                    p = p.Substring(p.LastIndexOf("#") + 1);
                if (val != null)
                {
                    string r = val.Substring(val.LastIndexOf("/") + 1);
                    // string r = ResourceFactory.createResource(val.ToString()).ToString()
                    //         .Substring(ResourceFactory.createResource(val.ToString()).ToString().LastIndexOf("/") + 1);
                    // attributes = attributes + p + ":" + r + " sup=" + vv + "\n";
                    CModel.AppendLine(p + ":" + r + " sup=" + vv);
                }
                else
                {
                    // attributes = attributes + p + " sup=" + vv + "\n";
                    CModel.AppendLine(p + " sup=" + vv);
                }
            }
            attributes = attributes + "}";
            CModel.AppendLine(attributes);

            HashSet<string> subclasses = new HashSet<string>();
            foreach (string c in classes)
            {
                classesWithSubclass.Add(c);
                subclasses = findSubclassAll(c);
                foreach (string s in subclasses)
                    classesWithSubclass.Add(s);
            }
            foreach (string c in classesWithSubclass)
            {
                subclasses = findSubclass(c);

                if (subclasses.Count == 0)
                    continue;
                foreach (string sc in subclasses)
                {
                    // outputModelsupclasses.Add(ResourceFactory.createStatement(ResourceFactory.createResource(c.ToString()),
                    //         sub, ResourceFactory.createResource(sc.ToString())));
                    string c1 = c.Substring(c.LastIndexOf("/") + 1);
                    // string c1 = ResourceFactory.createResource(c).ToString()
                    //         .Substring(ResourceFactory.createResource(c).ToString().LastIndexOf("/") + 1);
                    string c2 = sc.Substring(sc.LastIndexOf("/") + 1);
                    // string c2 = ResourceFactory.createResource(sc).ToString()
                    //         .Substring(ResourceFactory.createResource(sc).ToString().LastIndexOf("/") + 1);
                    if (c1.Equals(c2))
                        continue;
                    if (c1.Contains("#"))
                        c1 = c1.Substring(c1.LastIndexOf("#") + 1);
                    if (c2.Contains("#"))
                        c2 = c2.Substring(c2.LastIndexOf("#") + 1);
                    CModel.AppendLine(c2 + " <|-- " + c1);
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

            CModel.AppendLine("@enduml");
            contentForUml = CModel.ToString();
            return contentForUml;
            // Path fileCModel = Paths
            //         .get("/srv/www/htdocs/demo_conception/pictures_uml/CModel_" + type + "_" + threshold + ".txt");
            // Files.write(fileCModel, CModel, Charset.forName("UTF-8"));

            // try (FileWriter fileJSON = new FileWriter(
            //         "/srv/www/htdocs/demo_conception/pictures_uml/JSONclasses_" + type + "_" + threshold + ".json")) {
            //     fileJSON.write(finalclass.ToString());
            //     fileJSON.flush();
            //     fileJSON.close();
            //     System.out.println("Successfully Copied JSON Object to File...");
            //     System.out.println("\nJSON Object C BON");
            // } catch (IOException e) {
            //     e.printStackTrace();
            // }

            // if (!System.getProperty("os.name").toLowerCase().Contains("windows")) {
            //     // allow the web interface to handle files
            //     HashSet<PosixFilePermission> perms = Files.readAttributes(fileCModel, PosixFileAttributes.class).permissions();
            //     perms.Add(PosixFilePermission.OWNER_WRITE);
            //     perms.Add(PosixFilePermission.OWNER_READ);
            //     perms.Add(PosixFilePermission.OWNER_EXECUTE);
            //     perms.Add(PosixFilePermission.GROUP_WRITE);
            //     perms.Add(PosixFilePermission.GROUP_READ);
            //     perms.Add(PosixFilePermission.GROUP_EXECUTE);
            //     perms.Add(PosixFilePermission.OTHERS_WRITE);
            //     perms.Add(PosixFilePermission.OTHERS_READ);
            //     perms.Add(PosixFilePermission.OTHERS_EXECUTE);
            //     Files.setPosixFilePermissions(fileCModel, perms);
            // }

            // Main.saveModel(outputModelsupclasses, "/srv/www/htdocs/demo_conception/pictures_uml/subclasses.ttl",
            //         RDFFormat.TTL);
            // Main.saveModel(outputModelrelations, "/srv/www/htdocs/demo_conception/pictures_uml/relation.ttl",
            //         RDFFormat.TTL);

            // log.debug("outputModelsupclasses exists: " + Helpers.isFileExists("/srv/www/htdocs/demo_conception/pictures_uml/subclasses.ttl"));
            // log.debug("outputModelrelations exists: " + Helpers.isFileExists("/srv/www/htdocs/demo_conception/pictures_uml/relation.ttl"));
            // log.debug("leaving CreateTxtFile.");
        }
        private int getKey(Dictionary<int, string> db, string value)
        {
            foreach (int key in db.Keys)
            {
                if (db[key].Equals(value))
                {
                    return key; // return the first found
                }
            }
            // return (Integer) null;
            return default(int);
        }

        public HashSet<String> findSubclassAll(String c)
        {
            HashSet<String> sub = new HashSet<String>();
            String superclass = "";
            String requete1 = "PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#> " + "SELECT * WHERE { " + "<" + c
                    + "> rdfs:subClassOf* ?superClass . " + " } ";
            var results1 = (SparqlResultSet)ds.ontology.ExecuteQuery(requete1);
            foreach (var soln1 in results1)
            {
                superclass = soln1["superClass"].ToString();
                sub.Add(superclass);
            }
            // Query q1 = QueryFactory.create(requete1);
            // QueryExecution qexe1 = QueryExecutionFactory.create(q1, dbpOnt);
            // ResultSet results1 = qexe1.execSelect();
            // while (results1.hasNext())
            // {
            //     QuerySolution soln1 = results1.nextSolution();
            //     superclass = soln1.get("?superClass").toString();
            //     sub.Add(superclass);
            // }
            return sub;
        }

        public HashSet<String> findSubclass(String c)
        {
            HashSet<String> sub = new HashSet<String>();
            String superclass = "";
            String requete1 = "PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#> " + "SELECT * WHERE { " + "<" + c
                    + "> rdfs:subClassOf ?superClass . " + " } ";
            var results1 = (SparqlResultSet)ds.ontology.ExecuteQuery(requete1);
            foreach (var soln1 in results1)
            {
                superclass = soln1["superClass"].ToString();
                sub.Add(superclass);
            }
            // Query q1 = QueryFactory.create(requete1);
            // QueryExecution qexe1 = QueryExecutionFactory.create(q1, dbpOnt);
            // ResultSet results1 = qexe1.execSelect();
            // while (results1.hasNext())
            // {
            //     QuerySolution soln1 = results1.nextSolution();
            //     superclass = soln1.get("?superClass").toString();
            //     sub.Add(superclass);
            // }
            return sub;
        }

        public async Task<string> FindDomain(String p3)
        {
            Dictionary<String, int> typeMap = new Dictionary<String, int>();
            var subjectsTmp = await ds.GetSubjects(p3, "");
            // IteratorTripleString it = hdt.search("", p3, "");

            // // We get all subjects of the wanted type first.
            // HashSet<String> subjectsTmp = new HashSet<string>();
            // while (it.hasNext())
            // {
            //     TripleString ts = it.next();
            //     String s = ts.getSubject().toString();
            //     subjectsTmp.Add(s);
            // }
            var predicatesBySubject = subjectsTmp.AsParallel().Select(subject =>
            {
                var setTmp = ds.GetObjects(subject, OntologyHelper.PropertyType).Result;
                return new { subject, setTmp };
            }).ToDictionary(x => x.subject, x => x.setTmp);
            // subjectsTmp.parallelStream().forEach((subject)-> {
            //     IteratorTripleString iter = null;
            //     try
            //     {
            //         // TODO: adapt property if Wikidata is selected
            //         iter = hdt.search(subject, "http://www.w3.org/1999/02/22-rdf-syntax-ns#type", "");
            //     }
            //     catch (NotFoundException e)
            //     {
            //         e.printStackTrace();
            //     }
            //     Set<String> setTmp = new HashSet<>();
            //     while (iter.hasNext())
            //     {
            //         TripleString ts = iter.next();
            //         String p = ts.getObject().toString();
            //         setTmp.add(p);
            //     }
            //     predicatesBySubject.put(subject, setTmp);
            // });
            int big = 0;
            int c = 0;
            String ttt = "";
            foreach (var entry in predicatesBySubject)//.entrySet())
            {
                var types = entry.Value.Where(x => x.Contains("dbpedia") &&
                    !x.Contains("Wiki")).ToHashSet();
                // Set<String> types = entry.getValue().stream().filter((t)->t.contains("dbpedia") && !t.contains("Wiki"))
                //         .collect(Collectors.toSet());
                foreach (String type in types)
                {
                    if (typeMap.ContainsKey(type))
                    {
                        c = typeMap.GetValueOrDefault(type);
                        c++;
                        typeMap.Add(type, c);
                        if (big < c)
                            big = c;
                    }
                    else
                    {
                        typeMap.Add(type, 1);
                    }
                }
            }
            var oo = new HashSet<object>();
            oo = getKeyFromValue(typeMap, big);
            foreach (Object obj in oo)
                ttt = obj.ToString();
            return ttt;
        }

        public static HashSet<Object> getKeyFromValue(Dictionary<String, int> hm, Object value)
        {
            var lo = new HashSet<Object>();
            foreach (Object o in hm.Keys)
            {
                if (hm.GetValueOrDefault(o.ToString()).Equals(value))
                {
                    lo.Add(o);
                }
            }
            return lo;
        }

        public async Task<String> FindRange(String p4)
        {
            var typeMap = new Dictionary<String, int>();
            // IteratorTripleString it = hdt.search("", p4, "");
            var objectsTmp = await ds.GetObjects("", p4);
            // while (it.hasNext())
            // {
            //     TripleString ts = it.next();
            //     String s = ts.getObject().toString();
            //     objectsTmp.add(s);
            // }
            // final Map<String, Set< String >> predicatesByObject = new ConcurrentHashMap<>();
            var predicatesBySubject = objectsTmp.AsParallel().Select(obj =>
            {
                var setTmp = ds.GetObjects(obj, OntologyHelper.PropertyType).Result;
                return new { subject = obj, setTmp };
            }).ToDictionary(x => x.subject, x => x.setTmp);
            // objectsTmp.parallelStream().forEach((object) -> {
            //     IteratorTripleString iter = null;
            //     try
            //     {
            //         // TODO: adapt property if Wikidata is selected
            //         iter = hdt.search(object, "http://www.w3.org/1999/02/22-rdf-syntax-ns#type", "");
            //     }
            //     catch (NotFoundException e)
            //     {
            //         e.printStackTrace();
            //     }
            //     Set<String> setTmp = new HashSet<>();
            //     while (iter.hasNext())
            //     {
            //         TripleString ts = iter.next();
            //         String p = ts.getObject().toString();
            //         setTmp.add(p);
            //     }
            //     predicatesByObject.put(object, setTmp);
            // });

            int big = 0;
            int c = 0;
            String ttt = "";
            foreach (var entry in predicatesBySubject)
            {
                var types = entry.Value.Where(x => x.Contains("dbpedia") &&
                    !x.Contains("Wiki")).ToHashSet();
                // Set<String> types = entry.getValue().stream().filter((t)->t.contains("dbpedia") && !t.contains("Wiki"))
                //         .collect(Collectors.toSet());
                foreach (String type in types)
                {
                    if (typeMap.ContainsKey(type))
                    {
                        c = typeMap.GetValueOrDefault(type);
                        c++;
                        typeMap.Add(type, c);
                        if (big < c)
                            big = c;
                    }
                    else
                    {
                        typeMap.Add(type, 1);
                    }
                }
            }
            var oo = new HashSet<Object>();
            oo = getKeyFromValue(typeMap, big);
            foreach (Object obj in oo)
                ttt = obj.ToString();
            return ttt;
        }

    }
}