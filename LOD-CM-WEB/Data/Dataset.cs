using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LOD_CM.Data
{
    public class Dataset
    {
        public string Label { get; set; }
        public ICollection<string> Classes { get; set; }
        public ICollection<int> ThresholdRanges { get; set; }
        // [Required]
        public int CurrentThreshold { get; set; }
        public ICollection<string> ImagesContent { get; set; }
        // [Required]
        public string CurrentImage { get; set; }
        public ICollection<ICollection<ICollection<string>>> MFPs { get; set; }
        // [Required]
        public ICollection<string> CurrentMFP { get; set; }
    }
}