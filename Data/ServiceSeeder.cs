using EcommerceApp.Models;

namespace EcommerceApp.Data
{
    /// <summary>
    /// Seeder de servicios/rutas para datos de prueba.
    /// Ejecutar una sola vez para poblar la base de datos con rutas de muestra.
    /// </summary>
    public static class ServiceSeeder
    {
        public static async Task SeedServicesAsync(ApplicationDbContext context)
        {
            // Si ya hay servicios, no hacer nada
            if (context.Services.Any())
                return;

            var services = new List<Service>
            {
                // === COCHABAMBA ===
                new Service
                {
                    Name = "Parque Nacional Tunari",
                    Category = "Senderismo",
                    Region = "Cochabamba",
                    Location = "Cochabamba",
                    ShortDescription = "Ruta de senderismo por el parque con vistas panorámicas de la ciudad.",
                    Description = "Explora el Parque Nacional Tunari, una reserva natural ubicada en las alturas de Cochabamba. Esta ruta te llevará por senderos de montaña con vistas espectaculares de la ciudad y valles circundantes. Ideal para quienes buscan una experiencia cercana a la naturaleza sin alejarse demasiado de la urbe.",
                    Difficulty = RouteDifficulty.Moderate,
                    DifficultyDescription = "Recorrido de 8 km con desnivel de 400 metros. Requiere una condición física básica. El terreno es mayormente firme con algunas zonas rocosas.",
                    DistanceKm = 8.5,
                    DurationHours = 4,
                    Duration = "4 horas",
                    StartPoint = "Entrada del Parque Nacional Tunari",
                    EndPoint = "Mirador principal",
                    StartLatitude = -17.3333,
                    StartLongitude = -66.1667,
                    MaxGroupSize = 12,
                    Price = 120m,
                    PriceDescription = "Bs. 120 / persona",
                    ImageUrl = "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=800&h=600&fit=crop",
                    SecondaryImageUrl = "https://images.unsplash.com/photo-1464822759023-fed622ff2c3b?w=800&h=600&fit=crop",
                    GalleryImages = "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=800&h=600&fit=crop|https://images.unsplash.com/photo-1464822759023-fed622ff2c3b?w=800&h=600&fit=crop|https://images.unsplash.com/photo-1441974231531-c6227db76b6e?w=800&h=600&fit=crop",
                    Includes = "Guía certificado|Seguro de accidentes|Agua embotellada|Snack energético",
                    Excludes = "Transporte hasta el punto de encuentro|Comidas principales|Equipamiento personal",
                    Recommendations = "Llevar calzado de trekking o deportivo con buen agarre|Protección solar (bloqueador, gorra, lentes)|Mínimo 1.5 litros de agua adicional|Ropa cómoda y en capas|Bastones de trekking opcionales",
                    Status = "Active",
                    IsFeatured = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-15)
                },
                new Service
                {
                    Name = "Laguna Angostura",
                    Category = "Caminatas de naturaleza",
                    Region = "Cochabamba",
                    Location = "Cliza, Cochabamba",
                    ShortDescription = "Caminata relajada alrededor de una hermosa laguna natural.",
                    Description = "La Laguna Angostura es un destino perfecto para una caminata tranquila en familia. Este circuito bordea la laguna ofreciendo vistas del agua, aves locales y paisajes de valle. Es una ruta ideal para principiantes y familias con niños.",
                    Difficulty = RouteDifficulty.Easy,
                    DifficultyDescription = "Recorrido plano de 5 km alrededor de la laguna. Apto para todas las edades. Terreno mayormente llano y bien definido.",
                    DistanceKm = 5.2,
                    DurationHours = 2.5,
                    Duration = "2.5 horas",
                    StartPoint = "Área de descanso de Laguna Angostura",
                    EndPoint = "Mismo punto de partida (circuito)",
                    MaxGroupSize = 15,
                    Price = 80m,
                    PriceDescription = "Bs. 80 / persona",
                    ImageUrl = "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=800&h=600&fit=crop",
                    SecondaryImageUrl = "https://images.unsplash.com/photo-1441974231531-c6227db76b6e?w=800&h=600&fit=crop",
                    GalleryImages = "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=800&h=600&fit=crop|https://images.unsplash.com/photo-1441974231531-c6227db76b6e?w=800&h=600&fit=crop",
                    Includes = "Guía local|Seguro básico|Entrada a la reserva",
                    Excludes = "Transporte|Alimentación|Binoculares para avistamiento de aves",
                    Recommendations = "Calzado cómodo (zapatillas son suficientes)|Sombrero y bloqueador solar|Cámara fotográfica|Snacks ligeros|Repelente de insectos",
                    Status = "Active",
                    IsFeatured = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-20)
                },
                new Service
                {
                    Name = "Circuito Pairumani",
                    Category = "Senderismo",
                    Region = "Cochabamba",
                    Location = "Tiquipaya, Cochabamba",
                    ShortDescription = "Sendero entre bosques y jardines naturales.",
                    Description = "Pairumani ofrece un recorrido único combinando bosques de pinos, jardines botánicos y arquitectura colonial. Esta ruta es perfecta para quienes buscan una experiencia tranquila en contacto con la naturaleza sin grandes esfuerzos físicos.",
                    Difficulty = RouteDifficulty.Easy,
                    DifficultyDescription = "Caminata de 4 km con terreno mayormente plano. Algunas pequeñas pendientes. Apto para todas las edades.",
                    DistanceKm = 4.0,
                    DurationHours = 2,
                    Duration = "2 horas",
                    StartPoint = "Entrada principal Pairumani",
                    EndPoint = "Jardines botánicos",
                    MaxGroupSize = 12,
                    Price = 90m,
                    PriceDescription = "Bs. 90 / persona",
                    ImageUrl = "https://images.unsplash.com/photo-1511497584788-876760111969?w=800&h=600&fit=crop",
                    SecondaryImageUrl = "https://images.unsplash.com/photo-1501594907352-04cda38ebc29?w=800&h=600&fit=crop",
                    Includes = "Guía|Entrada al complejo|Degustación de productos locales",
                    Excludes = "Transporte|Comidas completas",
                    Recommendations = "Calzado deportivo o de senderismo ligero|Protección solar|Agua|Cámara para fotografías",
                    Status = "Active",
                    IsFeatured = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-25)
                },

