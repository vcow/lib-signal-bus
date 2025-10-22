using System;
using System.Collections.Generic;
using System.Linq;
using Plugins.vcow.SignalBus;
using R3;
using UnityEngine.Assertions;

namespace SignalsSystem
{
	public static class R3ObservableExtension
	{
		// ReSharper disable once InconsistentNaming
		private class _ObserveSignal<T> : Observable<T>, IDisposable
		{
			private readonly SignalBus _signalBus;
			private readonly HashSet<Observer<T>> _observers = new();
			private readonly List<Observer<T>> _observersBuffer = new();

			public _ObserveSignal(SignalBus signalBus)
			{
				_signalBus = signalBus;
				_signalBus.Subscribe<T>(SignalHandler);
			}

			public void Dispose()
			{
				_observers.Clear();
				_signalBus.Unsubscribe<T>(SignalHandler);
			}

			private void SignalHandler(T signal)
			{
				Assert.IsFalse(_observersBuffer.Any());
				_observersBuffer.AddRange(_observers);
				_observersBuffer.ForEach(observer => observer.OnNext(signal));
				_observersBuffer.Clear();
			}

			protected override IDisposable SubscribeCore(Observer<T> observer)
			{
				_observers.Add(observer);
				return this;
			}
		}

		public static Observable<T> ObserveSignal<T>(this SignalBus signalBus)
		{
			return new _ObserveSignal<T>(signalBus);
		}
	}
}