#region using block

using System;

#endregion

namespace MyProject.RTC.Models
{
    public class PeerConnection : IPeerConnection
    {
        public Guid Context { get; set; }
        public Guid PeerId { get; set; }
    }
}