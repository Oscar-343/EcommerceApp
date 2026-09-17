namespace EcommerceApp.Services
{
    // Abstracción para subir imágenes a un storage externo (Supabase Storage).
    public interface IImageStorageService
    {
        /// <summary>
        /// Sube un archivo y devuelve su URL pública.
        /// </summary>
        /// <param name="content">Contenido del archivo.</param>
        /// <param name="fileName">Nombre original (se usa solo para tomar la extensión).</param>
        /// <param name="contentType">Content-Type del archivo (ej: image/png).</param>
        /// <param name="folder">Subcarpeta dentro del bucket (ej: "products", "services").</param>
        Task<string> UploadAsync(Stream content, string fileName, string contentType, string folder);
    }
}
