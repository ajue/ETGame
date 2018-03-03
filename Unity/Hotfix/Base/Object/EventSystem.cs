﻿using System;
using System.Collections.Generic;
using Model;

namespace Hotfix
{
	public interface IObjectSystem
	{
		Type Type();
		void Set(object value);
	}

	public abstract class ObjectSystem<T> : IObjectSystem
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

	public sealed class EventSystem
	{
		private readonly Dictionary<int, List<IEvent>> allEvents = new Dictionary<int, List<IEvent>>();

		private readonly UnOrderMultiMap<Type, AAwakeSystem> awakeEvents = new UnOrderMultiMap<Type, AAwakeSystem>();

		private readonly UnOrderMultiMap<Type, AStartSystem> startEvents = new UnOrderMultiMap<Type, AStartSystem>();

		private readonly UnOrderMultiMap<Type, ALoadSystem> loadEvents = new UnOrderMultiMap<Type, ALoadSystem>();

		private readonly UnOrderMultiMap<Type, AUpdateSystem> updateEvents = new UnOrderMultiMap<Type, AUpdateSystem>();

		private readonly UnOrderMultiMap<Type, ALateUpdateSystem> lateUpdateEvents = new UnOrderMultiMap<Type, ALateUpdateSystem>();

		private Queue<Component> updates = new Queue<Component>();
		private Queue<Component> updates2 = new Queue<Component>();

		private readonly Queue<Component> starts = new Queue<Component>();

		private Queue<Component> loaders = new Queue<Component>();
		private Queue<Component> loaders2 = new Queue<Component>();

		private Queue<Component> lateUpdates = new Queue<Component>();
		private Queue<Component> lateUpdates2 = new Queue<Component>();

		private readonly HashSet<Component> unique = new HashSet<Component>();

		public EventSystem()
		{
			Type[] types = Game.Hotfix.GetHotfixTypes();
			foreach (Type type in types)
			{
				object[] attrs = type.GetCustomAttributes(typeof(ObjectSystemAttribute), false);

				if (attrs.Length == 0)
				{
					continue;
				}

				object obj = Activator.CreateInstance(type);

				AAwakeSystem objectSystem = obj as AAwakeSystem;
				if (objectSystem != null)
				{
					this.awakeEvents.Add(objectSystem.Type(), objectSystem);
				}

				AUpdateSystem aUpdateSystem = obj as AUpdateSystem;
				if (aUpdateSystem != null)
				{
					this.updateEvents.Add(aUpdateSystem.Type(), aUpdateSystem);
				}

				ALateUpdateSystem aLateUpdateSystem = obj as ALateUpdateSystem;
				if (aLateUpdateSystem != null)
				{
					this.lateUpdateEvents.Add(aLateUpdateSystem.Type(), aLateUpdateSystem);
				}

				AStartSystem aStartSystem = obj as AStartSystem;
				if (aStartSystem != null)
				{
					this.startEvents.Add(aStartSystem.Type(), aStartSystem);
				}

				ALoadSystem aLoadSystem = obj as ALoadSystem;
				if (aLoadSystem != null)
				{
					this.loadEvents.Add(aLoadSystem.Type(), aLoadSystem);
				}
			}

			this.allEvents.Clear();
			foreach (Type type in types)
			{
				object[] attrs = type.GetCustomAttributes(typeof(EventAttribute), false);

				foreach (object attr in attrs)
				{
					EventAttribute aEventAttribute = (EventAttribute)attr;
					object obj = Activator.CreateInstance(type);
					IEvent iEvent = obj as IEvent;
					if (iEvent == null)
					{
						Log.Error($"{obj.GetType().Name} 没有继承IEvent");
					}
					this.RegisterEvent(aEventAttribute.Type, iEvent);

					// hotfix的事件也要注册到mono层，hotfix可以订阅mono层的事件
					Action<List<object>> action = list => { Handle(aEventAttribute.Type, list); };
					Game.EventSystem.RegisterEvent(aEventAttribute.Type, new EventProxy(action));
				}
			}

			this.Load();
		}

