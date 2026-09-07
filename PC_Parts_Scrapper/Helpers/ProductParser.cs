using System.Text.RegularExpressions;

namespace PC_Parts_Scrapper.Helpers
{
    /// <summary>
    /// Centralises the regex-based extraction of listing metadata (brand, graphics chip vendor)
    /// from scraped product titles, so scraping and backfill use identical logic.
    /// </summary>
    public static class ProductParser
    {
        // Ordered roughly by how AIB/OEM names appear in CZone & ZahComputers titles.
        // "PNY" and "XFX" are short enough to false-match inside other words, so they use
        // word boundaries like everything else here.
        private static readonly string[] KnownBrands = new[]
        {
            "ASUS", "MSI", "GIGABYTE", "ASROCK", "COLORFUL", "ZOTAC", "INNO3D",
            "PALIT", "POWERCOLOR", "SAPPHIRE", "XFX", "GALAX", "GAINWARD",
            "BIOSTAR", "EVGA", "PNY", "YEYIAN", "INTEL", "AMD"
        };

        private static readonly Regex GraphicsVendorPattern =
            new(@"(?i)\b(RTX|GTX|GT)\b", RegexOptions.Compiled);

        private static readonly Regex AmdGpuPattern =
            new(@"(?i)\b(RX|RADEON)\b", RegexOptions.Compiled);

        /// <summary>
        /// Extracts the AIB/OEM brand from a listing title, e.g. "Asus RTX 4070 Dual OC" -> "Asus".
        /// Returns null when no known brand token is found.
        /// </summary>
        public static string? ExtractBrand(string? title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return null;

            foreach (var brand in KnownBrands)
            {
                if (Regex.IsMatch(title, $@"(?i)\b{Regex.Escape(brand)}\b"))
                {
                    return brand[0] + brand.Substring(1).ToLowerInvariant();
                }
            }

            return null;
        }

        /// <summary>
        /// Extracts the graphics chip vendor ("NVIDIA"/"AMD") for GPU listings.
        /// Returns null for non-GPU categories or when the title doesn't match a known chip family.
        /// </summary>
        public static string? ExtractGraphicsType(string? title, string? categoryName)
        {
            if (string.IsNullOrWhiteSpace(title) || !string.Equals(categoryName, "GPU", StringComparison.OrdinalIgnoreCase))
                return null;

            if (GraphicsVendorPattern.IsMatch(title))
                return "NVIDIA";

            if (AmdGpuPattern.IsMatch(title))
                return "AMD";

            return null;
        }
    }
}
