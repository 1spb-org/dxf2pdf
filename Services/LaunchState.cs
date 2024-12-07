/***
* Dxf2Pdf universal microservice
* Author: Georgii A. Kupriianov, 1spb.org, 2024
*/

using Newtonsoft.Json;
using System.Reflection;

namespace Dxf2Pdf.Queue.Services
{
    public enum LaunchError 
    {
        NoError = 0,
        InputFileNotFound = 1,

    }

    public class LaunchStateBase
    {
        public string? CmdPath { get; set; }
        public string? CmdParameters { get; set; }
        public string? InputDXFPath { get; set; }
        public string? PDFPath { get; set; }
        public string? PlotScrPath { get; set; }
    }

    public class LaunchState: LaunchStateBase
    {
        public static LaunchState Create(Launch launch, IConfiguration conf)
        {
            LaunchState L = new LaunchState(launch, conf);           
            return L;
        }
        public LaunchState(Launch launch, IConfiguration conf)
        {
            this.Id = launch.Id.ToString();
            this.Name = launch.Name;
            this._conf = conf;

            LoadJson(launch.Json);

            Construct();
        }

        private void LoadJson(string json)
        {
            var b = JsonConvert.DeserializeObject<LaunchStateBase>(json); 

            CmdPath = b.CmdPath;
            CmdParameters = b.CmdParameters;
            InputDXFPath = b.InputDXFPath;
            PDFPath = b.PDFPath;
            PlotScrPath = b.PlotScrPath;
        }

        // Needed for Hangfire!
        public LaunchState()
        {
        }

        public void Construct()
        {
            CmdPath = CmdPath ?? _defCmd;
            CmdParameters = CmdParameters ?? _defPar;
            InputDXFPath = InputDXFPath ?? _defDXF;
            PDFPath = PDFPath ?? _defPDF;
            PlotScrPath = PlotScrPath ?? _defPlot;

            if (!File.Exists(PlotScrPath))
                File.WriteAllText(PlotScrPath, _PlotScript);


            if (!File.Exists(InputDXFPath))
            {
                InputDXFPath = Path.Combine(_dirInput, Path.GetFileName(InputDXFPath));
                if (!File.Exists(InputDXFPath))
                {
                    Error = LaunchError.InputFileNotFound;
                    return;
                }
            }

            var dirDxf = Path.GetDirectoryName(InputDXFPath);
            if (dirDxf != null)
                PDFPath = Path.Combine(dirDxf, PDFPath);

            PlotScrPath = ReplaceInScript(PlotScrPath, PDFPath);

            CmdParameters = CmdParameters.Replace("<path-to-plotPDF.scr>", PlotScrPath);
            CmdParameters = CmdParameters.Replace("<path-to-my.dxf>", InputDXFPath);
        }

        private string ReplaceInScript(string plot, string pdf)
        {
            var txt = File.ReadAllText(plot);
            txt = txt.Replace("(* OUTPUTPATH.PDF *)", pdf);

            var dirPdf = Path.GetDirectoryName(pdf);

            if (dirPdf.ENull())
                dirPdf = Directory.GetCurrentDirectory();

            plot = Path.Combine(dirPdf, Id.ToString() + ".scr");

            File.WriteAllText(plot, txt);
            return plot;
        }


        public const string _defDXF = @"default.dxf";
        public const string _defPDF = @"default-output.pdf";
        internal static string _defPlotFileName =>  "plotPdf.scr";
        internal static string _defPlot => Path.Combine(_dirDefInput, _defPlotFileName);

        public const string _defCmd = @"C:\Program Files\Autodesk\DWG TrueView 2023 - English\dwgviewr.exe";
        public const string _defPar = @"""<path-to-my.dxf>"" /b ""<path-to-plotPDF.scr>"" /nologo";
        private static string _dirExe =>
           Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        internal static string _dirDefInput =>
             Path.Combine(_dirExe, "_InputData");
        internal static string _dirDefOutput =>
             Path.Combine(_dirExe, "_OutputData");

        internal string _dirInput =>
             _conf["Paths:InDir"] ?? _dirDefInput;
        internal string _dirOutput =>
             _conf["Paths:OutDir"] ?? _dirDefOutput;


        public string Name { get;  set; }

        private IConfiguration _conf;

        internal string Id { get; set; }
        internal LaunchError Error { get; private set; } = LaunchError.NoError;

        public const string _PlotScript = @"
;Sample plot script
_PLOT
_Y

AutoCAD PDF (High Quality Print).pc3
ISO A3 (297.00 x 420.00 MM)
_Millimeters
_Landscape
_No
_Layout
_Fit
0,0
_Yes
.
_Yes
_Yes
_Y
_Y
(* OUTPUTPATH.PDF *)
_Yes
_Yes
_QUIT _Yes
";
    }
}