﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Model
{
	public interface IObjectEvent
	{
		Type Type();
		void Set(object value);
	}

	public abstract class ObjectEvent<T> : IObjectEvent
	{
		private T value;

		protected T Get()
		{
			return value;
		}

		public void Set(object v)
		{
			this.value = (T)v;
		}

		public Type Type()
		{
			return typeof(T);
		}
	}

	public sealed class ObjectEvents
	{
		private static ObjectEvents instance;

		public static ObjectEvents Instance
		{
			get
			{
				return instance ?? (instance = new ObjectEvents());
			}
		}

		private readonly Dictionary<string, Assembly> assemblies = new Dictionary<string, Assembly>();

		private Dictionary<Type, IObjectEvent> disposerEvents;

		private Queue<Disposer> updates = new Queue<Disposer>();
		private Queue<Disposer> updates2 = new Queue<Disposer>();

		private Queue<Disposer> lateUpdates = new Queue<Disposer>();
		private Queue<Disposer> lateUpdates2 = new Queue<Disposer>();

		private Queue<Disposer> loaders = new Queue<Disposer>();
		private Queue<Disposer> loaders2 = new Queue<Disposer>();
		
		public void Register(string name, Assembly assembly)
		{
			this.assemblies[name] = assembly;

			this.disposerEvents = new Dictionary<Type, IObjectEvent>();
			foreach (Assembly ass in this.assemblies.Values)
			{
				Type[] types = ass.GetTypes();
				foreach (Type type in types)
				{
					object[] attrs = type.GetCustomAttributes(typeof(ObjectEventAttribute), false);

					if (attrs.Length == 0)
					{
						continue;
					}

					object obj = Activator.CreateInstance(type);
					IObjectEvent objectEvent = obj as IObjectEvent;
					if (objectEvent == null)
					{
						Log.Error($"组件事件没有继承IObjectEvent: {type.Name}");
						continue;
					}
					this.disposerEvents[objectEvent.Type()] = objectEvent;
				}
			}

			this.Load();
		}

		public Assembly GetAssembly(string name)
		{
			return this.assemblies[name];
		}

		public Assembly[] GetAssemblies()
		{
			return this.assemblies.Values.ToArray();
		}

		public void Add(Disposer disposer)
		{
			if (!this.disposerEvents.TryGetValue(disposer.GetType(), out IObjectEvent objectEvent))
			{
				return;
			}

			if (objectEvent is ILoad)
			{
				this.loaders.Enqueue(disposer);
			}

			if (objectEvent is IUpdate)
			{
				this.updates.Enqueue(disposer);
			}

			if (objectEvent is ILateUpdate)
			{
				this.lateUpdates.Enqueue(disposer);
			}
		}

		public void Awake(Disposer disposer)
		{
			if (!this.disposerEvents.TryGetValue(disposer.GetType(), out IObjectEvent objectEvent))
			{
				return;
			}
			IAwake iAwake = objectEvent as IAwake;
			if (iAwake == null)
			{
				return;
			}
			objectEvent.Set(disposer);
			iAwake.Awake();
		}

		public void Awake<P1>(Disposer disposer, P1 p1)
		{
			if (!this.disposerEvents.TryGetValue(disposer.GetType(), out IObjectEvent objectEvent))
			{
				return;
			}
			IAwake<P1> iAwake = objectEvent as IAwake<P1>;
			if (iAwake == null)
			{
				return;
			}
			objectEvent.Set(disposer);
			iAwake.Awake(p1);
		}

		public void Awake<P1, P2>(Disposer disposer, P1 p1, P2 p2)
		{
			if (!this.disposerEvents.TryGetValue(disposer.GetType(), out IObjectEvent objectEvent))
			{
				return;
			}
			IAwake<P1, P2> iAwake = objectEvent as IAwake<P1, P2>;
			if (iAwake == null)
			{
				return;
			}
			objectEvent.Set(disposer);
			iAwake.Awake(p1, p2);
		}

		public void Awake<P1, P2, P3>(Disposer disposer, P1 p1, P2 p2, P3 p3)
		{
			if (!this.disposerEvents.TryGetValue(disposer.GetType(), out IObjectEvent objectEvent))
			{
				return;
			}
			IAwake<P1, P2, P3> iAwake = objectEvent as IAwake<P1, P2, P3>;
			if (iAwake == null)
			{
				return;
			}
			objectEvent.Set(disposer);
			iAwake.Awake(p1, p2, p3);
		}

		public void Load()
		{
			while (this.loaders.Count > 0)
			{
				Disposer disposer = this.loaders.Dequeue();
				if (disposer.Id == 0)
				{
					continue;
				}

				if (!this.disposerEvents.TryGetValue(disposer.GetType(), out IObjectEvent objectEvent))
				{
					continue;
				}

				this.loaders2.Enqueue(disposer);

				ILoad iLoad = objectEvent as ILoad;
				if (iLoad == null)
				{
					continue;
				}
				objectEvent.Set(disposer);
				try
				{
					iLoad.Load();
				}
				catch (Exception e)
				{
					Log.Error(e.ToString());
				}
			}

			ObjectHelper.Swap(ref this.loaders, ref this.loaders2);
		}

		public void Update()
		{
			while (this.updates.Count > 0)
			{
				Disposer disposer = this.updates.Dequeue();
				if (disposer.Id == 0)
				{
					continue;
				}
				if (!this.disposerEvents.TryGetValue(disposer.GetType(), out IObjectEvent objectEvent))
				{
					continue;
				}

				this.updates2.Enqueue(disposer);

				IUpdate iUpdate = objectEvent as IUpdate;
				if (iUpdate == null)
				{
					continue;
				}
				objectEvent.Set(disposer);
				try
				{
					iUpdate.Update();
				}
				catch (Exception e)
				{
					Log.Error(e.ToString());
				}
			}
			
			ObjectHelper.Swap(ref this.updates, ref this.updates2);
		}

		public void LateUpdate()
		{
			while (this.lateUpdates.Count > 0)
			{
				Disposer disposer = this.lateUpdates.Dequeue();
				if (disposer.Id == 0)
				{
					continue;
				}
				if (!this.disposerEvents.TryGetValue(disposer.GetType(), out IObjectEvent objectEvent))
				{
					continue;
				}

				this.lateUpdates2.Enqueue(disposer);

				ILateUpdate iLateUpdate = objectEvent as ILateUpdate;
				if (iLateUpdate == null)
				{
					continue;
				}
				objectEvent.Set(disposer);
				try
				{
					iLateUpdate.LateUpdate();
				}
				catch (Exception e)
				{
					Log.Error(e.ToString());
				}
			}

			ObjectHelper.Swap(ref this.lateUpdates, ref this.lateUpdates2);
		}
	}
}