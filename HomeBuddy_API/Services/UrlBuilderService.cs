using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HomeBuddy_API.Data;
using HomeBuddy_API.Repositories;
using HomeBuddy_API.Interfaces.SlugURLInterfaces;
using HomeBuddy_API.Interfaces.ProductInterfaces;
using HomeBuddy_API.Models;


namespace HomeBuddy_API.Services
{
   public class UrlBuilder : IUrlBuilderService
    {
        private readonly string _baseUrl;

        public UrlBuilder(string? baseUrl = null)
        {
            _baseUrl = baseUrl?.TrimEnd('/') ?? string.Empty;
        }

        public string BaseUrl => _baseUrl;

        public string GroupUrl(string slugOrObjectId, string? sku = null)
        {
            if (string.IsNullOrWhiteSpace(slugOrObjectId))
                throw new ArgumentNullException(nameof(slugOrObjectId));

            // declare 'url' ONCE
            var url = $"/products/{Uri.EscapeDataString(slugOrObjectId)}";

            if (!string.IsNullOrWhiteSpace(sku))
                url += $"?sku={Uri.EscapeDataString(sku)}";

            return PrependBase(url);
        }

        public string VariantRedirectUrl(string sku)
        {
            if (string.IsNullOrWhiteSpace(sku))
                throw new ArgumentNullException(nameof(sku));

            var url = $"/p/{Uri.EscapeDataString(sku)}";
            return PrependBase(url);
        }

        private string PrependBase(string path)
            => string.IsNullOrEmpty(BaseUrl) ? path : $"{BaseUrl}{path}";
    }
}
