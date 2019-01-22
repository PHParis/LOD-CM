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
    }
}