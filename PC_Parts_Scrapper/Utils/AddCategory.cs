namespace PC_Parts_Scrapper.Utils
{
    public static class AddCategory
    {
        public static string Addcategory(string productName)
        {
            List<string> gpus = new List<string> 
            {
                "RTX","GTX","GT","RADEON","RX","`ARC"
            };

            List<String> cpus = new List<string>
            {
                "RYZEN","INTEL","CORE","AMD"
            };
            string upperName = productName.ToUpper();
            if (gpus.Any(gpu => upperName.Contains(gpu)))
            {
                if (upperName.Contains("RYZEN") || upperName.Contains("CORE I") || upperName.Contains("THREADRIPPER"))
                {
                    return "CPU";
                }
                return "GPU";
            }
            else if (cpus.Any(cpu => upperName.Contains(cpu)))
            {
                return "CPU";
            }
            else
            {
                return "Other";
            }
        }
    }
}
