using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using LOD_CM.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LOD_CM.Pages
{
    public class IndexModel : PageModel
    {
        public void OnGet()
        {
            // Datasets = new[]
            // {
            //     new Dataset
            //     {
            //         Label = "DBpedia",
            //         Classes = new[]
            //         {
            //             "Person",
            //             "Film"
            //         },
            //         ImagesContent = new[]
            //         {
            //             ""
            //         },
            //         MFPs = new[]
            //         {
            //             new[]
            //             {
            //                 new[]
            //                 {
            //                     "type",
            //                     "name"
            //                 },
            //                 new[]
            //                 {
            //                     "birthPlace",
            //                     "name"
            //                 }
            //             },
            //             new[]
            //             {
            //                 new[]
            //                 {
            //                     "type",
            //                     "director"
            //                 },
            //                 new[]
            //                 {
            //                     "starring",
            //                     "name"
            //                 }
            //             }
            //         },
            //         ThresholdRanges = Enumerable.Range(1, 100).ToList()
            //     }
            // };
            // TODO: provide real data
            ThresholdRanges = Enumerable.Range(1, 100).ToList();
            DatasetNames = new[] { "DBpedia", "Wikidata" };
            ClassesNames = new[]
                    {
                        new[]
                        {
                            "Person",
                            "Film"
                        },
                        new[]
                        {
                            "Entity1",
                            "Entity2"
                        },
                    };
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
