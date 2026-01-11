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
    public class SlugService : ISlugService
    {
        private readonly IProductGroupRepository _groupRepo;

        public SlugService(IProductGroupRepository groupRepo)
        {
            _groupRepo = groupRepo;
        }

        public string GenerateGroupSlug(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;
            var normalized = name.Trim().ToLowerInvariant();
            normalized = normalized.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (var ch in normalized)
            {
                var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            normalized = sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
            normalized = Regex.Replace(normalized, "[^a-z0-9]+", "-");
            normalized = Regex.Replace(normalized, "-+", "-").Trim('-');
            if (string.IsNullOrWhiteSpace(normalized)) normalized = Guid.NewGuid().ToString("n").Substring(0, 8);
            return normalized;
        }

        public async Task<string> EnsureUniqueGroupSlugAsync(string baseSlug, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(baseSlug)) baseSlug = GenerateGroupSlug(Guid.NewGuid().ToString());
            var candidate = baseSlug;
            var suffix = 1;
            while (await _groupRepo.GetBySlugOrObjectIdAsync(candidate, ct) != null || await _groupRepo.ExistsByObjectIdAsync(candidate, ct)) 
                while (await _groupRepo.GetBySlugOrObjectIdAsync(candidate, ct) != null || await _groupRepo.ExistsByObjectIdAsync(candidate, ct))
                {
                    candidate = $"{baseSlug}-{suffix}";
                    suffix++;
                    if (suffix > 1000) candidate = $"{baseSlug}-{Guid.NewGuid().ToString("n").Substring(0, 8)}";
                }
            return candidate;
        }
    }
}