		public static void Handle(int type, List<object> param)
		{
			switch (param.Count)
			{
				case 0:
					Hotfix.EventSystem.Run(type);
					break;
				case 1:
					Hotfix.EventSystem.Run(type, param[0]);
					break;
				case 2:
					Hotfix.EventSystem.Run(type, param[0], param[1]);
					break;
				case 3:
					Hotfix.EventSystem.Run(type, param[0], param[1], param[2]);
					break;
			}
		}
		
		public void RegisterEvent(int eventId, IEvent e)
		{
			if (!this.allEvents.ContainsKey(eventId))
			{
				this.allEvents.Add(eventId, new List<IEvent>());
			}
			this.allEvents[eventId].Add(e);
		}

		public void Add(Component disposer)
		{
			Type type = disposer.GetType();

			if (this.loadEvents.ContainsKey(type))
			{
				this.loaders.Enqueue(disposer);
			}

			if (this.updateEvents.ContainsKey(type))
			{
				this.updates.Enqueue(disposer);
			}

			if (this.startEvents.ContainsKey(type))
			{
				this.starts.Enqueue(disposer);
			}

			if (this.lateUpdateEvents.ContainsKey(type))
			{
				this.lateUpdates.Enqueue(disposer);
			}
		}

		public void Awake(Component disposer)
		{
			this.Add(disposer);

			List<AAwakeSystem> iAwakeSystems = this.awakeEvents[disposer.GetType()];
			if (iAwakeSystems == null)
			{
				return;
			}

			foreach (AAwakeSystem aAwakeSystem in iAwakeSystems)
			{
				if (aAwakeSystem == null)
				{
					continue;
				}
				
				IAwake iAwake = aAwakeSystem as IAwake;
				if (iAwake == null)
				{
					continue;
				}
				iAwake.Run(disposer);
			}
		}

		public void Awake<P1>(Component disposer, P1 p1)
		{
			this.Add(disposer);

			List<AAwakeSystem> iAwakeSystems = this.awakeEvents[disposer.GetType()];
			if (iAwakeSystems == null)
			{
				return;
			}
			
			foreach (AAwakeSystem aAwakeSystem in iAwakeSystems)
			{
				if (aAwakeSystem == null)
				{
					continue;
				}
				
				IAwake<P1> iAwake = aAwakeSystem as IAwake<P1>;
				if (iAwake == null)
				{
					continue;
				}
				iAwake.Run(disposer, p1);
			}
		}

		public void Awake<P1, P2>(Component disposer, P1 p1, P2 p2)
		{
			this.Add(disposer);

			List<AAwakeSystem> iAwakeSystems = this.awakeEvents[disposer.GetType()];
			if (iAwakeSystems == null)
			{
				return;
			}

			foreach (AAwakeSystem aAwakeSystem in iAwakeSystems)
			{
				if (aAwakeSystem == null)
				{
					continue;
				}
				
				IAwake<P1, P2> iAwake = aAwakeSystem as IAwake<P1, P2>;
				if (iAwake == null)
				{
					continue;
				}
				iAwake.Run(disposer, p1, p2);
			}
		}

		public void Awake<P1, P2, P3>(Component disposer, P1 p1, P2 p2, P3 p3)
		{
			this.Add(disposer);

			List<AAwakeSystem> iAwakeSystems = this.awakeEvents[disposer.GetType()];
			if (iAwakeSystems == null)
			{
				return;
			}

			foreach (AAwakeSystem aAwakeSystem in iAwakeSystems)
			{
				if (aAwakeSystem == null)
				{
					continue;
				}
				
				IAwake<P1, P2, P3> iAwake = aAwakeSystem as IAwake<P1, P2, P3>;
				if (iAwake == null)
				{
					continue;
				}
				iAwake.Run(disposer, p1, p2, p3);
			}
		}

		public void Load()
		{
			unique.Clear();
			while (this.loaders.Count > 0)
			{
				Component disposer = this.loaders.Dequeue();
				if (disposer.IsDisposed)
				{
					continue;
				}

				if (!this.unique.Add(disposer))
				{
					continue;
				}

				List<ALoadSystem> aLoadSystems = this.loadEvents[disposer.GetType()];
				if (aLoadSystems == null)
				{
					continue;
				}

				this.loaders2.Enqueue(disposer);

				foreach (ALoadSystem aLoadSystem in aLoadSystems)
				{
					try
					{
						aLoadSystem.Run(disposer);
					}
					catch (Exception e)
					{
						Log.Error(e.ToString());
					}
				}
			}

			ObjectHelper.Swap(ref this.loaders, ref this.loaders2);
		}

