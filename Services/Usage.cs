/***
* Dxf2Pdf universal microservice
* Author: Georgii A. Kupriianov, 1spb.org, 2024
*/


namespace Dxf2Pdf.Queue.Services
{
    internal class Usage
    {
        internal static void Print()
        {
            "USAGE :".CoutLn(ConsoleColor.White);
            @"
1. Configure paths and else in appsettings.json in microservice executable current directory.
2. Launch this microservice.
3. Create <name>.json with correct paths to files.
4. Place <name>.json in a directory that reachable by the microservise.
5. Use http(s)://hostname:port/v1/launcher/<name> as GET request endpoint.
6. After successful request, use http(s)://hostname:port/hangfire/ to see job progress in the webbrowser.
7. See requested output file after the job succeeded
"
            .CoutLn(ConsoleColor.Gray);
            "Enjoy it!".CoutLn(ConsoleColor.Green);
        }
    }
}