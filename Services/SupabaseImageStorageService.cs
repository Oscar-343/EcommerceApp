using System.Net.Http.Headers;

namespace EcommerceApp.Services
{
    // Sube archivos al Storage de Supabase usando su API REST y devuelve la URL pública.
    //
    // Necesita en appsettings.json (sección "Supabase"):
    //   "Url"            -> URL del proyecto, ej: https://xxxx.supabase.co
    //   "ServiceRoleKey" -> Service Role Key del proyecto (Project Settings > API).
    //                       Se usa SOLO del lado del servidor, nunca se expone al navegador.
    //   "Bucket"         -> nombre del bucket público donde se guardan las imágenes.
    public class SupabaseImageStorageService : IImageStorageService
    {
        private readonly HttpClient _http;
        private readonly string _url;
        private readonly string _serviceRoleKey;
        private readonly string _bucket;
        private readonly ILogger<SupabaseImageStorageService> _logger;

        public SupabaseImageStorageService(HttpClient http, IConfiguration configuration, ILogger<SupabaseImageStorageService> logger)
        {
            _http = http;
            _logger = logger;
            _url = (configuration["Supabase:Url"] ?? "").TrimEnd('/');
            _serviceRoleKey = configuration["Supabase:ServiceRoleKey"] ?? "";
            _bucket = configuration["Supabase:Bucket"] ?? "product-image";
        }

        public async Task<string> UploadAsync(Stream content, string fileName, string contentType, string folder)
        {
            if (string.IsNullOrWhiteSpace(_url) || string.IsNullOrWhiteSpace(_serviceRoleKey))
            {
                throw new InvalidOperationException(
                    "Supabase no está configurado. Definí Supabase:Url y Supabase:ServiceRoleKey en appsettings.json o en los secretos de usuario.");
            }

            var extension = Path.GetExtension(fileName);
            if (string.IsNullOrWhiteSpace(extension)) extension = ".jpg";
            var safeFolder = string.IsNullOrWhiteSpace(folder) ? "misc" : folder.Trim('/');
            var objectPath = $"{safeFolder}/{Guid.NewGuid():N}{extension}";

            using var streamContent = new StreamContent(content);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);

            var requestUrl = $"{_url}/storage/v1/object/{_bucket}/{objectPath}";
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl) { Content = streamContent };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _serviceRoleKey);
            request.Headers.Add("apikey", _serviceRoleKey);
            request.Headers.Add("x-upsert", "true");

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error al subir imagen a Supabase Storage ({Status}): {Body}", response.StatusCode, body);
                throw new InvalidOperationException($"No se pudo subir la imagen a Supabase (HTTP {(int)response.StatusCode}).");
            }

            // URL pública: solo funciona si el bucket está marcado como "Public" en Supabase.
            return $"{_url}/storage/v1/object/public/{_bucket}/{objectPath}";
        }
    }
}
