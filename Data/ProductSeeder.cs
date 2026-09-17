using EcommerceApp.Models;

namespace EcommerceApp.Data
{
    /// <summary>
    /// Seeder de productos para datos de prueba.
    /// Ejecutar una sola vez para poblar la base de datos con productos de muestra.
    /// </summary>
    public static class ProductSeeder
    {
        public static async Task SeedProductsAsync(ApplicationDbContext context)
        {
            // Si ya hay productos, no hacer nada
            if (context.Products.Any())
                return;

            var products = new List<Product>
            {
                // === TIENDAS DE CAMPAÑA ===
                new Product
                {
                    Name = "Tienda Alpina 3 Estaciones",
                    Brand = "Montaña Pro",
                    Category = "Tiendas de campaña",
                    Description = "Tienda de 3 temporadas para dos personas. Muy resistente ante condiciones climáticas extremas. Incluye doble piso y vestíbulo amplio.",
                    Price = 450m,
                    PromotionalPrice = 380m,
                    ImageUrl = "https://images.unsplash.com/photo-1478131143081-80f7f84ca84d?w=400&h=400&fit=crop",
                    SecondaryImageUrl = "https://images.unsplash.com/photo-1504280390367-361c6d9f38f4?w=400&h=400&fit=crop",
                    Stock = 12,
                    IsFeatured = true,
                    IsNew = true,
                    IsBestSeller = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-30)
                },
                new Product
                {
                    Name = "Tienda Ultraligera 2 Personas",
                    Brand = "Trail Gear",
                    Category = "Tiendas de campaña",
                    Description = "Solo 900g. Perfecta para trekking de larga distancia. Resistente al agua y con ventilación óptima.",
                    Price = 350m,
                    ImageUrl = "https://images.unsplash.com/photo-1469854523086-cc02fe5d8800?w=400&h=400&fit=crop",
                    SecondaryImageUrl = "https://images.unsplash.com/photo-1478131143081-80f7f84ca84d?w=400&h=400&fit=crop",
                    Stock = 8,
                    IsFeatured = false,
                    IsNew = false,
                    IsBestSeller = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-45)
                },

                // === CALZADO DE TREKKING ===
                new Product
                {
                    Name = "Botas Trekking Summit Pro",
                    Brand = "Alpina Boots",
                    Category = "Calzado de trekking",
                    Description = "Botas de montaña de cuero Nubuck. Suela Vibram. Excelente agarre en terrenos rocosos y húmedos. Peso: 820g por par.",
                    Price = 280m,
                    ImageUrl = "https://images.unsplash.com/photo-1542291026-7eec264c27ff?w=400&h=400&fit=crop",
                    SecondaryImageUrl = "https://images.unsplash.com/photo-1491553895911-0055eca6402d?w=400&h=400&fit=crop",
                    Stock = 25,
                    IsFeatured = true,
                    IsNew = false,
                    IsBestSeller = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-60)
                },
                new Product
                {
                    Name = "Zapatillas Sendero Compact",
                    Brand = "Trail Runners",
                    Category = "Calzado de trekking",
                    Description = "Para senderos fáciles a moderados. Ligeras (320g) y cómodas. Ideal para senderismo de día.",
                    Price = 120m,
                    PromotionalPrice = 95m,
                    ImageUrl = "https://images.unsplash.com/photo-1542291026-7eec264c27ff?w=400&h=400&fit=crop",
                    Stock = 30,
                    IsFeatured = false,
                    IsNew = true,
                    IsBestSeller = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },

