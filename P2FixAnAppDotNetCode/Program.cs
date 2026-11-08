using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;

// L'assembly s'appelle "Diayma" mais le code est dans le namespace "P2FixAnAppDotNetCode" :
// on indique à la localisation la racine à utiliser pour retrouver les fichiers .resx.
[assembly: RootNamespace("P2FixAnAppDotNetCode")]

namespace P2FixAnAppDotNetCode
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        // Migration .NET 10 : IWebHost (ASP.NET Core 2.0) remplacé par l'hôte générique IHost.
        // La classe Startup est conservée pour garder la même structure que le code d'origine.
        public static IHost BuildWebHost(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>())
                .Build();
    }
}
