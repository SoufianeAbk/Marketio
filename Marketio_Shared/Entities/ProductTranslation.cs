namespace Marketio_Shared.Entities
{
    public class ProductTranslation
    {
        public int Id { get; set; }
        public int ProductId { get; set; }

        /// <summary>"nl", "fr" of "en"</summary>
        public string Locale { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Navigatie
        public Product Product { get; set; } = null!;
    }
}