                // === BOLIVIA ===
                new Service
                {
                    Name = "Choro Trek - La Paz",
                    Category = "Trekking",
                    Region = "Bolivia",
                    Location = "La Paz",
                    ShortDescription = "El clásico trekking de 3 días desde los Andes hasta los Yungas.",
                    Description = "El Choro es uno de los trekkings más famosos de Bolivia. Desciendes desde los 4,800 msnm hasta los 1,200 msnm en los Yungas. Atraviesas paisajes andinos, bosques nublados y valles subtropicales. Una experiencia de inmersión completa en la naturaleza boliviana.",
                    Difficulty = RouteDifficulty.Difficult,
                    DifficultyDescription = "Trekking exigente de 3 días con 60 km de recorrido. Desnivel acumulado de 3,600 metros. Requiere muy buena condición física y experiencia previa en trekkings de varios días.",
                    DistanceKm = 60,
                    DurationHours = 72,
                    Duration = "3 días / 2 noches",
                    StartPoint = "La Cumbre - La Paz",
                    EndPoint = "Coroico - Yungas",
                    StartLatitude = -16.3,
                    StartLongitude = -68.0,
                    EndLatitude = -16.2,
                    EndLongitude = -67.7,
                    MaxGroupSize = 10,
                    Price = 850m,
                    PriceDescription = "Bs. 850 / persona (incluye 2 noches)",
                    ImageUrl = "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=800&h=600&fit=crop",
                    SecondaryImageUrl = "https://images.unsplash.com/photo-1464822759023-fed622ff2c3b?w=800&h=600&fit=crop",
                    GalleryImages = "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=800&h=600&fit=crop|https://images.unsplash.com/photo-1464822759023-fed622ff2c3b?w=800&h=600&fit=crop|https://images.unsplash.com/photo-1441974231531-c6227db76b6e?w=800&h=600&fit=crop|https://images.unsplash.com/photo-1511497584788-876760111969?w=800&h=600&fit=crop",
                    Includes = "Guía certificado|Cocinero|Alimentación completa (desayuno, almuerzo, cena)|Transporte La Paz - La Cumbre|Transporte Coroico - La Paz|Equipo de campamento (carpas, sleeping bags)|Seguro de accidentes",
                    Excludes = "Equipamiento personal (mochila, botas, ropa)|Propinas|Bebidas alcohólicas|Snacks adicionales",
                    Recommendations = "Botas de trekking impermeables obligatorias|Mochila de 40-50L|Saco de dormir (se proporciona pero puede llevar el suyo)|Bastones de trekking recomendados|Ropa impermeable|Medicamentos personales y mal de altura|Linterna frontal|Protección solar extrema|Mínimo 3 litros de capacidad de agua",
                    Status = "Active",
                    IsFeatured = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                },
                new Service
                {
                    Name = "Salar de Uyuni Trekking",
                    Category = "Trekking",
                    Region = "Bolivia",
                    Location = "Uyuni, Potosí",
                    ShortDescription = "Caminata por el desierto de sal más grande del mundo.",
                    Description = "Experimenta el Salar de Uyuni de una forma única: caminando. Esta ruta te lleva por el corazón del salar, visitando formaciones de sal, islas de cactus gigantes y paisajes surrealistas. Una aventura fotográfica incomparable.",
                    Difficulty = RouteDifficulty.Moderate,
                    DifficultyDescription = "Caminata de 12 km sobre superficie plana de sal. El desafío principal es la altitud (3,650 msnm) y el sol intenso. Requiere aclimatación previa.",
                    DistanceKm = 12,
                    DurationHours = 6,
                    Duration = "1 día (6 horas)",
                    StartPoint = "Colchani - Uyuni",
                    EndPoint = "Isla Incahuasi",
                    StartLatitude = -20.3,
                    StartLongitude = -66.8,
                    MaxGroupSize = 12,
                    Price = 450m,
                    PriceDescription = "Bs. 450 / persona",
                    ImageUrl = "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=800&h=600&fit=crop",
                    SecondaryImageUrl = "https://images.unsplash.com/photo-1464822759023-fed622ff2c3b?w=800&h=600&fit=crop",
                    Includes = "Guía especializado|Transporte 4x4 de apoyo|Almuerzo campestre|Agua y snacks|Entrada a Isla Incahuasi|Seguro",
                    Excludes = "Transporte desde/hacia Uyuni|Alojamiento|Comidas adicionales",
                    Recommendations = "Lentes de sol con protección UV alta obligatorios|Bloqueador solar factor 50+|Gorra y buff para protección|Calzado cerrado cómodo|Ropa en capas (frío en la mañana, calor al mediodía)|Medicamentos para el mal de altura|Cámara con baterías extra",
                    Status = "Active",
                    IsFeatured = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-18)
                },

                // === INTERNACIONAL ===
                new Service
                {
                    Name = "Machu Picchu - Camino Inca",
                    Category = "Trekking",
                    Region = "Internacional",
                    Location = "Cusco, Perú",
                    ShortDescription = "El legendario Camino Inca de 4 días hacia Machu Picchu.",
                    Description = "El trekking más icónico de Sudamérica. Sigue los antiguos caminos incas a través de montañas, bosques nublados y ruinas arqueológicas hasta llegar a Machu Picchu por la Puerta del Sol. Una experiencia que combina aventura, historia y paisajes inolvidables.",
                    Difficulty = RouteDifficulty.Difficult,
                    DifficultyDescription = "Trekking de 4 días con 43 km de recorrido. El segundo día incluye el ascenso al paso de Warmiwañusca (4,215 msnm). Requiere excelente condición física y aclimatación a la altitud.",
                    DistanceKm = 43,
                    DurationHours = 96,
                    Duration = "4 días / 3 noches",
                    StartPoint = "Km 82 - Cusco",
                    EndPoint = "Machu Picchu",
                    StartLatitude = -13.3,
                    StartLongitude = -72.2,
                    MaxGroupSize = 12,
                    Price = 2800m,
                    PriceDescription = "Bs. 2,800 / persona (todo incluido)",
                    ImageUrl = "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=800&h=600&fit=crop",
                    SecondaryImageUrl = "https://images.unsplash.com/photo-1587595431973-160d0d94add1?w=800&h=600&fit=crop",
                    GalleryImages = "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=800&h=600&fit=crop|https://images.unsplash.com/photo-1587595431973-160d0d94add1?w=800&h=600&fit=crop|https://images.unsplash.com/photo-1531968455001-5c5272a41129?w=800&h=600&fit=crop",
                    Includes = "Guía profesional bilingüe|Porteadores|Chef de campamento|Alimentación completa|Equipo de campamento|Transporte Cusco - Km 82|Tren Aguas Calientes - Ollantaytambo|Bus Machu Picchu|Entrada a Machu Picchu|Seguro de viaje",
                    Excludes = "Vuelos internacionales|Alojamiento en Cusco|Comidas en Cusco|Propinas|Sleeping bag (se puede alquilar)|Bastones de trekking (se pueden alquilar)",
                    Recommendations = "Reservar con 6 meses de anticipación mínimo|Botas de trekking bien probadas|Mochila de 50-60L|Bastones de trekking obligatorios|Ropa impermeable de calidad|Aclimatación previa en Cusco (3 días)|Documentos: pasaporte, permiso de ingreso|Medicamentos personales y mal de altura|Linterna frontal|Protección solar extrema|Sistema de hidratación de 3L",
                    Status = "Active",
                    IsFeatured = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new Service
                {
                    Name = "Torres del Paine - Circuito W",
                    Category = "Trekking con camping",
                    Region = "Internacional",
                    Location = "Patagonia, Chile",
                    ShortDescription = "Trekking de 5 días por uno de los paisajes más espectaculares del planeta.",
                    Description = "El Circuito W en Torres del Paine es considerado uno de los mejores trekkings del mundo. Atraviesa glaciares, lagos turquesa, bosques lenga y las icónicas torres de granito. Una experiencia de naturaleza pura en la Patagonia chilena.",
                    Difficulty = RouteDifficulty.Difficult,
                    DifficultyDescription = "Trekking exigente de 5 días con 80 km de recorrido. Incluye ascensos prolongados y condiciones climáticas variables. Requiere experiencia en trekking de altura y excelente condición física.",
                    DistanceKm = 80,
                    DurationHours = 120,
                    Duration = "5 días / 4 noches",
                    StartPoint = "Hotel Las Torres",
                    EndPoint = "Guardería Grey",
                    StartLatitude = -51.0,
                    StartLongitude = -73.0,
                    MaxGroupSize = 10,
                    Price = 4500m,
                    PriceDescription = "Bs. 4,500 / persona (todo incluido)",
                    ImageUrl = "https://images.unsplash.com/photo-1464822759023-fed622ff2c3b?w=800&h=600&fit=crop",
                    SecondaryImageUrl = "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=800&h=600&fit=crop",
                    GalleryImages = "https://images.unsplash.com/photo-1464822759023-fed622ff2c3b?w=800&h=600&fit=crop|https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=800&h=600&fit=crop|https://images.unsplash.com/photo-1441974231531-c6227db76b6e?w=800&h=600&fit=crop",
                    Includes = "Guía certificado CONAF|Transporte desde/hacia Puerto Natales|Entrada al parque (4 días)|Alimentación completa|Equipo de campamento|Sleeping bags extremos|Seguro de rescate de montaña",
                    Excludes = "Vuelos internacionales|Alojamiento en Puerto Natales|Equipamiento personal|Bastones de trekking|Comidas en Puerto Natales|Propinas",
                    Recommendations = "Época recomendada: Diciembre a Marzo|Ropa impermeable y cortaviento de alta calidad obligatoria|Botas de trekking impermeables|Capas térmicas (puede nevar)|Bastones de trekking obligatorios|Gafas de sol y protección solar|Guantes y gorro térmico|Mochila 60-70L|Sistema de hidratación|Documentos y seguro de viaje internacional",
                    Status = "Active",
                    IsFeatured = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-12)
                },
                new Service
                {
                    Name = "Kilimanjaro - Ruta Machame",
                    Category = "Alta montaña",
                    Region = "Internacional",
                    Location = "Tanzania, África",
                    ShortDescription = "Ascenso al techo de África por la ruta más escénica.",
                    Description = "El Kilimanjaro (5,895 msnm) es el pico más alto de África y uno de los Siete Cumbres. La ruta Machame, conocida como la ruta Whiskey, es la más popular por su belleza y mejores tasas de éxito. Un desafío de alta montaña sin necesidad de experiencia técnica en escalada.",
                    Difficulty = RouteDifficulty.Extreme,
                    DifficultyDescription = "Ascenso extremadamente exigente de 7 días. La altitud es el principal desafío (5,895 msnm). El día cumbre puede alcanzar -20°C. Requiere excelente condición física, determinación mental y aclimatación gradual. Tasa de éxito: 75%.",
                    DistanceKm = 62,
                    DurationHours = 168,
                    Duration = "7 días / 6 noches",
                    StartPoint = "Machame Gate (1,800 msnm)",
                    EndPoint = "Cumbre Uhuru Peak (5,895 msnm)",
                    StartLatitude = -3.0674,
                    StartLongitude = 37.3556,
                    MaxGroupSize = 8,
                    Price = 12000m,
                    PriceDescription = "Bs. 12,000 / persona (paquete completo)",
                    ImageUrl = "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=800&h=600&fit=crop",
                    SecondaryImageUrl = "https://images.unsplash.com/photo-1464822759023-fed622ff2c3b?w=800&h=600&fit=crop",
                    GalleryImages = "https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=800&h=600&fit=crop|https://images.unsplash.com/photo-1464822759023-fed622ff2c3b?w=800&h=600&fit=crop|https://images.unsplash.com/photo-1441974231531-c6227db76b6e?w=800&h=600&fit=crop|https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=800&h=600&fit=crop",
                    Includes = "Guía certificado de montaña|Porteadores|Chef de campamento|Alimentación completa durante el ascenso|Equipo de campamento completo (carpas 4 estaciones, sleeping bags extremos)|Entrada al Parque Nacional|Certificado de cumbre|Rescate de emergencia|Seguro de evacuación|Transporte desde/hacia Moshi",
                    Excludes = "Vuelos internacionales|Visa de Tanzania (USD 50)|Alojamiento pre/post trekking|Propinas (USD 250-300 recomendado)|Equipamiento personal|Seguro de viaje internacional obligatorio|Medicamentos para mal de altura",
                    Recommendations = "Consulta médica previa obligatoria|Preparación física de 4-6 meses|Vacunas: fiebre amarilla obligatoria, hepatitis A/B recomendadas|Medicamentos: Diamox para mal de altura|Botas de montaña 4 estaciones|Ropa térmica extrema (hasta -20°C)|Bastones de trekking obligatorios|Saco de dormir extremo (-15°C mínimo)|Mochila pequeña para día cumbre|Linterna frontal potente|Protección solar extrema|Sistema de hidratación 3L|Batería portátil|Documentos: pasaporte con 6 meses validez",
                    Status = "Active",
                    IsFeatured = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-8)
                }
            };

            context.Services.AddRange(services);
            await context.SaveChangesAsync();
        }
    }
}
