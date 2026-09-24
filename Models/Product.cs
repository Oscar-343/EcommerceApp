using System.ComponentModel.DataAnnotations;

namespace EcommerceApp.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required, Range(0.01, 999999.99, ErrorMessage = "El precio debe ser mayor a 0.")]
        public decimal Price { get; set; }

        // Precio de oferta opcional: si existe, se muestra tachado el precio normal.
        [Range(0.01, 999999.99)]
        public decimal? PromotionalPrice { get; set; }

        // Destacado/en promoción: estos productos salen primero en la portada de la tienda.
        public bool IsFeatured { get; set; }

        [Required, Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo.")]
        public int Stock { get; set; }

        // Imagen principal del producto
        public string? ImageUrl { get; set; }

        // Segunda imagen: producto en uso / contexto real
        public string? SecondaryImageUrl { get; set; }

        // Galería de imágenes adicionales (separadas por pipe |), igual que en Service.
        [MaxLength(2000)]
        public string? GalleryImages { get; set; }

        // Marca del producto
        [MaxLength(100)]
        public string? Brand { get; set; }

        public string? Category { get; set; }

        // Indica si es un producto nuevo (útil para mostrar etiqueta "NUEVO")
        public bool IsNew { get; set; }

        // Indica si es un producto más vendido
        public bool IsBestSeller { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
