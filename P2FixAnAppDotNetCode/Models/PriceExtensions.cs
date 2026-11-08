using System.Globalization;

namespace P2FixAnAppDotNetCode.Models
{
    public static class PriceExtensions
    {
        /// <summary>
        /// Affiche un prix en francs CFA.
        /// BUG corrigé : ToString("c") affichait la devise de la culture courante
        /// (¤, $ ou €) alors que les prix de la boutique sont en FCFA.
        /// </summary>
        public static string ToFcfa(this double price) =>
            price.ToString("N0", CultureInfo.CurrentCulture) + " FCFA";
    }
}
