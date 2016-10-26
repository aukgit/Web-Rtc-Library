#region using block

using System;

#endregion

namespace MyProject.RTC.Models
{
    public interface ISignalingModel
    {
        string Message { get; set; }
        Guid Recipient { get; set; }
        Guid Sender { get; set; }
    }
}