/***
* Dxf2Pdf universal microservice
* Author: Georgii A. Kupriianov, 1spb.org, 2024
*/

 
using System.Diagnostics;
using System.Text; 

namespace Dxf2Pdf.Queue.Services
{
    /// <summary>
    /// Console communication helper class
    /// </summary>
    class ProcessComm
    {
        /// <summary>
        /// ConsoleResult class describes the resulting data of console process
        /// </summary>
        public class LaunchResult
        {
            public LaunchResult(string outData, string errData)
            {
                this.OutData = outData;
                this.ErrorData = errData;
            }
            public string OutData { get; private set; }
            public string ErrorData { get; private set; }
            public bool IsCompleted { get; set; }
            public int PID { get; set; }
        }

        public static LaunchResult Execute(string executablePath, string arguments,
           uint Timeout = 0,
           Func<string, bool> actOutput = null,
           Func<string, bool> actError = null,
           Action<Process> actStarted = null)
        {
            var output = new StringBuilder();
            var error = new StringBuilder();
            bool processExited = true;
            int pid = 0;
            var cyrDOS = Encoding.GetEncoding(866);

            ProcessStartInfo processStartupInfo = new ProcessStartInfo(executablePath, arguments)
            {
                StandardOutputEncoding = cyrDOS,
                StandardErrorEncoding = cyrDOS,               
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true, 
                CreateNoWindow = true,
                UseShellExecute = false
            };

            AutoResetEvent evtOutDataRead = new AutoResetEvent(false);
            AutoResetEvent evtErrDataRead = new AutoResetEvent(false);


            using (Process process = new Process() 
            {
                StartInfo = processStartupInfo 
            })
            {
                process.EnableRaisingEvents = true;

                process.OutputDataReceived += (o, e) =>
                {
                    if (e.Data!.ENull())
                    {
                        if (e.Data == null)
                            evtOutDataRead.Set();
                        return;
                    }

                    output.Append(e.Data + Environment.NewLine);
                    try
                    {
                        bool? terminateProcess = actOutput?.Invoke(e.Data);

                        process.StandardOutput.DiscardBufferedData();

                        if (terminateProcess.HasValue && terminateProcess.Value)
                        {
                            processExited = false;
                            process.Kill();
                        }
                    }
                    catch { }

                };

                process.ErrorDataReceived += (o, e) =>
                {
                    if (e.Data!.ENull())
                    {
                        if (e.Data == null)
                            evtErrDataRead.Set();
                        return;
                    }
                    error.Append(e.Data + Environment.NewLine);
                    try
                    {
                        bool? terminateProcess = actError?.Invoke(e.Data!);

                        process.StandardError.DiscardBufferedData();

                        if (terminateProcess.HasValue && terminateProcess.Value)
                        {
                            processExited = false;
                            process.Kill();
                        }
                    }
                    catch { }
                };

                process.Start();
                pid = process.Id;
                try { actStarted?.Invoke(process); } catch (Exception ex) { }
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                if (Timeout <= 0)
                {
                    try { process.WaitForExit(); } catch { processExited = false; }
                    try { process.WaitForExit(); } catch { }
                }
                else
                {
                    try { processExited = process.WaitForExit((int)Timeout); } catch { processExited = false; }

                    if (!processExited)
                        try { process.Kill(); } catch { }

                    try { process.WaitForExit(); } catch { }
                }

                evtOutDataRead.WaitOne(500);
                evtErrDataRead.WaitOne(500);

                process.Close();
            }

            return new LaunchResult(output.ToString(), error.ToString())
            {
                PID = pid,
                IsCompleted = processExited
            };
        }


    }

}

