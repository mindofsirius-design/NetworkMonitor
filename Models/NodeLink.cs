using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkMonitor.Models
{
    public class NodeLink
    {
        public string Id { get; set; } = System.Guid.NewGuid().ToString();
        public string SourceNodeId { get; set; }
        public string TargetNodeId { get; set; }
        public string Label { get; set; }
    }
}
