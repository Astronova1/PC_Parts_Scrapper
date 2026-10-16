using System.Text.RegularExpressions;

namespace PC_Parts_Scrapper.Helpers
{
    public static class ProductParser
    {
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


        // Extracts the AIB/OEM brand from a listing title, e.g. "Asus RTX 4070 Dual OC" -> "Asus".
        // Returns null when no known brand token is found.
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
