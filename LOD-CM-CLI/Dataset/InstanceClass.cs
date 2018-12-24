namespace LOD_CM_CLI.Data
{
    /// <summary>
    /// Contains information about an OWL or RDF class
    /// </summary>
    public class InstanceClass
    {
        /// <summary>
        /// The URI of the given class.
        /// </summary>
        /// <value></value>
        public string Uri { get; set; }

        /// <summary>
        /// The label of the property. It is used to display a human reading
        /// name in Web interface or to be part of file name.
        /// It must contain only alpha numeric characters.
        /// </summary>
        /// <value></value>
        public string Label { get; set; }
    }
}