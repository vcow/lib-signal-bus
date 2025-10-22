using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace Plugins.vcow.SignalBus
{
#if UNITY_INCLUDE_TESTS
	public class SignalBus : IDisposable
	{
		protected readonly List<SignalBus> _children = new();

		protected SignalBus _parent;
		protected volatile int _isDisposed;
		protected readonly object _lock = new();
#else
	public sealed class SignalBus : IDisposable
	{
		private readonly List<SignalBus> _children = new();

		private SignalBus _parent;
		private volatile int _isDisposed;
		private readonly object _lock = new();
#endif
		private readonly Dictionary<Type, HashSet<object>> _handlers = new();

		public SignalBus()
		{
		}

		public void Dispose()
		{
			lock (_lock)
			{
				if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
				{
					return;
				}

				if (_parent != null)
				{
					lock (_parent._lock)
					{
						_parent._children.Remove(this);
					}
				}

				foreach (var sub in _children.ToArray())
				{
					sub.Dispose();
				}

				_handlers.Clear();
			}
		}

		public SignalBus CreateSubBus()
		{
			if (_isDisposed != 0)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}

			lock (_lock)
			{
				var sub = new SignalBus();
				sub._parent = this;
				_children.Add(sub);
				return sub;
			}
		}

		public void Subscribe<T>(Action<T> handler)
		{
			if (_isDisposed != 0)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}

			lock (_lock)
			{
				if (!_handlers.TryGetValue(typeof(T), out var handlersSet))
				{
					handlersSet = new HashSet<object>();
					_handlers[typeof(T)] = handlersSet;
				}

				if (!handlersSet.Add(handler))
				{
					Debug.LogWarning("[SignalBus] Try to subscribe twice.");
				}
			}
		}

		public void Unsubscribe<T>(Action<T> handler)
		{
			if (_isDisposed != 0)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}

			lock (_lock)
			{
				if (_handlers.TryGetValue(typeof(T), out var handlersSet))
				{
					if (handlersSet.Remove(handler))
					{
						if (!handlersSet.Any())
						{
							_handlers.Remove(typeof(T));
						}
					}
				}
			}
		}

		public int GetNumSubscriptions<T>()
		{
			if (_isDisposed != 0)
			{
				return 0;
			}

			lock (_lock)
			{
				var result = _handlers.TryGetValue(typeof(T), out var handlersSet) ? handlersSet.Count : 0;
				foreach (var sub in _children)
				{
					result += sub.GetNumSubscriptions<T>();
				}

				return result;
			}
		}

		public int Fire<T>() where T : new()
		{
			return Fire(new T());
		}

		public int Fire<T>(T signal)
		{
			if (_isDisposed != 0)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}

			lock (_lock)
			{
				int result;
				if (_handlers.TryGetValue(typeof(T), out var handlers))
				{
					result = handlers.Count;
					foreach (var handler in handlers.Cast<Action<T>>())
					{
						handler.Invoke(signal);
					}
				}
				else
				{
					result = 0;
				}

				foreach (var sub in _children)
				{
					lock (sub._lock)
					{
						if (sub._isDisposed == 0)
						{
							result += sub.Fire(signal);
						}
					}
				}

				return result;
			}
		}
	}
}