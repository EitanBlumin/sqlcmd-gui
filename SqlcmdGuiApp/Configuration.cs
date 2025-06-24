using System.Collections.Generic;

namespace SqlcmdGuiApp
{
    public class Configuration
    {
        public string FilePath { get; set; } = string.Empty;
        public string Server { get; set; } = string.Empty;
        public string Database { get; set; } = string.Empty;
        public bool UseSqlAuth { get; set; }
        public string User { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool Encrypt { get; set; }
        public bool TrustServerCertificate { get; set; }
        public bool ReadOnlyIntent { get; set; }
        public List<SqlParameter> Parameters { get; set; } = new();
    }
}
