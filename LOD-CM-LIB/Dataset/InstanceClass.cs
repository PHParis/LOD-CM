using System;
using System.Linq;
using LOD_CM_CLI.Utils;

namespace LOD_CM_CLI.Data
{
    /// <summary>
    /// Contains information about an OWL or RDF class
    /// </summary>
    public class InstanceClass
    {
        /// <summary>
        /// Use the propertyForLabel property to get label of this instance.
        /// If propertyForLabel is null, then use fragment to get the label
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="propertyForLabel"></param>
        /// <param name="ds"></param>
        public InstanceClass(string uri, string propertyForLabel, Dataset ds)
        {
            this.Uri = uri;
            if (propertyForLabel != null)
            {
                throw new NotImplementedException();
                var labels = ds.GetObjects(uri, propertyForLabel)
                    .Result.Distinct().ToList();
                if (!labels.Any())
                    this.Label = uri.GetUriFragment();
                else
                {
                    if (labels.Any(x => x.EndsWith("en")))
                    {
                        this.Label = labels.Where(x => x.EndsWith("en")).FirstOrDefault();
                    }
                    else
                    {
                        this.Label = labels.FirstOrDefault();
                    }
                    this.Label = this.Label.ToCamelCaseAlphaNum();
                }                
            }
            else
            {
                this.Label = uri.GetUriFragment();
            }            
        }

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