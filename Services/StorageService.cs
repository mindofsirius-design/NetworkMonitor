using System;
using System.Security.Cryptography;
using System.Text;


using Newtonsoft.Json;
using NetworkMonitor.Models;
using System.Collections.Generic;
using System.IO;

namespace NetworkMonitor.Services
{
    public class StorageService
    {
        private static readonly string BaseDir = AppDomain.CurrentDomain.BaseDirectory;
        private readonly string _nodesFile = Path.Combine(BaseDir, "nodes.json");
        private readonly string _linksFile = Path.Combine(BaseDir, "links.json");
        private readonly string _settingsFile = Path.Combine(BaseDir, "settings.json");

        public List<NetworkNode> LoadNodes()
        {
            if (!File.Exists(_nodesFile)) return new List<NetworkNode>();
            return JsonConvert.DeserializeObject<List<NetworkNode>>(File.ReadAllText(_nodesFile))
                   ?? new List<NetworkNode>();
        }

        public void SaveNodes(IEnumerable<NetworkNode> nodes)
        {
            File.WriteAllText(_nodesFile, JsonConvert.SerializeObject(nodes, Formatting.Indented));
        }

        public List<NodeLink> LoadLinks()
        {
            if (!File.Exists(_linksFile)) return new List<NodeLink>();
            return JsonConvert.DeserializeObject<List<NodeLink>>(File.ReadAllText(_linksFile))
                   ?? new List<NodeLink>();
        }

        public void SaveLinks(IEnumerable<NodeLink> links)
        {
            File.WriteAllText(_linksFile, JsonConvert.SerializeObject(links, Formatting.Indented));
        }

        public AppSettings LoadSettings()
        {
            if (!File.Exists(_settingsFile)) return new AppSettings();
            var loaded = JsonConvert.DeserializeObject<AppSettings>(File.ReadAllText(_settingsFile))
                         ?? new AppSettings();
            loaded.SmtpPassword = Unprotect(loaded.SmtpPassword);   // дешифруем пароль
            return loaded;
        }

        public void SaveSettings(AppSettings settings)
        {
            var copy = JsonConvert.DeserializeObject<AppSettings>(JsonConvert.SerializeObject(settings));
            copy.SmtpPassword = Protect(copy.SmtpPassword); // шифруем пароль
            File.WriteAllText(_settingsFile, JsonConvert.SerializeObject(copy, Formatting.Indented));
        }

        public static string Protect(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;
            var bytes = Encoding.UTF8.GetBytes(plainText);
            var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encrypted);
        }

        public static string Unprotect(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;
            try
            {
                var bytes = Convert.FromBase64String(cipherText);
                var decrypted = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(decrypted);
            }
            catch { return string.Empty; }
        }

    }
}