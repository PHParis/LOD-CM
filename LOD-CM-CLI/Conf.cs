using LOD_CM_CLI.Data;

namespace LOD_CM_CLI
{
    public class Conf
    {
        public string LocalGraphvizDotPath { get; set; }

        /// <summary>
        /// Directory where files will be saved
        /// </summary>
        /// <value></value>
        public string mainDir { get; set; }
        /// <summary>
        /// informations about datasets
        /// </summary>
        /// <value></value>
        public Dataset[] datasets { get; set; }
        /// <summary>
        /// If true, the program stops after precomputation of each dataset,
        /// if false it will run until the end
        /// </summary>
        /// <value></value>
        public bool precomputationOnly { get; set; }
        public string plantUmlJarPath { get; set; }

        
        /// <summary>
        /// If true, get properties from ontology.false If false, get them from dataset.
        /// </summary>
        /// <value></value>
        public bool getPropertiesFromOntology { get; set; } = true;
        
        
        /// <summary>
        /// hard encoding of used classes. If provided, the program doesn't try to compute classes that are in the dataset
        /// </summary>
        /// <value></value>
        public string[] classesToCompute { get; set; }
    }
}