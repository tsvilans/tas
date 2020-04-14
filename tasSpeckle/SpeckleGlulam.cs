using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpeckleCore;
using SpeckleCoreGeometryClasses;
using Newtonsoft.Json;

namespace SpeckleGlulamClasses
{
    [Serializable]
    public partial class SpeckleGlulam: SpeckleObject
    {
        [JsonProperty("type", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public override string Type { get => "Glulam"; set => base.Type = value; }

        [JsonProperty("centreline", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public SpeckleCurve Centreline { get; set; }

        [JsonProperty("glulam_type", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public string GlulamType { get; set; }

        [JsonProperty("lamella_height", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public double LamellaHeight { get; set; }

        [JsonProperty("lamella_width", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public double LamellaWidth { get; set; }

        [JsonProperty("num_lamella_height", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public int NumLamellaHeight { get; set; }

        [JsonProperty("num_lamella_width", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public int NumLamellaWidth { get; set; }

        [JsonProperty("interpolation_type", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public int InterpolationType { get; set; }

        [JsonProperty("section_alignment", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public int SectionAlignment { get; set; }

        [JsonProperty("frames", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public List<SpecklePlane> Frames { get; set; }
    }
}
