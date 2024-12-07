/***
* Dxf2Pdf universal microservice
* Author: Georgii A. Kupriianov, 1spb.org, 2024
*/

namespace Dxf2Pdf.Queue.Services
{
    public static class Ext
    {
        public static void CoutLn(this string s, ConsoleColor c = ConsoleColor.White)
        {
          Console.ForegroundColor = c;
          Console.WriteLine(s);
          Console.ResetColor();
        }
        public static void Error(this string v)
        {
            v.CoutLn(ConsoleColor.Red);
        }

        public static string? EmptyAsNull(this string? s)
            => string.IsNullOrEmpty(s) ? null : s;

        public static bool ENull(this string e) => string.IsNullOrEmpty(e);
    }
}
