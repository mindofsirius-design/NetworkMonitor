using System;
using System.Collections.Generic;

namespace NetworkMonitor.Models
{
    public class RouteGroup
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        /// <summary>Порядок: от ближайшего к источнику к дальнему.</summary>
        public List<string> OrderedNodeIds { get; set; } = new List<string>();
        public List<string> SharedHops { get; set; } = new List<string>();
    }
}