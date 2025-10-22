using System;
using Plugins.vcow.SignalBus;

namespace SignalsSystem
{
	public static class CommandsExtension
	{
		public static void FireCommand<T>(this SignalBus signalBus, T command) where T : ICommand
		{
			var numSubscriptions = signalBus.GetNumSubscriptions<T>();
			if (numSubscriptions > 1)
			{
				throw new Exception("Command can't have more than one subscription.");
			}

			if (numSubscriptions < 1)
			{
				throw new Exception("Command must have subscription.");
			}

			numSubscriptions = signalBus.Fire(command);

			if (numSubscriptions != 1)
			{
				throw new Exception("Command has been received more or less than once.");
			}
		}

		public static void SubscribeCommand<T>(this SignalBus signalBus, Action<T> handler) where T : ICommand
		{
			if (signalBus.GetNumSubscriptions<T>() > 0)
			{
				throw new Exception("Command can have only one subscription.");
			}

			signalBus.Subscribe(handler);
		}
	}
}