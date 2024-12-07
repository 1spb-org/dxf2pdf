/***
* 
* Dxf2Pdf universal microservice
* Author: Georgii A. Kupriianov, 1spb.org, 2024
*/

using LiteDB;

namespace Dxf2Pdf.Queue.Services
{

    // TODO:// обернуть все DB в exception

    public class Launch
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public DateTime Requested { get; set; } = DateTime.Now;
        public DateTime? Approved { get; set; } = null;

        public string Json { set; get; } = null!;
        public bool IsActive { get; set; } = false;
    }


    internal class Stg
    {
        const string defDbName = "Dxf2Pdf.db";

        internal static Launch New(string name, string json, string? dbn)
        {
            return Act(dbn, col =>
            {
                // Create your new customer instance
                var c = new Launch
                {
                    Name = name,
                    Json = json
                };

                col.Insert(c);

                // Index document using document Name property
                col.EnsureIndex(x => x.Name);

                return c;
            });
        }


        internal static bool Approve(Guid g, string? dbn)
        {
            return Act(dbn, col =>
            {
                var c = col.Query().Where(x => x.Id == g).FirstOrDefault();

                if (c == null)
                    return false;

                // Update a document inside a collection
                c.IsActive = true;
                c.Approved = DateTime.Now;

                col.Update(c);

                return true;
            });
        }


        internal static bool Delete(string name, string? dbn)
        {
           return Act(dbn, col => col.DeleteMany(x => x.Name == name) > 0);
        }

        internal static Launch GetLaunch(string name, string? dbn)
        {            
            return Act(dbn, col => col.Query().Where(x => x.Name == name).FirstOrDefault());            
        }

        internal static Launch GetLaunchById(Guid g, string? dbn)
        {
            return Act(dbn, col => col.Query().Where(x => x.Id == g).FirstOrDefault());
        }

        internal static List<Launch> GetAllLaunches(string? dbn)
        {
            return Act(dbn, col => col.Query().ToList());         
        }

        internal static T Act<T>(string? dbn, Func<ILiteCollection<Launch>, T> F)
        {
            using (var db = DB(dbn))
            {
                return F(GetColl(db));
            }
        }
        private static LiteDatabase DB(string? dbn)
        {
            EnsureDBPathExists(dbn);
            return new LiteDatabase(dbn ?? defDbName);
        }

        private static ILiteCollection<Launch> GetColl(LiteDatabase db)
        {
            // Get a collection (or create, if doesn't exist)
            return db.GetCollection<Launch>("Launch");
        }
        private static void EnsureDBPathExists(string dbn)
        {
            if (File.Exists(dbn))
               return;
            var dir = Path.GetDirectoryName(dbn) ?? "";
            if (dir != "")
                Directory.CreateDirectory(dir);
        }

    }
}