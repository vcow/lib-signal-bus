using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

namespace Tests.EditMode
{
	public sealed class SignalBus : Plugins.vcow.SignalBus.SignalBus
	{
		public string Name { get; }

		public SignalBus(SignalBus parent, string name)
		{
			Name = name;
			_parent = parent;
		}

		public bool IsDisposed => _isDisposed != 0;

		public SignalBus CreateSubBus(string name)
		{
			if (_isDisposed != 0)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}

			lock (_lock)
			{
				var sub = new SignalBus(this, name);
				_children.Add(sub);
				return sub;
			}
		}
	}
	
	public class SignalBusTests
	{
		public abstract class SignalBase
		{
			public int i;
			public string s = string.Empty;
		}

		public sealed class SignalLvl0Mock : SignalBase
		{
		}

		public sealed class SignalLvl1Mock : SignalBase
		{
		}

		public sealed class SignalLvl2Mock : SignalBase
		{
		}

		public sealed class SignalLvl3Mock : SignalBase
		{
		}

		public sealed class SignalLvl4Mock : SignalBase
		{
		}

		private static int _globalSeed = Environment.TickCount;

		private static readonly ThreadLocal<Random> _random = new(() =>
		{
			var seed = Interlocked.Increment(ref _globalSeed);
			return new Random(seed);
		});

		/// <summary>
		/// Создается пять шин, каждая из которых, кроме первой, является дочерней по отношению к предыдущей.
		/// В шины пробрасываются сигналы пяти разных типов. Родительские шины не должны ловить сигналы от дочерних.
		/// </summary>
		[Test]
		public static void SignalBusHierarchyTest()
		{
			var sb0 = new SignalBus(null, "sb0");
			var sb1 = sb0.CreateSubBus("sb1");
			var sb2 = sb1.CreateSubBus("sb2");
			var sb3 = sb2.CreateSubBus("sb3");
			var sb4 = sb3.CreateSubBus("sb4");

			var buses = new[] { sb0, sb1, sb2, sb3, sb4 };
			var receivers = new Dictionary<int, int>();
			for (var i = 0; i < buses.Length; ++i)
			{
				SubscribeHierarchyBus(buses[i], receivers);
			}

			new List<Action>
				{
					() => FireAll<SignalLvl0Mock>(0),
					() => FireAll<SignalLvl1Mock>(1),
					() => FireAll<SignalLvl2Mock>(2),
					() => FireAll<SignalLvl3Mock>(3),
					() => FireAll<SignalLvl4Mock>(4)
				}
				.ForEach(action => action());

			return;

			void FireAll<T>(int i) where T : SignalBase, new()
			{
				for (var j = 0; j < buses.Length; ++j)
				{
					var signal = new T { i = i };
					buses[j].Fire(signal);
					Assert.IsTrue(receivers[i] == buses.Length - j);
					receivers.Clear();
				}
			}
		}

		private static void SubscribeHierarchyBus(SignalBus bus, Dictionary<int, int> receivers)
		{
			bus.Subscribe<SignalLvl0Mock>(handler);
			bus.Subscribe<SignalLvl1Mock>(handler);
			bus.Subscribe<SignalLvl2Mock>(handler);
			bus.Subscribe<SignalLvl3Mock>(handler);
			bus.Subscribe<SignalLvl4Mock>(handler);

			return;

			void handler(SignalBase signal)
			{
				receivers[signal.i] = receivers.TryGetValue(signal.i, out var i) ? i + 1 : 1;
			}
		}

		private static volatile int totalBuses;

		/// <summary>
		/// Создается пять шин, каждая из которых, кроме первой, является дочерней по отношению к предыдущей.
		/// Все шины спамятся из параллельных потоков разными сигналами. Должно нормально отработать параллельные вызовы.
		/// </summary>
		[Test]
		public static async void SignalBusMultithreadTest()
		{
			var receivers = new ConcurrentDictionary<int, int>();
			var statystics = new ConcurrentDictionary<int, int>();
			var tasks = new List<Task>();
			var sb0 = new SignalBus(null, "sb0");
			totalBuses = 5;
			tasks.Add(Task.Run(() => HierarchySignalThread<SignalLvl0Mock>(0, sb0, receivers, statystics)));
			var sb1 = sb0.CreateSubBus("sb1");
			tasks.Add(Task.Run(() => HierarchySignalThread<SignalLvl1Mock>(1, sb1, receivers, statystics)));
			var sb2 = sb1.CreateSubBus("sb2");
			tasks.Add(Task.Run(() => HierarchySignalThread<SignalLvl2Mock>(2, sb2, receivers, statystics)));
			var sb3 = sb2.CreateSubBus("sb3");
			tasks.Add(Task.Run(() => HierarchySignalThread<SignalLvl3Mock>(3, sb3, receivers, statystics)));
			var sb4 = sb3.CreateSubBus("sb4");
			tasks.Add(Task.Run(() => HierarchySignalThread<SignalLvl4Mock>(4, sb4, receivers, statystics)));

			await Task.WhenAll(tasks);

			Assert.AreEqual(0, totalBuses);
			Assert.AreEqual(receivers.Keys.Count, statystics.Keys.Count);
			foreach (var key in receivers.Keys)
			{
				Assert.AreEqual(receivers[key], statystics[key]);
			}
		}

		private static void HierarchySignalThread<T>(int index, SignalBus signalBus,
			ConcurrentDictionary<int, int> receivers, ConcurrentDictionary<int, int> statystics)
			where T : SignalBase, new()
		{
			Debug.Log($"Thread #{index} started.");

			signalBus.Subscribe<SignalLvl0Mock>(handler);
			signalBus.Subscribe<SignalLvl1Mock>(handler);
			signalBus.Subscribe<SignalLvl2Mock>(handler);
			signalBus.Subscribe<SignalLvl3Mock>(handler);
			signalBus.Subscribe<SignalLvl4Mock>(handler);

			double lifeTimeMs = _random.Value.Next(1000, 3000);
			var startTime = DateTime.Now;
			var ctr = 0;
			do
			{
				if (signalBus.IsDisposed)
				{
					Interlocked.Decrement(ref totalBuses);
					Debug.Log($"Thread #{index} finished.");
					return;
				}

				var sent = signalBus.Fire(new T { i = index, s = ctr++.ToString() });
				statystics[index] = statystics.TryGetValue(index, out var count) ? count + sent : sent;
				Debug.Log($"->>> {index} sent.");
				Task.Delay(10).Wait();
			} while ((DateTime.Now - startTime).TotalMilliseconds < lifeTimeMs);

			signalBus.Unsubscribe<SignalLvl0Mock>(handler);
			signalBus.Unsubscribe<SignalLvl1Mock>(handler);
			signalBus.Unsubscribe<SignalLvl2Mock>(handler);
			signalBus.Unsubscribe<SignalLvl3Mock>(handler);
			signalBus.Unsubscribe<SignalLvl4Mock>(handler);

			Interlocked.Decrement(ref totalBuses);
			signalBus.Dispose();

			Debug.Log($"Thread #{index} finished.");
			return;

			void handler(SignalBase signal)
			{
				if (signalBus.IsDisposed)
				{
					throw new ObjectDisposedException(nameof(SignalBus));
				}

				receivers[signal.i] = receivers.TryGetValue(signal.i, out var i) ? i + 1 : 1;
				Debug.Log($"<<<- {index} received from {signal.i}.");
			}
		}
	}
}