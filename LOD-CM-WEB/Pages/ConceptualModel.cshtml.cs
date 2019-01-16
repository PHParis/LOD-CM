using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Linq;
using LOD_CM_CLI.Data;
using Newtonsoft.Json;
using System;
using LOD_CM_CLI;

namespace LOD_CM.Pages
{
    public class ConceptualModel : PageModel
    {
        //public DatasetForIndex Dataset{ get; private set; }
        public string ImageContent { get; private set; }
        // public IList<string> Properties { get; private set; }
        public string ErrorMessage { get; private set; }
        [HiddenInput]
        public string ClassName { get; private set; }

        [HiddenInput]
        public int Threshold { get; private set; }

        [HiddenInput]
        public string DatasetLabel { get; private set; }

        public List<string> images { get; private set; }
        // public List<HashSet<InstanceLabel>> classes { get; private set; }
        public List<HashSet<InstanceLabel>> properties { get; private set; }
        // public List<List<(string[] props, int supp)>> mfpsList { get; private set; }

        public int Selection { get; private set; }
        public async Task OnGetAsync(DatasetForIndex dataset)
        {
            if (dataset.Threshold < 50)
                dataset.Threshold = 50;
            if (dataset.Threshold > 100)
                dataset.Threshold = 100;
            ErrorMessage = string.Empty;
            Selection = 0;
            var mainDir = Program.mainDir;
            var classDir = Path.Combine(mainDir, dataset.Label,
                dataset.Class);
            var directory = Path.Combine(classDir, dataset.Threshold.ToString());
            var dictionaryContent = await System.IO.File.ReadAllLinesAsync(Path.Combine(classDir, "dictionary.txt"));
            var dictionary = dictionaryContent.Select(x => x.Split(" ", StringSplitOptions.RemoveEmptyEntries))
                .Where(x => x.Length == 2).Select(array =>
                    new { id = Convert.ToInt32(array[0]), uri = array[1] })
                .ToDictionary(x => x.id, x => x.uri);
            images = new List<string>();
            properties = new List<HashSet<InstanceLabel>>();
            if (Directory.Exists(directory))
            {
                var files = Directory.GetFiles(directory);
                foreach (var file in files.OrderBy(x => x))
                {
                    if (file.Contains("plant_"))
                    {
                        var imagePath = file.Replace("plant_", "img_").Replace(".txt", ".svg");
                        if (!System.IO.File.Exists(imagePath))
                        {
                            // if image doesn't exist, we must create it before retrieving its content!
                            try
                            {
                                var contentForUml = await System.IO.File.ReadAllTextAsync(file);
                                #if DEBUG
                                var sw = System.Diagnostics.Stopwatch.StartNew();
                                #endif
                                var svgFileContent = await LOD_CM_CLI.Uml.ImageGenerator.GetImageContent(contentForUml, Program.plantUmlJarPath, Program.localGraphvizDotPath);
                                #if DEBUG
                                sw.Stop();
                                System.Console.WriteLine(sw.Elapsed.ToPrettyFormat());
                                #endif
                                await System.IO.File.WriteAllTextAsync(imagePath, svgFileContent);
                            }
                            catch (Exception ex)
                            {
                                ErrorMessage += ex;
                            }
                        }
                        var imageContent = (await System.IO.File.ReadAllTextAsync(imagePath)).Replace(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""no""?>", string.Empty);
                        images.Add(imageContent);
                    }
                    else if (file.EndsWith("json") && file.Contains("usedProperties_"))
                    {
                        var json = await System.IO.File.ReadAllTextAsync(file);
                        var props = JsonConvert.DeserializeObject<HashSet<InstanceLabel>>(json);
                        properties.Add(props);
                    }
                }
                ImageContent = images.FirstOrDefault();
                ClassName = dataset.Class;
                Threshold = dataset.Threshold;
                DatasetLabel = dataset.Label;
            }
        }
    }
}