using System;
using System.Collections.Generic;

namespace Yorozu.PrefabDiffViewer
{

	internal enum TargetFlag
	{
		None = 0,
		Add = 1,
		Sub = 2,
		Modify = 4,
	}

	internal class PrefabDiff
	{
		private readonly PrefabObject _root;

		public PrefabDiff(PrefabObject info)
		{
			_root = info;
		}

		internal DiffTreeViewItem Convert()
		{
			return _root.Convert();
		}
	}

	internal class PrefabObject
	{
		internal TargetFlag Flag;
		internal string Name;
		internal long ID;
		internal bool IsNestedPrefab;
		internal List<PrefabObject> Child = new List<PrefabObject>();
		internal List<PrefabComponent> Components = new List<PrefabComponent>();

		public PrefabObject(string name, TargetFlag flag)
		{
			Name = name;
			Flag = flag;
		}

		internal DiffTreeViewItem Convert()
		{
			var root = new DiffTreeViewItem
			{
				id = (int) ID,
				displayName = Name,
			};
			root.SetUp(Flag, Components, IsNestedPrefab);

			foreach (var c in Child)
			{
				var child = c.Convert();
				root.AddChild(child);
			}

			return root;
		}
	}

	internal class PrefabComponent
	{
		internal TargetFlag Flag;
		internal long ID;
		internal string Name => Type.Name;
		internal List<PrefabField> Diffs = new List<PrefabField>();
		internal Type Type;

		public PrefabComponent(Type type, long id, TargetFlag flag)
		{
			Type = type;
			ID = id;
			Flag = flag;
		}

		/// <summary>
		/// 差分のあるフィールドを検索して追加
		/// </summary>
		internal void AddDiffField(List<YamlField> current, List<YamlField> prev)
		{
			foreach (var field in current)
			{
				var index = prev.FindIndex(pf => pf.Name == field.Name);
				var f = new PrefabField(field.Name);
				if (index < 0)
				{
					f.CurrentValues = field.Values;
				}
				else
				{
					var pf = prev[index];
					// あった場合は値の確認
					if (field.Values.Count != pf.Values.Count)
					{
						f.CurrentValues = field.Values;
						f.PrevValues = pf.Values;
					}
					else
					{
						for (var i = 0; i < field.Values.Count; i++)
						{
							if (field.Values[i] != pf.Values[i])
							{
								f.CurrentValues.Add(field.Values[i]);
								f.PrevValues.Add(pf.Values[i]);
							}
						}
					}
				}
				if (f.CurrentValues.Count > 0 || f.PrevValues.Count > 0)
					Diffs.Add(f);
			}
		}
	}

	internal class PrefabField
	{
		internal PrefabField(string name)
		{
			Name = name;
		}

		internal string Name;
		internal List<string> CurrentValues = new List<string>();
		internal List<string> PrevValues = new List<string>();
	}
}
