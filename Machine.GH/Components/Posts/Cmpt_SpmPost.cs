using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;

using Rhino.Geometry;

using tas.Machine.Posts;
using GH_IO.Serialization;
using tas.Core.Types;

namespace tas.Machine.GH.Posts
{
    public class Cmpt_PostSpm : GH_Component
    {

        public Cmpt_PostSpm()
          : base("Post (SPM)", "PostSPM",
              "Postprocess toolpaths to CNC machine using SPM post-processor file.",
              "tasMachine", UiNames.OutputSection)
        {
        }

        private string CurrentPost = "";
        private SpmPost Post = null;

        protected override System.Drawing.Bitmap Icon => Properties.Resources.tasMachine_Post;
        public override Guid ComponentGuid => new Guid("E8055291-677E-488F-9419-E792D7B8C480");
        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("SPM path", "SPM", "Path to .spm postprocessor file.", GH_ParamAccess.item);
            pManager.AddGenericParameter(
                "Toolpaths", "TP", "Toolpaths as a list.", GH_ParamAccess.list);
            pManager.AddPlaneParameter(
                "Workplane", "WP", "Optional workplane for all targets.", GH_ParamAccess.item);
            pManager.AddTextParameter("Output path", "O", "File path to save the G-code to, without file type extension.", GH_ParamAccess.item, "C:/tmp/tas.Machine");

            pManager[2].Optional = true;
            pManager[3].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("G-code", "G", "Output G-code as a list of individual lines.", GH_ParamAccess.list);
            pManager.AddGenericParameter("Path", "P", "Output toolpath.", GH_ParamAccess.item);
            //pManager.AddTextParameter("debug", "d", "debug info", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string postPath = "";
            string outputPath = "C:/tmp/tas.Machine";
            List<Toolpath> tpIn = new List<Toolpath>();
            Plane workplane = Plane.WorldXY;


            DA.GetData("SPM path", ref postPath);
            var postName = System.IO.Path.GetFileNameWithoutExtension(postPath);
            this.Message = postName;


            DA.GetData("Output path", ref outputPath);

            DA.GetDataList("Toolpaths", tpIn);
            DA.GetData("Workplane", ref workplane);

            List<Toolpath> toolpaths = tpIn.Select(x => x.Duplicate()).ToList();

            if (postName != CurrentPost)
            {
                CurrentPost = postName;
                Post = new SpmPost(postPath);
            }

            Post.ClearErrors();

            Post.Workplane = workplane;

            var doc = OnPingDocument();
            if (doc != null)
            {
                Post.Author = doc.Author.Name;
                Post.Name = doc.DisplayName;
            }
            else
            {
                Post.Author = "an unknown Rhino user";
                Post.Name = "an unknown Grasshopper file";
            }

            var code = (Post.Post(outputPath, toolpaths) as List<string>).Select(x => new GH_String(x));

            if (!string.IsNullOrEmpty(Post.ErrorMessage))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, Post.ErrorMessage);
            }

            DA.SetDataList("G-code", code);
            DA.SetDataList("Path", Post.Toolpaths.Select(x => new GH_Toolpath(x)));
        
        }
    }
}