namespace Frontend.WebAssembly.Poker.ViewModels
{
    /// <summary>
    /// A recept ajánlásokat reprezentáló modell.
    /// </summary>
    public class RecipeSuggestion
    {
        /// <summary>
        /// A recept címe.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Rövid leírás a recept lényegéről.
        /// </summary>
        public string ShortDescription { get; set; }

        /// <summary>
        /// A recept részletes elkészítési módja.
        /// </summary>
        public string DetailedRecipe { get; set; }
    }

    /// <summary>
    /// A backend felé elküldendő recept kérés adatai.
    /// </summary>
    public class RecipeRequest
    {
        /// <summary>
        /// A felhasználó által megadott recept leírása.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// A rendelkezésre álló hozzávalók listája.
        /// </summary>
        public List<string> Ingredients { get; set; }
    }
}
