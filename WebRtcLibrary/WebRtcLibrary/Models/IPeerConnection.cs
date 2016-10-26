#region using block

using System;

#endregion

namespace MyProject.RTC.Models
{
    public interface IPeerConnection
    {
        Guid Context { get; set; }
        Guid PeerId { get; set; }
    }
}