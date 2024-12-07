/***
* Dxf2Pdf universal microservice
* Author: Georgii A. Kupriianov, 1spb.org, 2024
*/


using Grpc.Core;
using Hangfire;
using Hangfire.Server;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Dxf2Pdf.Queue.Services
{
    public class LauncherService : Launcher.LauncherBase
    {
        private readonly ILogger<LauncherService> _logger;
        private readonly IConfiguration _conf;
        private string? _dbn;
        private IBackgroundJobClient _backgroundJobClient;

        public LauncherService(IConfiguration configuration, ILogger<LauncherService> logger, IBackgroundJobClient bjc)
        {
            _logger = logger;
            _conf = configuration;
            _dbn = _conf["Common:DbName"] ?? "Dxf2Pdf.db";
            _backgroundJobClient = bjc;
        }

        public override Task<LaunchReply> Do(LaunchRequest request, ServerCallContext context)
        {
            var name = request.Name;
            var key = request.Key;
            var forceNew = request.New;

#if DEBUG
            // ignore API key, catch test data conditionally
            if(name.StartsWith("_test_"))
               DbgCreateTestData(name);

#else
            string confAPIKey = _conf["Common:API_KEY"] ?? "5485a468-ca3e-4a02-b945-4be27f437949";

            if (key != confAPIKey)
                return Task.FromResult(new HelloReply
                {
                    Message = "Не выполнить " + request.Name + ", поскольку указан неверный API ключ",
                    Id = "",
                    Name = name
                });
#endif

            if (forceNew)
                Stg.Delete(name, _dbn);

            var launch = Stg.GetLaunch(name, _dbn);

            if (launch != null)
            {
                if (launch.IsActive)
                    return Task.FromResult(new LaunchReply
                    {
                        Message = "Уже выполнено: " + name,
                        Id = launch.Id.ToString(),
                        Name = name
                    });
            }
            else
            {
                string ? json = LoadLaunchData(name);

                if (json!.ENull())
                {
                    _logger.LogError("Service request data loading failed!");

                    return Task.FromResult(new LaunchReply
                    {
                        Message = "Service request data loading failed: " + name,
                        Id = "",
                        Name = name
                    });
                }
                
                launch = Stg.New(name, json!, _dbn);               
            }

            try
            {
                LaunchState state = LaunchState.Create(launch, _conf);

                if(state.Error != LaunchError.NoError)
                {
                    _logger.LogError("Service data preparation failed: " + state.Error.ToString());

                    return Task.FromResult(new LaunchReply
                    {
                        Message = "Service data preparation failed: " + name,
                        Id = launch.Id.ToString(),
                        Name = name
                    });

                }

                _backgroundJobClient.Enqueue(() => DoDxf2Pdf(null, state));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Service exception!");

                return Task.FromResult(new LaunchReply
                {
                    Message = "Service exception! " + name,
                    Id = launch.Id.ToString(),
                    Name = name
                });
            }


            return Task.FromResult(new LaunchReply
            {
                Message = "Dxf2Pdf job started:" + name,
                Id = launch.Id.ToString(),
                Name = name
            });
        }


        string _inDir => _conf["Paths:InDir"] ?? LaunchState._dirDefInput;
        string _outDir => _conf["Paths:OutDir"] ?? LaunchState._dirDefOutput;

        private void DbgCreateTestData(string name)
        {
            if (!_inDir.ENull())            
                Directory.CreateDirectory(_inDir);
            
            if (!_outDir.ENull())
                Directory.CreateDirectory(_outDir);

            bool testerror = (name == "_test_error_");                      

            LaunchStateBase b = new LaunchStateBase
            {
                CmdPath =LaunchState._defCmd,
                CmdParameters = LaunchState._defPar,
                InputDXFPath = testerror? Guid.NewGuid().ToString()+".notfound.dxf": 
                  Path.Combine(_inDir, LaunchState._defDXF),
                PDFPath = Path.Combine(_outDir, LaunchState._defPDF),
                PlotScrPath = Path.Combine(_inDir, LaunchState._defPlotFileName)
            };

            if (!File.Exists(b.PlotScrPath) && File.Exists(LaunchState._defPlot))
                File.Copy(LaunchState._defPlot, b.PlotScrPath);

            if (!testerror)
            {
                if (!File.Exists(b.InputDXFPath) && File.Exists(LaunchState._defDXF))
                    File.Copy(LaunchState._defDXF, b.InputDXFPath);
            }

            var path = Path.Combine(_inDir, name + ".json");
            if (!File.Exists(path))
            {
                File.WriteAllText(path,
                    JsonConvert.SerializeObject(b, Formatting.Indented));
            }

        }

        private string? LoadLaunchData(string name)
        {
            var path = Path.Combine(_inDir, name + ".json");
            try
            {
#if DEBUG
                if(name.StartsWith("_test_"))
                _logger.LogInformation("*** Loading test data...");
#endif
                return File.ReadAllText(path);
            }
            catch(Exception e) 
            {
                _logger.LogError( e.Message + ": " + path );
                return null;
            }
        }

        public bool DoDxf2Pdf(PerformContext? ctx, LaunchState state)
        {
            var r = CallExe(state.CmdPath!, state.CmdParameters!, 0);
            return r.IsCompleted;
        }

        private ProcessComm.LaunchResult CallExe(
         string exe, string exe_params,
         uint timeout)
        {

            Action<Process> actProcStarted = z => OnProcessStarted(z);

            var r = ProcessComm.Execute(
                    exe,
                    exe_params,
                    timeout,
                    (d) => OnExeOutput(d),
                    (d) => OnExeErrorOutput(d),
                    actProcStarted);


            if (r.IsCompleted)
                OnProcessCompleted(r);
     
            return r;
        }

        private void OnProcessCompleted(ProcessComm.LaunchResult r)
        {
            Console.WriteLine(r.OutData);
        }

        private bool OnExeErrorOutput(string d)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(d);
            Console.ResetColor();
            return false;
        }

        private bool OnExeOutput(string d)
        {
            Console.WriteLine(d);
            return false;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        private void OnProcessStarted(Process p)
        {
            while (p.MainWindowHandle == IntPtr.Zero)
            {
                Thread.Sleep(50);
                p.Refresh();
            }

            if (_conf.GetValue<bool?>("Common:HideMainWindow") ?? true)
            {
                ShowWindow(p.MainWindowHandle, SW_HIDE);
            }
        
        }



    }
}
