using System.ComponentModel.DataAnnotations;

namespace HomeBuddy_API.DTOs.Requests.FavoriteDTOs
{
    /// <summary>
    /// Request body for adding a favorite. Caller can supply either VariantId or Sku.
    /// </summary>
    public class AddFavoriteRequest
    {
        public Guid? VariantId { get; set; }

        [MaxLength(100)]
        public string? Sku { get; set; }
    }
}

