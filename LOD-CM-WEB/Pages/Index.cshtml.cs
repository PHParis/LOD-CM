﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LOD_CM_CLI.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;

namespace LOD_CM.Pages
{
    public class IndexModel : PageModel
    {
        public async Task OnGetAsync()
        {
            ThresholdRanges = Enumerable.Range(1, 100).ToList();
            DatasetNames = await System.IO.File.ReadAllLinesAsync(
                Path.Combine(Program.mainDir, "datasets.txt")
            );
            ClassesNames = new List<IList<string>>();
            foreach (var datasetName in DatasetNames)
            {
                var dsDir = Path.Combine(Program.mainDir, datasetName);
                if (Directory.Exists(dsDir))
                {
                    var dsInfoContent = await System.IO.File.ReadAllTextAsync(
                        Path.Combine(dsDir, "dataset.json")
                    );
                    var dsInfo = JsonConvert.DeserializeObject<Dataset>(dsInfoContent);
                    ClassesNames.Add(dsInfo.classes
                        .Select(x => x.Value.Label)
                        .OrderBy(x => x)
                        .ToList());
                }
            }
            #if DEBUG
            ClassesNames.Add(new[]
            {
                "WikiTest1",
                "WikiTest2"
            });
            #endif
            // ClassesNames = new[]
            //         {
            //             new[]
            //             {
            //                 "Person",
            //                 "Film"
            //             },
            //             new[]
            //             {
            //                 "Entity1",
            //                 "Entity2"
            //             },
            //         };
        }

        [BindProperty]
        public DatasetForIndex Dataset { get; set; }
        public IList<int> ThresholdRanges { get; private set; }
        public IList<string> DatasetNames { get; private set; }
        public IList<IList<string>> ClassesNames { get; private set; }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return RedirectToPage("/Index");
            }
            return RedirectToPage("/ConceptualModel", Dataset);
        }
    }

    public class DatasetForIndex
    {
        [Required]
        public string Label { get; set; }
        [Required]
        public string Class { get; set; }
        [Required]
        public int Threshold { get; set; }
    }
}
