using System;
using Meadow;
using Meadow.Foundation;
using Meadow.Foundation.Sensors.Temperature;
using Meadow.Foundation.Graphics;
using Meadow.Foundation.Displays;
using Meadow.Foundation.Relays;
using Meadow.Devices;
using Meadow.Hardware;
using Meadow.Gateway.WiFi;
using Meadow.Units;
using System;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using NewCode.Web;
using NETDuinoWar;

public class Class1
{
	public Class1()
	{
		var relay = new Meadow.Foundation.Relays.Relay(Device.CreateDigitalOutputPort(Device.Pins.D05));
		while (true)
        {
			Thread.Sleep(500);
            relay.Toggle();
			Console.WriteLine(relay.isOn);
        }
    }
}
