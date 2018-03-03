﻿using UnityEngine;
using UnityEngine.UI;

namespace Model
{
	[ObjectSystem]
	public class UiLoadingComponentAwakeSystem : AwakeSystem<UILoadingComponent>
	{
		public override void Awake(UILoadingComponent self)
		{
			self.text = self.GetParent<UI>().GameObject.Get<GameObject>("Text").GetComponent<Text>();
		}
	}

	[ObjectSystem]
	public class UiLoadingComponentStartSystem : StartSystem<UILoadingComponent>
	{
		public override async void Start(UILoadingComponent self)
		{
			TimerComponent timerComponent = Game.Scene.GetComponent<TimerComponent>();

			while (true)
			{
				await timerComponent.WaitAsync(1000);

				if (self.IsDisposed)
				{
					return;
				}

				BundleDownloaderComponent bundleDownloaderComponent = Game.Scene.GetComponent<BundleDownloaderComponent>();
				if (bundleDownloaderComponent == null)
				{
					continue;
				}
				self.text.text = $"{bundleDownloaderComponent.Progress}%";
			}
		}
	}

	public class UILoadingComponent : Component
	{
		public Text text;
	}
}
