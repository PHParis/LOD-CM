using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Linq;
using LOD_CM_CLI.Data;
using Newtonsoft.Json;
using System;

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
            // TODO: display several MFP and let user choose
            Selection = 0;
            var mainDir = Program.mainDir;//@"E:\download";
            var classDir = Path.Combine(mainDir, dataset.Label,
                dataset.Class);
            var directory = Path.Combine(classDir, dataset.Threshold.ToString());
            var dictionaryContent = await System.IO.File.ReadAllLinesAsync(Path.Combine(classDir, "dictionary.txt"));
            var dictionary = dictionaryContent.Select(x => x.Split(" ", StringSplitOptions.RemoveEmptyEntries))
                .Where(x => x.Length == 2).Select(array =>
                    new { id = Convert.ToInt32(array[0]), uri = array[1] })
                .ToDictionary(x => x.id, x => x.uri);
            images = new List<string>();
            // classes = new List<HashSet<InstanceLabel>>();
            properties = new List<HashSet<InstanceLabel>>();
            // mfpsList = new List<List<(string[] props, int supp)>>();
            if (Directory.Exists(directory))
            {
                var files = Directory.GetFiles(directory);
                foreach (var file in files.OrderBy(x => x))
                {
                    if (file.EndsWith("svg"))
                    {
                        var imageContent = (await System.IO.File.ReadAllTextAsync(file)).Replace(@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""no""?>", string.Empty);
                        images.Add(imageContent);
                    }
                    // else if (file.EndsWith("json") && file.Contains("usedClasses_"))
                    // {
                    //     var json = await System.IO.File.ReadAllTextAsync(file);
                    //     var cls = JsonConvert.DeserializeObject<HashSet<InstanceLabel>>(json);
                    //     classes.Add(cls);
                    // }
                    else if (file.EndsWith("json") && file.Contains("usedProperties_"))
                    {
                        var json = await System.IO.File.ReadAllTextAsync(file);
                        var props = JsonConvert.DeserializeObject<HashSet<InstanceLabel>>(json);
                        properties.Add(props);
                    }
                    // else if (file.EndsWith("mfp.txt"))
                    // {
                    //     var content = await System.IO.File.ReadAllLinesAsync(file);
                    //     var mfps = new List<(string[] props, int supp)>();
                    //     foreach (var line in content.Where(x => !string.IsNullOrWhiteSpace(x)))
                    //     {
                    //         var array = line.Split(" #SUP: ", StringSplitOptions.RemoveEmptyEntries);
                    //         if (array.Length != 2) continue;
                    //         var support = Convert.ToInt32(array[1]);
                    //         var mfp = array[0].Split(" ", StringSplitOptions.RemoveEmptyEntries).Select(x =>
                    //             Convert.ToInt32(x)).ToArray();
                    //         mfps.Add((mfp.Select(x => dictionary[x]).ToArray(), support));
                    //     }
                    //     mfpsList.Add(mfps);
                    // }
                }
            }
            // Dataset = dataset;
            ImageContent = images.First();
            // Properties = new[]
            // {
            //     "type", "director"
            // };
            ErrorMessage = null;//$"Label: {dataset.Label} // Class: {dataset.Class} // Threshold: {dataset.Threshold}";
            ClassName = dataset.Class;
            Threshold = dataset.Threshold;
            DatasetLabel = dataset.Label;
        }
    }
}