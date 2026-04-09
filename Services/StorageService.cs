using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using NetworkMonitor.Models;
using System.Collections.Generic;
using System.IO;

namespace NetworkMonitor.Services
{
    public class StorageService
    {
        private readonly string _nodesFile = "nodes.json";
        private readonly string _linksFile = "links.json";
        private readonly string _settingsFile = "settings.json";

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
            return JsonConvert.DeserializeObject<AppSettings>(File.ReadAllText(_settingsFile))
                   ?? new AppSettings();
        }

        public void SaveSettings(AppSettings settings)
        {
            File.WriteAllText(_settingsFile, JsonConvert.SerializeObject(settings, Formatting.Indented));
        }
    }
}