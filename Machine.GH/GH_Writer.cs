using GH_IO.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tas.Machine.GH
{
    public static class GH_Writer
    {
        #region WRITING
        static public bool Write(GH_IWriter writer, ToolSettings t)
        {
            writer.SetDouble("ToolDiameter", t.ToolDiameter);
            writer.SetDouble("StepDown", t.StepDown);
            writer.SetDouble("StepOver", t.StepOver);
            return true;
        }

        /*
        static public bool Write(GH_IWriter writer, RobotInfo r)
        {
            writer.SetString("RobotName", r.RobotName);
            writer.SetString("TaskName", r.TaskName);
            writer.SetString("ModuleName", r.ModuleName);
            writer.SetString("LocalFolder", r.LocalFolder);

            return true;
        }
        */
        #endregion

        #region READING
        static public bool Read(GH_IReader reader, ref ToolSettings t)
        {
            t.ToolDiameter = reader.GetDouble("ToolDiameter");
            t.StepDown = reader.GetDouble("StepDown");
            t.StepOver = reader.GetDouble("StepOver");
            return true;
        }
        /*
        static public bool Read(GH_IReader reader, ref RobotInfo r)
        {
            r.RobotName = reader.GetString("RobotName");
            r.TaskName = reader.GetString("TaskName");
            r.ModuleName = reader.GetString("ModuleName");
            r.LocalFolder = reader.GetString("LocalFolder");

            return true;
        }
        */
        #endregion
    }
}
