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
// using Iternity.PlantUML;
using System.Net.Http;
using LOD_CM_CLI.Utils;
using LOD_CM_CLI.Mining;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using PlantUml.Net;

namespace LOD_CM_CLI.Uml
{
    public class ImageGenerator
    {
        private ILogger log;

        private ImageGenerator() { }

        // /// <summary>
        // /// Key is the property id, value is its support
        // /// </summary>
        // /// <value></value>
        // public Dictionary<int, int> propertyMinsup { get; private set; }

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

        // /// <summary>
        // /// the key is a class uri and value contains all its direct super class,
        // /// i.e. there is a gap of only one between classes from value and the key.
        // /// For example, Actor will have only Artist (not Person because Person 
        // /// is too far from Actor (distance of 2))
        // /// </summary>
        // private Dictionary<string, HashSet<string>> superClassesOfClass;
        public FrequentPattern<int> fp { get; private set; }
        public string plantUmlJarPath { get; private set; }
        public string localGraphvizDotPath { get; private set; }

        /// <summary>
        /// Download SVG file content from PlantUML
        /// </summary>
        /// <returns></returns>
        public async Task<bool> GetImageContent()
        {
            try
            {
                svgFileContent = await GetImageContent(contentForUml, plantUmlJarPath, localGraphvizDotPath);
            }
            catch (Exception ex)
            {
                log.LogError($"{fp.transactions.instanceClass.Label} ({fp.minSupport}): {ex.Message}");
            }
            return false;
        }

        public static async Task<string> GetImageContent(string contentForUml, string plantUmlJarPath, string localGraphvizDotPath)
        {            
            var factory = new RendererFactory();
            var renderer = factory.CreateRenderer(new PlantUmlSettings
            {
                RenderingMode = RenderingMode.Local,
                LocalPlantUmlPath = plantUmlJarPath,
                LocalGraphvizDotPath = localGraphvizDotPath
            });
            var bytes = renderer.Render(contentForUml, OutputFormat.Svg);
            return System.Text.Encoding.UTF8.GetString(bytes);
            // var uri = PlantUMLUrl.SVG(contentForUml);
            // string svgFileContent;
            // using (var client = new HttpClient())
            // {
            //     svgFileContent = await client.GetStringAsync(uri);
            // }
            // return svgFileContent;
        }

        public async Task SaveImage(string filePath)
        {
            await File.WriteAllTextAsync(filePath, svgFileContent);
        }
        public async Task SaveContentForPlantUML(string filePath)
        {
            await File.WriteAllTextAsync(filePath, contentForUml);
        }

        /// <summary>
        /// Return strings representing a class hierarchy for PlantUML.
        /// For example, Person <|-- Agent.
        /// </summary>
        /// <param name="classUri"></param>
        /// <returns></returns>
        public IEnumerable<string> GetAllSuperClasses(string classUri)
        {
            if ("http://www.w3.org/2002/07/owl#Thing".Equals(classUri))
                yield return string.Empty;
            else if (!fp.transactions.dataset.superClassesOfClass.ContainsKey(classUri))
                yield return string.Empty;
            else
            {
                var set = fp.transactions.dataset.superClassesOfClass[classUri];
                if (!set.Any()) yield return string.Empty;
                else
                {
                    foreach (var superClass in set)
                    {
                        var c = classUri.GetUriFragment();
                        var sc = superClass.GetUriFragment();
                        yield return sc + " <|-- " + c;
                        foreach (var res in GetAllSuperClasses(superClass))
                        {
                            yield return res;
                        }
                    }
                }
            }
        }


        public static async Task<List<ImageGenerator>> GenerateTxtForUml(Dataset ds,
            InstanceLabel instanceClass, double threshold,
            FrequentPattern<int> fp, ServiceProvider serviceProvider, string plantUmlJarPath, string localGraphvizDotPath)
        {
            var thresholdInt = Convert.ToInt32(threshold * 100);
            var maximalSets = fp.fis.Where(x => x.IsMaximal).OrderByDescending(x => x.Count)
                .ThenByDescending(x => x.TransactionCount).ToList();
            var finalResults = new List<ImageGenerator>();
            foreach (var mfp in maximalSets)
            {
                var result = new ImageGenerator();
                result.log = serviceProvider.GetService<ILogger<ImageGenerator>>();
                result.fp = fp;
                result.plantUmlJarPath = plantUmlJarPath;
                result.localGraphvizDotPath = localGraphvizDotPath;
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
                    if (!string.IsNullOrWhiteSpace(line))
                        cModel.AppendLine(line);
                }
                // loop for related classes hierarchy
                foreach (var classUri in classes)
                {
                    var c = classUri.GetUriFragment();
                    foreach (var superClass in fp.transactions.dataset.superClassesOfClass[classUri])
                    {
                        var sc = superClass.GetUriFragment();
                        cModel.AppendLine(sc + " <|-- " + c);
                    }
                }
                cModel.AppendLine("@enduml");
                result.contentForUml = cModel.ToString();
            }
            return finalResults;
        }

    }
}