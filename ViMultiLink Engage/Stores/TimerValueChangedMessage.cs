using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace ViMultiSync.Stores
{
    public class TimerValueChangedMessage : ValueChangedMessage<Dictionary<string, double>>
    {
        public TimerValueChangedMessage(Dictionary<string, double> timers, string buttonId, string value) : base(timers)
        {
            ButtonId = buttonId;
        }

        public string ButtonId { get; }
    }
}