		private void Start()
		{
			unique.Clear();
			while (this.starts.Count > 0)
			{
				Component disposer = this.starts.Dequeue();

				if (!this.unique.Add(disposer))
				{
					continue;
				}

				List<AStartSystem> aStartSystems = this.startEvents[disposer.GetType()];
				if (aStartSystems == null)
				{
					continue;
				}

				foreach (AStartSystem aStartSystem in aStartSystems)
				{
					try
					{
						aStartSystem.Run(disposer);
					}
					catch (Exception e)
					{
						Log.Error(e.ToString());
					}
				}
			}
		}

		public void Update()
		{
			this.Start();

			this.unique.Clear();
			while (this.updates.Count > 0)
			{
				Component disposer = this.updates.Dequeue();
				if (disposer.IsDisposed)
				{
					continue;
				}

				if (!this.unique.Add(disposer))
				{
					continue;
				}

				List<AUpdateSystem> aUpdateSystems = this.updateEvents[disposer.GetType()];
				if (aUpdateSystems == null)
				{
					continue;
				}

				this.updates2.Enqueue(disposer);

				foreach (AUpdateSystem aUpdateSystem in aUpdateSystems)
				{
					try
					{
						aUpdateSystem.Run(disposer);
					}
					catch (Exception e)
					{
						Log.Error(e.ToString());
					}
				}
			}

			ObjectHelper.Swap(ref this.updates, ref this.updates2);
		}

		public void LateUpdate()
		{
			this.unique.Clear();
			while (this.lateUpdates.Count > 0)
			{
				Component disposer = this.lateUpdates.Dequeue();
				if (disposer.IsDisposed)
				{
					continue;
				}

				if (!this.unique.Add(disposer))
				{
					continue;
				}

				List<ALateUpdateSystem> aLateUpdateSystems = this.lateUpdateEvents[disposer.GetType()];
				if (aLateUpdateSystems == null)
				{
					continue;
				}

				this.lateUpdates2.Enqueue(disposer);

				foreach (ALateUpdateSystem aLateUpdateSystem in aLateUpdateSystems)
				{
					try
					{
						aLateUpdateSystem.Run(disposer);
					}
					catch (Exception e)
					{
						Log.Error(e.ToString());
					}
				}
			}

			ObjectHelper.Swap(ref this.lateUpdates, ref this.lateUpdates2);
		}

		public void Run(int type)
		{
			List<IEvent> iEvents;
			if (!this.allEvents.TryGetValue((int)type, out iEvents))
			{
				return;
			}
			foreach (IEvent iEvent in iEvents)
			{
				try
				{
					iEvent?.Handle();
				}
				catch (Exception e)
				{
					Log.Error(e.ToString());
				}
			}
		}

		public void Run<A>(int type, A a)
		{
			List<IEvent> iEvents;
			if (!this.allEvents.TryGetValue((int)type, out iEvents))
			{
				return;
			}
			foreach (IEvent iEvent in iEvents)
			{
				try
				{
					iEvent?.Handle(a);
				}
				catch (Exception e)
				{
					Log.Error(e.ToString());
				}
			}
		}

		public void Run<A, B>(int type, A a, B b)
		{
			List<IEvent> iEvents;
			if (!this.allEvents.TryGetValue((int)type, out iEvents))
			{
				return;
			}
			foreach (IEvent iEvent in iEvents)
			{
				try
				{
					iEvent?.Handle(a, b);
				}
				catch (Exception e)
				{
					Log.Error(e.ToString());
				}
			}
		}

		public void Run<A, B, C>(int type, A a, B b, C c)
		{
			List<IEvent> iEvents;
			if (!this.allEvents.TryGetValue((int)type, out iEvents))
			{
				return;
			}
			foreach (IEvent iEvent in iEvents)
			{
				try
				{
					iEvent?.Handle(a, b, c);
				}
				catch (Exception e)
				{
					Log.Error(e.ToString());
				}
			}
		}
	}
}