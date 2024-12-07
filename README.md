# The `Dxf2Pdf` Universal Microservice


## Description

The service is intended for use with Autodesk DWG TrueView,
see [here](https://www.cadforum.cz/en/unattended-dwg-plotting-and-pdf-publishing-without-autocad-tip10461),
to convert DXF to PDF files by calling an executable as follows:

 ```
 "<Path-to-dwgviewr.exe>" "<path-to-my.dxf>" /b "<path-to-plotPDF.scr>" /nologo
 ```
 
## Usage

1. Configure paths and else in appsettings.json placed in microservice executable current directory.
2. Launch this microservice.
3. Create `name`.json with correct paths to files.
4. Place `name`.json in a directory that reachable by the microservise.
5. Use http(s)://`hostname:port`/v1/launcher/`name` as GET request endpoint.
6. After successful request, use http(s)://`hostname:port`/hangfire/ to see job progress in the webbrowser.
7. See requested output file after the job succeeded