                // === MOCHILAS ===
                new Product
                {
                    Name = "Mochila Trekking 65L",
                    Brand = "BackPack Master",
                    Category = "Mochilas",
                    Description = "Mochila de trekking de 65 litros. Bastidor interno ajustable. Sistema de ventilación. Cintura acolchada.",
                    Price = 350m,
                    ImageUrl = "https://images.unsplash.com/photo-1553062407-98eeb64c6a62?w=400&h=400&fit=crop",
                    SecondaryImageUrl = "https://images.unsplash.com/photo-1458668383970-8542c3b1d25e?w=400&h=400&fit=crop",
                    Stock = 15,
                    IsFeatured = true,
                    IsNew = false,
                    IsBestSeller = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-50)
                },
                new Product
                {
                    Name = "Mochila Daypack 30L",
                    Brand = "Daily Adventure",
                    Category = "Mochilas",
                    Description = "Mochila de día versátil. 30 litros. Perfecta para excursiones. Bolsillos organizadores.",
                    Price = 89m,
                    ImageUrl = "https://images.unsplash.com/photo-1553062407-98eeb64c6a62?w=400&h=400&fit=crop",
                    Stock = 40,
                    IsFeatured = false,
                    IsNew = true,
                    IsBestSeller = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-8)
                },

                // === BASTONES DE TREKKING ===
                new Product
                {
                    Name = "Bastones Trekking Aluminio",
                    Brand = "Pole Pro",
                    Category = "Bastones de trekking",
                    Description = "Par de bastones de aluminio ligero. Ajustables. Con protectores de nieve. Muy resistentes.",
                    Price = 95m,
                    ImageUrl = "https://images.unsplash.com/photo-1505228395891-9a51e7e86e81?w=400&h=400&fit=crop",
                    SecondaryImageUrl = "https://images.unsplash.com/photo-1571863533956-461c92f1704a?w=400&h=400&fit=crop",
                    Stock = 50,
                    IsFeatured = false,
                    IsNew = false,
                    IsBestSeller = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-40)
                },
                new Product
                {
                    Name = "Bastones Carbono Ultra Ligero",
                    Brand = "Carbon Peak",
                    Category = "Bastones de trekking",
                    Description = "Solo 280g el par. Hechos de fibra de carbono. Premium. Para trekking de larga distancia.",
                    Price = 220m,
                    PromotionalPrice = 180m,
                    ImageUrl = "https://images.unsplash.com/photo-1505228395891-9a51e7e86e81?w=400&h=400&fit=crop",
                    Stock = 18,
                    IsFeatured = true,
                    IsNew = true,
                    IsBestSeller = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                },

                // === ROPA OUTDOOR ===
                new Product
                {
                    Name = "Chaqueta Impermeable Pro",
                    Brand = "Weather Guard",
                    Category = "Ropa outdoor",
                    Description = "Chaqueta impermeable respirable. Gore-Tex. Protección contra viento y lluvia. Múltiples bolsillos.",
                    Price = 280m,
                    ImageUrl = "https://images.unsplash.com/photo-1533392839391-6db7ba1f2068?w=400&h=400&fit=crop",
                    SecondaryImageUrl = "https://images.unsplash.com/photo-1591047990632-15b42e323202?w=400&h=400&fit=crop",
                    Stock = 22,
                    IsFeatured = false,
                    IsNew = true,
                    IsBestSeller = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                },
                new Product
                {
                    Name = "Pantalón Trekking Resistente",
                    Brand = "Trail Wear",
                    Category = "Ropa outdoor",
                    Description = "Pantalón resistente a roturas. Bolsillos laterales. Cintura ajustable. Ideal para senderismo.",
                    Price = 135m,
                    ImageUrl = "https://images.unsplash.com/photo-1506629082847-11b7fb5f2e5c?w=400&h=400&fit=crop",
                    Stock = 35,
                    IsFeatured = false,
                    IsNew = false,
                    IsBestSeller = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-55)
                },

                // === CAMPING ===
                new Product
                {
                    Name = "Saco de Dormir Invierno",
                    Brand = "Night Comfort",
                    Category = "Camping",
                    Description = "Saco de dormir para temperaturas extremas. Aislamiento 800 fill. Muy cálido y ligero.",
                    Price = 320m,
                    ImageUrl = "https://images.unsplash.com/photo-1571863533956-461c92f1704a?w=400&h=400&fit=crop",
                    SecondaryImageUrl = "https://images.unsplash.com/photo-1478131143081-80f7f84ca84d?w=400&h=400&fit=crop",
                    Stock = 14,
                    IsFeatured = false,
                    IsNew = false,
                    IsBestSeller = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-70)
                },
                new Product
                {
                    Name = "Colchoneta Aislante Eva",
                    Brand = "Comfort Gear",
                    Category = "Camping",
                    Description = "Colchoneta aislante de espuma EVA. Ligera. Impermeable. Perfecta para suelos fríos.",
                    Price = 45m,
                    ImageUrl = "https://images.unsplash.com/photo-1571863533956-461c92f1704a?w=400&h=400&fit=crop",
                    Stock = 60,
                    IsFeatured = false,
                    IsNew = false,
                    IsBestSeller = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-80)
                },

                // === HIDRATACIÓN ===
                new Product
                {
                    Name = "Botella Térmica Stainless",
                    Brand = "Hydro Bottle",
                    Category = "Hidratación",
                    Description = "Botella de acero inoxidable de 1L. Mantiene bebidas frías 24h o calientes 12h. Ligera y duradera.",
                    Price = 65m,
                    ImageUrl = "https://images.unsplash.com/photo-1535632066927-ab7c9ab60908?w=400&h=400&fit=crop",
                    SecondaryImageUrl = "https://images.unsplash.com/photo-1602089113235-ab78c1220b6e?w=400&h=400&fit=crop",
                    Stock = 80,
                    IsFeatured = false,
                    IsNew = false,
                    IsBestSeller = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-65)
                },
                new Product
                {
                    Name = "Mochila Hidratación 2L",
                    Brand = "Hydro Pack",
                    Category = "Hidratación",
                    Description = "Mochila con depósito de agua 2L integrado. Tubo de silicona. Para senderismo intenso.",
                    Price = 85m,
                    PromotionalPrice = 65m,
                    ImageUrl = "https://images.unsplash.com/photo-1535632066927-ab7c9ab60908?w=400&h=400&fit=crop",
                    Stock = 25,
                    IsFeatured = false,
                    IsNew = true,
                    IsBestSeller = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-7)
                },

                // === ILUMINACIÓN ===
                new Product
                {
                    Name = "Linterna Frontal LED 1000L",
                    Brand = "Bright Head",
                    Category = "Iluminación",
                    Description = "Linterna frontal 1000 lúmenes. Resistente al agua. Batería de larga duración. 3 modos.",
                    Price = 55m,
                    ImageUrl = "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=400&h=400&fit=crop",
                    SecondaryImageUrl = "https://images.unsplash.com/photo-1600788594351-49a4af9f0e9f?w=400&h=400&fit=crop",
                    Stock = 40,
                    IsFeatured = false,
                    IsNew = false,
                    IsBestSeller = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-50)
                },
                new Product
                {
                    Name = "Linterna Camping LED",
                    Brand = "Camp Light",
                    Category = "Iluminación",
                    Description = "Linterna de camping con panel LED. 500 lúmenes. Impermeable. Asa para colgar.",
                    Price = 35m,
                    ImageUrl = "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=400&h=400&fit=crop",
                    Stock = 55,
                    IsFeatured = false,
                    IsNew = true,
                    IsBestSeller = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-12)
                },

                // === ACCESORIOS ===
                new Product
                {
                    Name = "Brújula Multifuncional",
                    Brand = "Navigate Pro",
                    Category = "Accesorios",
                    Description = "Brújula de precisión. Inclinómetro. Ideal para orientación en montaña.",
                    Price = 25m,
                    ImageUrl = "https://images.unsplash.com/photo-1577401132019-40a53b4fd6f0?w=400&h=400&fit=crop",
                    Stock = 100,
                    IsFeatured = false,
                    IsNew = false,
                    IsBestSeller = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-75)
                },
                new Product
                {
                    Name = "Kit Reparación Mochilas",
                    Brand = "Repair Kit Pro",
                    Category = "Accesorios",
                    Description = "Kit de reparación: aguja, hilo, parches. Esencial para viajes de trekking.",
                    Price = 15m,
                    ImageUrl = "https://images.unsplash.com/photo-1606611282519-d91603135d4e?w=400&h=400&fit=crop",
                    Stock = 120,
                    IsFeatured = false,
                    IsNew = false,
                    IsBestSeller = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-90)
                },

                // === SEGURIDAD Y ORIENTACIÓN ===
                new Product
                {
                    Name = "Casco Montaña Ajustable",
                    Brand = "Safe Peak",
                    Category = "Seguridad y orientación",
                    Description = "Casco de montaña certificado. Ventilación superior. Muy ligero (250g). Certificación EN 12492.",
                    Price = 120m,
                    ImageUrl = "https://images.unsplash.com/photo-1577401132019-40a53b4fd6f0?w=400&h=400&fit=crop",
                    SecondaryImageUrl = "https://images.unsplash.com/photo-1578342976847-6eae6c2ec866?w=400&h=400&fit=crop",
                    Stock = 30,
                    IsFeatured = false,
                    IsNew = false,
                    IsBestSeller = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-85)
                },
                new Product
                {
                    Name = "GPS Portátil Trail",
                    Brand = "GPS Navigator",
                    Category = "Seguridad y orientación",
                    Description = "GPS portátil para senderismo. Resistente al agua. Mapas topográficos incluidos. Batería 20h.",
                    Price = 280m,
                    PromotionalPrice = 220m,
                    ImageUrl = "https://images.unsplash.com/photo-1577401132019-40a53b4fd6f0?w=400&h=400&fit=crop",
                    Stock = 16,
                    IsFeatured = true,
                    IsNew = true,
                    IsBestSeller = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                }
            };

            context.Products.AddRange(products);
            await context.SaveChangesAsync();
        }
    }
}
