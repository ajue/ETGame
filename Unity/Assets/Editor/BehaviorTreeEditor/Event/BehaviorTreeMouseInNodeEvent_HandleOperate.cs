﻿using Model;

namespace MyEditor
{
	[Event(EventIdType.BehaviorTreeMouseInNode)]
	public class BehaviorTreeMouseInNodeEvent_HandleOperate: AEvent<BehaviorNodeData, NodeDesigner>
	{
		public override void Run(BehaviorNodeData nodeData, NodeDesigner nodeDesigner)
		{
			BTEditorWindow.Instance.onMouseInNode(nodeData, nodeDesigner);
		}
	}
}