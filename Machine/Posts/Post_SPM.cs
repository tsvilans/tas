using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace tas.Machine.Posts
{
    public class SpmPost
    {
        public BoundingBox Stock = BoundingBox.Unset;

        // General tolerance value
        public double Epsilon = 1e-05;
        public Plane Workplane = Plane.WorldXY;
        public List<Toolpath> Toolpaths = null;
        public string Name = "unknown";
        public string Author = "unknown";
        public string ErrorMessage { get; private set; }
        public bool Loaded { get; private set; }

        #region SPM input variables
        public double INFORMATION_Version { get; private set; }
        public double INFORMATION_MacroVersion { get; private set; }

        public string GENERAL_Extension { get; private set; }
        public string GENERAL_StartReadingChar { get; private set; }
        public string GENERAL_StopReadingChar { get; private set; }
        public double GENERAL_UseSequencNo { get; private set; }
        public string GENERAL_PrefixLetter { get; private set; }
        public double GENERAL_Increment { get; private set; }
        public double GENERAL_SequenceStartNo { get; private set; }
        public double GENERAL_ShowLeadingZeros { get; private set; }
        public double GENERAL_LeadingZerosNumOfDigit { get; private set; }
        public string GENERAL_Mode { get; private set; }
        public string GENERAL_AbsCode { get; private set; }
        public string GENERAL_IncCode { get; private set; }
        public string GENERAL_AbsCenterCode { get; private set; }
        public string GENERAL_Units { get; private set; }
        public string GENERAL_InchCode { get; private set; }
        public string GENERAL_MetricCode { get; private set; }
        public string GENERAL_ModalGCode { get; private set; }
        public string GENERAL_ModalXYZ { get; private set; }
        public string GENERAL_ModalFeedrate { get; private set; }
        public string GENERAL_ModalSpindle { get; private set; }
        public int GENERAL_Delimiter { get; private set; }
        public string GENERAL_UserDefinedDelimiter { get; private set; }
        public string GENERAL_EndBlockCharacter { get; private set; }
        public string GENERAL_OutputPlusSign { get; private set; }
        public string GENERAL_CommentOutput { get; private set; }
        public string GENERAL_CommentStartChar { get; private set; }
        public string GENERAL_CommentEndChar { get; private set; }
        public string GENERAL_CommentSequenceMode { get; private set; }
        public string GENERAL_XRegister { get; private set; }
        public string GENERAL_YRegister { get; private set; }
        public string GENERAL_ZRegister { get; private set; }

        public string MOTION_BlockPrefix { get; private set; }
        public string MOTION_LinearMotionCode { get; private set; }
        public string MOTION_RapidMotionCode { get; private set; }
        public string MOTION_BlockPostfix { get; private set; }
        public string MOTION_Order { get; private set; }
        public double MOTION_RapidFormat { get; private set; }
        public double MOTION_ScaleFactorX { get; private set; }
        public double MOTION_ScaleFactorY { get; private set; }
        public double MOTION_ScaleFactorZ { get; private set; }
        public int MOTION_NumOfDecimalPlaces { get; private set; }
        public string MOTION_LinearBlock { get; private set; }
        public int MOTION_ShowMotionTrailingZeros { get; private set; }
        public string MOTION_RapidBlock { get; private set; }

        public string CIRCLE_ClockwiseArcCode { get; private set; }
        public string CIRCLE_CClockwiseArcCode { get; private set; }
        public string CIRCLE_XYPlaneCode { get; private set; }
        public string CIRCLE_YZPlaneCode { get; private set; }
        public string CIRCLE_ZXPlaneCode { get; private set; }
        public string CIRCLE_BlockXY { get; private set; }
        public string CIRCLE_BlockZX { get; private set; }
        public string CIRCLE_BlockYZ { get; private set; }
        public double CIRCLE_ArcCenterCoordinate { get; private set; }
        public string CIRCLE_Modal { get; private set; }
        public string CIRCLE_Format { get; private set; }
        public double CIRCLE_SignedRadius { get; private set; }
        public double CIRCLE_SwitchPlanes { get; private set; }
        public double CIRCLE_LimitArcs { get; private set; }
        public double CIRCLE_LimitAngle { get; private set; }
        public string CIRCLE_IRegister { get; private set; }
        public string CIRCLE_JRegister { get; private set; }
        public string CIRCLE_KRegister { get; private set; }
        public string CIRCLE_RRegister { get; private set; }

        public string HELIXSPIRAL_ClockwiseArcCodeHelix { get; private set; }
        public string HELIXSPIRAL_CClockwiseArcCodeHelix { get; private set; }
        public string HELIXSPIRAL_ClockwiseArcCodeSpiral { get; private set; }
        public string HELIXSPIRAL_CClockwiseArcCodeSpiral { get; private set; }
        public string HELIXSPIRAL_BlockXYHelix { get; private set; }
        public string HELIXSPIRAL_BlockZXHelix { get; private set; }
        public string HELIXSPIRAL_BlockYZHelix { get; private set; }
        public string HELIXSPIRAL_BlockXYSpiral { get; private set; }
        public string HELIXSPIRAL_BlockZXSpiral { get; private set; }
        public string HELIXSPIRAL_BlockYZSpiral { get; private set; }

        public string GENERALMOTION_ClockwiseRotation { get; private set; }
        public string GENERALMOTION_CClockwiseRotation { get; private set; }
        public string GENERALMOTION_Aaxis { get; private set; }
        public string GENERALMOTION_Baxis { get; private set; }
        public string GENERALMOTION_Caxis { get; private set; }
        public double GENERALMOTION_ScaleFactor { get; private set; }
        public double GENERALMOTION_ShowTrailingZeros { get; private set; }
        public double GENERALMOTION_NumOfDecimalPlaces { get; private set; }
        public double GENERALMOTION_RestrictToPositiveAngles { get; private set; }
        public string GENERALMOTION_LinearBlock { get; private set; }
        public string GENERALMOTION_RapidBlock { get; private set; }

        public string SETUP_Setup0Block { get; private set; }
        public string SETUP_Setup1Block { get; private set; }

        public string SPINDLE_BlockPrefix { get; private set; }
        public string SPINDLE_Code { get; private set; }
        public string SPINDLE_BlockPostfix { get; private set; }
        public string SPINDLE_ClockwiseRotationCode { get; private set; }
        public string SPINDLE_CClockwiseRotationCode { get; private set; }
        public string SPINDLE_OffCode { get; private set; }
        public double SPINDLE_HighValue { get; private set; }
        public double SPINDLE_LowValue { get; private set; }
        public double SPINDLE_ScaleFactor { get; private set; }
        public int SPINDLE_NumOfDecimalPlaces { get; private set; }
        public string SPINDLE_Block { get; private set; }
        public int SPINDLE_ShowSpindleTrailingZeros { get; private set; }
        public double SPINDLE_ConstantSurfaceSpeed { get; private set; }
        public double SPINDLE_ConstantRotationSpeed { get; private set; }

        public string FEEDRATE_BlockPrefix { get; private set; }
        public string FEEDRATE_Code { get; private set; }
        public string FEEDRATE_BlockPostfix { get; private set; }
        public double FEEDRATE_HighValue { get; private set; }
        public double FEEDRATE_LowValue { get; private set; }
        public double FEEDRATE_ScaleFactor { get; private set; }
        public int FEEDRATE_NumOfDecimalPlaces { get; private set; }
        public string FEEDRATE_Block { get; private set; }
        public int FEEDRATE_OutputPlace { get; private set; }
        public double FEEDRATE_ShowTrailingZeros { get; private set; }
        public double FEEDRATE_FeedScaleFactor { get; private set; }
        public int FEEDRATE_FeedShowTrailingZeros { get; private set; }
        public int FEEDRATE_FeedNumOfDecimalPlaces { get; private set; }
        public double FEEDRATE_ZScaleFactor { get; private set; }
        public double FEEDRATE_UnitsPerRevolution { get; private set; }
        public double FEEDRATE_UnitsPerMinute { get; private set; }

        public string MISCELLANEOUS_CoolantOff { get; private set; }
        public string MISCELLANEOUS_CoolantOn { get; private set; }
        public string MISCELLANEOUS_CoolantMist { get; private set; }
        public string MISCELLANEOUS_CoolantFlood { get; private set; }
        public string MISCELLANEOUS_CoolantThru { get; private set; }
        public string MISCELLANEOUS_CoolantTap { get; private set; }
        public string MISCELLANEOUS_CompensationOff { get; private set; }
        public string MISCELLANEOUS_CompensationLeft { get; private set; }
        public string MISCELLANEOUS_CompensationRight { get; private set; }
        public string MISCELLANEOUS_CompensationLength { get; private set; }
        public string MISCELLANEOUS_WorkOffset { get; private set; }

        public string STARTUP_ProgramCode { get; private set; }

        public string TOOLCHANGE_AdjustRegister { get; private set; }
        public string TOOLCHANGE_FirstMacro { get; private set; }
        public string TOOLCHANGE_Macro { get; private set; }
        public string TOOLCHANGE_CutComLeft { get; private set; }
        public string TOOLCHANGE_CutComRight { get; private set; }
        public string TOOLCHANGE_CutComOff { get; private set; }
        public int TOOLCHANGE_Use2DigitFormat { get; private set; }

        public string END_ProgramCode { get; private set; }

        #endregion

        #region Post variables
        public string G_CODE { get; private set; }
        public string DELIMITER { get; private set; }
        public string NEXT_NONMDL_X { get; private set; }
        public string NEXT_NONMDL_Y { get; private set; }
        public string NEXT_NONMDL_Z { get; private set; }
        public string NEXT_X { get; private set; }
        public string NEXT_Y { get; private set; }
        public string NEXT_Z { get; private set; }
        public string NEXT_I { get; private set; }
        public string NEXT_J { get; private set; }
        public string NEXT_K { get; private set; }
        public string ROTATION_AXIS { get; private set; }
        public string ROTATION_DIR { get; private set; }
        public string ANGLE { get; private set; }
        public string LINEAR { get; private set; }
        public string RAPID { get; private set; }
        public string SPINDLE_CODE { get; private set; }
        public string SPINDLE_SPD { get; private set; }
        public string FEEDRATE_CODE { get; private set; }
        public string FEEDRATE { get; private set; }
        public string ZFEEDRATE { get; private set; }
        public string START_CHAR { get; private set; }
        public string OUTPUTFILE_NAME { get; private set; }
        public string STOCK_MIN_X { get; private set; }
        public string STOCK_MIN_Y { get; private set; }
        public string STOCK_MIN_Z { get; private set; }
        public string STOCK_MAX_X { get; private set; }
        public string STOCK_MAX_Y { get; private set; }
        public string STOCK_MAX_Z { get; private set; }
        public string STOCK_LENGTH_X { get; private set; }
        public string STOCK_LENGTH_Y { get; private set; }
        public string STOCK_LENGTH_Z { get; private set; }
        public string START_POSITION_X { get; private set; }
        public string START_POSITION_Y { get; private set; }
        public string START_POSITION_Z { get; private set; }
        public int TOOL_NUM { get; private set; }
        public double TOOL_DIA { get; private set; }
        public double TOOL_LENGTH { get; private set; }
        public string SEQ_PRECHAR { get; private set; }
        public string SEQNUM { get; private set; }
        public string OUTPUT_UNITS_CODE { get; private set; }
        public string OUTPUT_MODE_CODE { get; private set; }
        public string SPINDLE_BLK { get; private set; }
        public string CIR_PLANE { get; private set; }
        // public double CYCL_Z_MINUS_DEPTH {get; private set;}
        // public double CYCL_Z_PLUS_CLEAR {get; private set;}
        // public double CYCL_IPM {get; private set;}
        // public string CYCL_SCALED_DWELL {get; private set;}
        // public string CYCL_INCR {get; private set;}
        // public string CYCL_IPR {get; private set;}
        // public string CYCL_CSINK_DEPTH {get; private set;}
        // public string CYCL_ORIENT {get; private set;}
        // public string CYCL_DWELL {get; private set;}
        public string STOP_CHAR { get; private set; }
        public string EOB { get; private set; }

        #endregion

        public SpmPost()
        {
        }

        public struct ModalState
        {
            public double X, Y, Z, I, J, K, SpindleSpeed, FeedRate;
            public string Gcode;

            public static ModalState Zero
            {
                get
                {
                    return new ModalState(0, 0, 0, 0, 0, 0, 0, 0, "");
                }
            }

            public ModalState(double x, double y, double z, double i, double j, double k, double spindlespeed, double feedrate, string gcode)
            {
                X = x;
                Y = y;
                Z = z;
                I = i;
                J = j;
                K = k;
                SpindleSpeed = spindlespeed;
                FeedRate = feedrate;
                Gcode = gcode;
            }

            public ModalState(ModalState prev) : this(prev.X, prev.Y, prev.Z, prev.I, prev.J, prev.K, prev.SpindleSpeed, prev.FeedRate, prev.Gcode)
            {
            }

            public Point3d XYZ
            {
                get { return new Point3d(X, Y, Z); }
                set { X = value.X; Y = value.Y; Z = value.Z; }
            }

            public Point3d IJK
            {
                get { return new Point3d(I, J, K); }
                set { I = value.X; J = value.Y; K = value.Z; }
            }
        }

        // public List<string> Post(PolyCurve pcurve)
        public List<string> Post(string filepath, IEnumerable<Toolpath> inputToolpaths)
        {

            this.OUTPUTFILE_NAME = filepath + "." + this.GENERAL_Extension;
            var nc = new List<string>();

            if (!Loaded)
            {
                ErrorMessage = "Post is not correctly loaded!";
                return nc;
            }

            var transform = Transform.PlaneToPlane(Workplane, Plane.WorldXY);

            Toolpaths = inputToolpaths.Select(x => x.Duplicate()).ToList();
            if (Toolpaths == null || Toolpaths.Count < 1) { throw new ArgumentException("No valid toolpaths provided.");  }

            for (int i = 0; i < Toolpaths.Count; ++i)
            {
                Toolpaths[i].Transform(transform);
            }

            // Initialize program
            if (!Stock.IsValid)
            {
                Stock = GetStockSize(Toolpaths);
            }

            var StartPosition = Toolpaths.First().Paths.First().First().Plane.Origin;

            this.STOCK_MIN_X = Stock.Min.X.ToString("0.000");
            this.STOCK_MIN_Y = Stock.Min.Y.ToString("0.000");
            this.STOCK_MIN_Z = Stock.Min.Z.ToString("0.000");

            this.STOCK_MAX_X = Stock.Max.X.ToString("0.000");
            this.STOCK_MAX_Y = Stock.Max.Y.ToString("0.000");
            this.STOCK_MAX_Z = Stock.Max.Z.ToString("0.000");

            this.STOCK_LENGTH_X = (Stock.Max.X - Stock.Min.X).ToString("0.000");
            this.STOCK_LENGTH_Y = (Stock.Max.Y - Stock.Min.Y).ToString("0.000");
            this.STOCK_LENGTH_Z = (Stock.Max.Z - Stock.Min.Z).ToString("0.000");

            this.START_POSITION_X = StartPosition.X.ToString("0.000");
            this.START_POSITION_Y = StartPosition.Y.ToString("0.000");
            this.START_POSITION_Z = StartPosition.Z.ToString("0.000");

            this.SPINDLE_CODE = this.SPINDLE_Code;
            this.DELIMITER = this.GENERAL_Delimiter == 0 ? this.GENERAL_UserDefinedDelimiter : " ";
            this.EOB = this.GENERAL_EndBlockCharacter;
            this.CIR_PLANE = this.CIRCLE_XYPlaneCode;
            this.OUTPUT_MODE_CODE = GENERAL_AbsCode;
            this.SEQ_PRECHAR = "";
            this.SEQNUM = "";
            this.FEEDRATE_CODE = this.FEEDRATE_Code;

            var feedrateInline = this.FEEDRATE_OutputPlace > 0 ? this.DELIMITER : System.Environment.NewLine;

            // Calculate actual speeds
            var feedrateLowActual = (int)(this.FEEDRATE_LowValue / this.FEEDRATE_ScaleFactor);
            var feedrateHighActual = (int)(this.FEEDRATE_HighValue / this.FEEDRATE_ScaleFactor);

            // Hack to set first tool's data
            var firstTool = Toolpaths.First().Tool;
            this.TOOL_NUM = firstTool.Number;
            this.TOOL_DIA = firstTool.Diameter;
            this.TOOL_LENGTH = firstTool.Length;

            MachineTool tool = null;

            // Start program
            nc.Add($"{GENERAL_CommentStartChar}Generated from {Name} by {Author} on {DateTime.Now}{GENERAL_CommentEndChar}");
            nc.Add($"{InstantiateVariables(STARTUP_ProgramCode)}");

            // Iterate through toolpaths
            ModalState current = ModalState.Zero, prev = ModalState.Zero;
            var lineNum = 0;

            double currentSpindleSpeed = 0;
            foreach (var toolpath in Toolpaths)
            {
                var seconds = toolpath.GetTotalTime();
                var minutes = (int)Math.Floor(seconds / 60);
                seconds = seconds - minutes * 60;

                nc.Add($"{GENERAL_CommentStartChar}{toolpath.Name} - Estimated time {minutes:00}m {seconds:00}s{GENERAL_CommentEndChar}");
                nc.Add($"{GENERAL_CommentStartChar}Using {toolpath.Tool.Name} - {toolpath.Tool.Diameter:0.0}mm{GENERAL_CommentEndChar}");

                if (tool == null)
                {
                    tool = toolpath.Tool;

                    this.TOOL_NUM = tool.Number;
                    this.TOOL_DIA = tool.Diameter;
                    this.TOOL_LENGTH = tool.Length;

                    this.SPINDLE_SPD = string.Format($"{{0:F{SPINDLE_NumOfDecimalPlaces}}}", tool.SpindleSpeed);
                    this.SPINDLE_BLK = InstantiateVariables(this.SPINDLE_Block, SPINDLE_NumOfDecimalPlaces);
                    currentSpindleSpeed = tool.SpindleSpeed;

                    TOOLCHANGE_FirstMacro = InstantiateVariables(this.TOOLCHANGE_FirstMacro, 0);

                    nc.Add($"{TOOLCHANGE_FirstMacro}");
                }
                else if (tool != toolpath.Tool)
                {
                    tool = toolpath.Tool;

                    this.TOOL_NUM = tool.Number;
                    this.TOOL_DIA = tool.Diameter;
                    this.TOOL_LENGTH = tool.Length;

                    this.SPINDLE_SPD = string.Format($"{{0:F{SPINDLE_NumOfDecimalPlaces}}}", tool.SpindleSpeed);
                    this.SPINDLE_BLK = InstantiateVariables(this.SPINDLE_Block, SPINDLE_NumOfDecimalPlaces);
                    currentSpindleSpeed = tool.SpindleSpeed;

                    TOOLCHANGE_Macro = InstantiateVariables(this.TOOLCHANGE_Macro, 0);
                    nc.Add($"{TOOLCHANGE_Macro}");
                }

                if (Math.Abs(currentSpindleSpeed - tool.SpindleSpeed) > Epsilon)
                {
                    this.SPINDLE_SPD = string.Format($"{{0:F{SPINDLE_NumOfDecimalPlaces}}}", tool.SpindleSpeed);
                    this.SPINDLE_BLK = InstantiateVariables(this.SPINDLE_Block, SPINDLE_NumOfDecimalPlaces);
                    nc.Add($"{this.SPINDLE_BLK}");
                }

                foreach (var path in toolpath.Paths)
                {
                    foreach (var wp in path)
                    {
                        var builder = new StringBuilder();

                        double speed = wp.Rapid ? toolpath.Tool.RapidRate : (wp.Plunge ? toolpath.Tool.PlungeRate : toolpath.Tool.FeedRate);
                        // TODO: Limit speed based on SPM max and min feed rates

                        if (!wp.Rapid)
                        {
                            if (speed < feedrateLowActual)
                            {
                                nc.Add($"{GENERAL_CommentStartChar}WARNING: Set feed rate of {speed} is below minimum actual allowed feedrate of {feedrateLowActual}!{GENERAL_CommentEndChar}");
                                speed = feedrateLowActual;
                            }
                            else if (speed > feedrateHighActual)
                            {
                                nc.Add($"{GENERAL_CommentStartChar}WARNING: Set feed rate of {speed} exceeds maximum actual allowed feedrate of {feedrateHighActual}!{GENERAL_CommentEndChar}");
                                speed = feedrateHighActual;
                            }
                        }

                        // speed = Math.Min(this.FEEDRATE_HighValue / this.FEEDRATE_ScaleFactor, Math.Max(this.FEEDRATE_LowValue / this.FEEDRATE_ScaleFactor, speed));
                        var motionCode = wp.IsArc() ? (wp.Clockwise ? this.CIRCLE_ClockwiseArcCode : this.CIRCLE_CClockwiseArcCode) : wp.IsRapid() ? this.MOTION_RapidMotionCode : this.MOTION_LinearMotionCode;

                        current = new ModalState(wp.Plane.OriginX, wp.Plane.OriginY, wp.Plane.OriginZ, 0, 0, 0, toolpath.Tool.SpindleSpeed, speed, motionCode);
                        this.G_CODE = current.Gcode;

                        if (Math.Abs(current.FeedRate - prev.FeedRate) > Epsilon && current.Gcode != this.MOTION_RapidMotionCode)
                        {
                            this.FEEDRATE = string.Format($"{{0:F{this.FEEDRATE_FeedNumOfDecimalPlaces}}}", current.FeedRate * this.FEEDRATE_ScaleFactor);
                            this.ZFEEDRATE = string.Format($"{{0:F{this.FEEDRATE_FeedNumOfDecimalPlaces}}}", current.FeedRate * this.FEEDRATE_ScaleFactor * this.FEEDRATE_ZScaleFactor);

                            if (this.FEEDRATE_OutputPlace == 0)
                            {
                                builder.AppendLine($"{InstantiateVariables(this.FEEDRATE_Block, this.FEEDRATE_NumOfDecimalPlaces, this.FEEDRATE_FeedShowTrailingZeros > 0)}{this.EOB}");
                            }
                        }

                        this.NEXT_NONMDL_X = string.Format($"{{0:F{this.MOTION_NumOfDecimalPlaces}}}", current.X * this.MOTION_ScaleFactorX);
                        this.NEXT_NONMDL_Y = string.Format($"{{0:F{this.MOTION_NumOfDecimalPlaces}}}", current.Y * this.MOTION_ScaleFactorY);
                        this.NEXT_NONMDL_Z = string.Format($"{{0:F{this.MOTION_NumOfDecimalPlaces}}}", current.Z * this.MOTION_ScaleFactorZ);

                        this.NEXT_X = string.Format($"{GENERAL_XRegister}{{0:F{this.MOTION_NumOfDecimalPlaces}}}", current.X * this.MOTION_ScaleFactorX);
                        this.NEXT_Y = string.Format($"{GENERAL_YRegister}{{0:F{this.MOTION_NumOfDecimalPlaces}}}", current.Y * this.MOTION_ScaleFactorY);
                        this.NEXT_Z = string.Format($"{GENERAL_ZRegister}{{0:F{this.MOTION_NumOfDecimalPlaces}}}", current.Z * this.MOTION_ScaleFactorZ);

                        if (wp.Rapid)
                        {
                            builder.Append($"{InstantiateVariables(this.MOTION_RapidBlock, this.MOTION_NumOfDecimalPlaces, this.MOTION_ShowMotionTrailingZeros > 0)}");
                            
                        }
                        else if (wp.Arc)
                        {
                            current.IJK = (Point3d)(current.XYZ - tas.Core.Util.Interpolation.GetXYArcCentre(prev.XYZ, current.XYZ, wp.Radius));

                            this.NEXT_I = string.Format($"{CIRCLE_IRegister}{{0:F{this.MOTION_NumOfDecimalPlaces}}}", current.I);
                            this.NEXT_J = string.Format($"{CIRCLE_JRegister}{{0:F{this.MOTION_NumOfDecimalPlaces}}}", current.J);
                            this.NEXT_K = string.Format($"{CIRCLE_KRegister}{{0:F{this.MOTION_NumOfDecimalPlaces}}}", current.K);

                            // this.G_CODE = arc.Plane.ZAxis * Vector3d.ZAxis < 0 ? this.CIRCLE_ClockwiseArcCode : this.CIRCLE_CClockwiseArcCode;
                            //this.G_CODE = wp.Clockwise ? this.CIRCLE_ClockwiseArcCode : this.CIRCLE_CClockwiseArcCode;
                            builder.Append($"{InstantiateVariables(this.CIRCLE_BlockXY, this.MOTION_NumOfDecimalPlaces, this.MOTION_ShowMotionTrailingZeros > 0)}");
                        }
                        else if (!wp.Arc)
                        {
                            //this.G_CODE = current.Gcode;
                            builder.Append($"{InstantiateVariables(this.MOTION_LinearBlock, this.MOTION_NumOfDecimalPlaces, this.MOTION_ShowMotionTrailingZeros > 0)}");
                        }

                        if (Math.Abs(current.FeedRate - prev.FeedRate) > Epsilon && this.FEEDRATE_OutputPlace > 0 && current.Gcode != this.MOTION_RapidMotionCode)
                        {
                            builder.Append($"{this.DELIMITER}{InstantiateVariables(this.FEEDRATE_Block, this.FEEDRATE_NumOfDecimalPlaces, this.FEEDRATE_FeedShowTrailingZeros > 0)}");
                        }

                        builder.Append(this.EOB);

                        nc.Add(builder.ToString());

                        prev = current;

                    }
                }
            }

            // End program
            nc.Add($"{InstantiateVariables(this.END_ProgramCode)}");

            try
            {
                System.IO.File.WriteAllLines(this.OUTPUTFILE_NAME, nc);
            }
            catch(Exception e)
            {
                ErrorMessage = e.Message;
            }

            return nc;
        }

        public string InstantiateVariables(string block, int decimalPlaces = 4, bool trailingZeros = true)
        {
            // if (string.IsNullOrEmpty(block)) return block;
            var matches = Regex.Matches(block, @"(?<=\[).+?(?=\])");
            var fstring = trailingZeros ? $"{{0:F{decimalPlaces}}}" : $"{{0:{new String('#', decimalPlaces)}}}";

            var builder = new StringBuilder(block);

            foreach (Match match in matches)
            {
                Type myType = this.GetType();
                PropertyInfo pinfo = myType.GetProperty(match.Value);
                if (pinfo != null)
                {
                    if (pinfo.PropertyType == typeof(double))
                        builder.Replace($"[{match.Value}]", string.Format(fstring, pinfo.GetValue(this, null)));
                    else
                        builder.Replace($"[{match.Value}]", $"{pinfo.GetValue(this, null)}");
                }
                else
                    builder.Replace($"[{match.Value}]", $"<UNKNOWN VARIABLE>");
            }

            return builder.ToString();
        }

        public SpmPost(string filepath, Plane? workplane = null)
        {
            Console.WriteLine($"Reading '{filepath}' post-processor file...");
            try
            {
                Read(filepath);
                Loaded = true;
            }
            catch (Exception e)
            {
                Loaded = false;
                ErrorMessage = e.Message;
            }

            if (workplane != null)
                Workplane = workplane.Value;
        }

        internal BoundingBox GetStockSize(IEnumerable<Toolpath> toolpaths)
        {
            var bb = BoundingBox.Empty;
            foreach (var toolpath in toolpaths)
            {
                var tbb = BoundingBox.Empty;
                foreach (var path in toolpath.Paths)
                {
                    foreach (var wp in path)
                    {
                        tbb.Union(wp.Plane.Origin);
                    }
                }

                tbb.Inflate(toolpath.Tool.Diameter * 0.5, toolpath.Tool.Diameter * 0.5, 0);

                bb.Union(tbb);
            }

            return bb;
        }

        internal void Read(string filepath)
        {
            var lines = System.IO.File.ReadAllLines(filepath);

            bool block = false;
            string blockKey = "";
            var blockLines = new List<string>();

            var splitChar = new char[] { '=' };

            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line) || line.StartsWith("//")) continue;

                if (block && line.EndsWith("End"))
                {
                    SetProperty(blockKey, string.Join(System.Environment.NewLine, blockLines));
                    block = false;
                }
                else if (line.EndsWith("Start"))
                {
                    block = true;
                    blockKey = line.Substring(0, line.Length - 5);
                    blockLines = new List<string>();
                }
                else if (block)
                {
                    blockLines.Add(line);
                }
                else
                {
                    var tok = line.Split(splitChar, StringSplitOptions.None);
                    if (tok.Length == 2)
                    {
                        SetProperty(tok[0].Trim(), tok[1].Trim());
                    }
                }
            }
        }

        internal void SetProperty(string key, string value)
        {
            if (string.IsNullOrEmpty(value))
                return;

            Type myType = this.GetType();
            PropertyInfo pinfo = myType.GetProperty(key);

            if (pinfo == null)
            {
                Console.WriteLine($"WARNING: '{key}' is not implemented.");
                return;
            }

            Console.WriteLine($"{key} : {value}");

            if (pinfo.PropertyType == typeof(string))
                pinfo.SetValue(this, value, null);
            else if (pinfo.PropertyType == typeof(double))
                pinfo.SetValue(this, double.Parse(value), null);
            else if (pinfo.PropertyType == typeof(int))
                pinfo.SetValue(this, int.Parse(value), null);
        }

        public void ClearErrors()
        {
            ErrorMessage = string.Empty;
        }
    }
}
