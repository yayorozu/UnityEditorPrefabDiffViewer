using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Yorozu.PrefabDiffViewer
{
	internal static class PrefabDiffUtil
	{
		internal static PrefabDiff GetDiff(GameObject current, string currentPath, GameObject prev, string prevPath)
		{
			var diff = new PrefabDiff();
			var info = Recursive(current.transform, TargetFlag.Add);
			// 混ぜる
			AddRecursive(prev.transform, ref info);

			var currentYaml = YamlParse(currentPath);
			var prevYaml = YamlParse(prevPath);
			CheckFieldDiff(info, currentYaml, prevYaml);

			diff.Root = info;
			//diff.Display();
			return diff;
		}

		/// <summary>
		/// Create Prefab Info
		/// </summary>
		private static PrefabObject Recursive(Transform transform, TargetFlag flag)
		{
			var info = new PrefabObject
			{
				Name = transform.name,
				Flag = flag,
			};
			{
				if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(transform.gameObject, out string guid, out long id))
				{
					info.ID = id;
				}
			}

			var components = transform.GetComponents<Component>();
			foreach (var component in components)
			{
				if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(component, out string guid, out long id))
					continue;

				var c = new PrefabComponent
				{
					Type = component.GetType(),
					ID = id,
					Flag = flag,
					GUID = guid,
				};
				info.Components.Add(c);
			}

			var count = transform.childCount;
			for (var i = 0; i < count; i++)
			{
				info.Child.Add(Recursive(transform.GetChild(i), flag));
			}

			return info;
		}

		/// <summary>
		/// 追記
		/// </summary>
		private static void AddRecursive(Transform transform, ref PrefabObject info)
		{
			info.Flag = TargetFlag.None;
			var components = transform.GetComponents<Component>();
			foreach (var component in components)
			{
				if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(component, out string guid, out long id))
					continue;

				var index = info.Components.FindIndex(c => c.ID == id);
				if (index >= 0)
				{
					info.Components[index].Flag = TargetFlag.None;
					continue;
				}

				var c = new PrefabComponent
				{
					Type = component.GetType(),
					ID = id,
					Flag = TargetFlag.Sub,
					GUID = guid,
				};
				info.Components.Add(c);
			}

			var count = transform.childCount;
			for (var i = 0; i < count; i++)
			{
				var child = transform.GetChild(i);
				if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(transform.gameObject, out string guid, out long id))
					continue;

				var index = info.Child.FindIndex(c => c.ID == id);
				if (index >= 0)
				{
					var f = info.Child[index];
					AddRecursive(child, ref f);
				}
				else
				{
					// 前にしかない
					info.Child.Add(Recursive(transform.GetChild(i), TargetFlag.Sub));
				}
			}
		}

		/// <summary>
		/// 適当パーサー
		/// </summary>
		private static Yaml YamlParse(string path)
		{
			var text = File.ReadAllLines(path);
			Debug.Log(string.Join("\n", text));
			var yaml = new Yaml();
			YamlComponent c = null;
			YamlField f = null;
			var isArray = false;
			for (var i = 2; i < text.Length; i++)
			{
				// New Component
				if (text[i].StartsWith("---"))
				{
					if (c != null)
						yaml.Components.Add(c);

					var index = text[i].IndexOf("&", StringComparison.Ordinal) + 1;
					c = new YamlComponent(text[i].Substring(index));
					continue;
				}

				// ComponentName
				if (!text[i].StartsWith(" "))
				{
					c.Component = text[i].Substring(0, text[i].Length - 1);
					continue;
				}

				var trim = text[i].Trim();
				// インデントが変わっていたら Array の内部判定
				if (isArray && (text[i].StartsWith("  - ") || text[i].StartsWith("    ")))
				{
					f.Values.Add(text[i].Substring(4));
					continue;
				}

				isArray = false;

				if (f != null)
					c.Fields.Add(f);

				// List or Array  stringで空の場合もある・・・
				if (trim.EndsWith(":"))
				{
					f = new YamlField(trim.Substring(0, trim.Length - 1));
					isArray = true;
					continue;
				}

				var spIndex = trim.IndexOf(":", StringComparison.Ordinal);
				f = new YamlField(trim.Substring(0, spIndex));
				f.Values.Add(trim.Substring(spIndex + 2));
			}

			if (f != null)
				c.Fields.Add(f);

			if (c != null)
				yaml.Components.Add(c);

			return yaml;
		}

		private static void CheckFieldDiff(PrefabObject info, Yaml current, Yaml prev)
		{
			if (info.Flag != TargetFlag.None)
				return;

			foreach (var component in info.Components)
			{
				if (component.Flag != TargetFlag.None)
					continue;

				var c = current.Components.First(c => c.ID == component.ID.ToString());
				var p = prev.Components.First(c => c.ID == component.ID.ToString());
				component.AddDiffField(c.Fields, p.Fields);
				if (component.Diffs.Count > 0)
					component.Flag |= TargetFlag.Modify;
			}

			foreach (var c in info.Child)
			{
				CheckFieldDiff(c, current, prev);
			}
		}
	}

	internal enum TargetFlag
	{
		None = 0,
		Add = 1,
		Sub = 2,
		Modify = 4,
	}

	internal class PrefabDiff
	{
		internal PrefabObject Root = new PrefabObject();

		internal void Display()
		{
			Root.Display();
		}

		internal DiffTreeViewItem Convert()
		{
			return Root.Convert();
		}
	}

	internal class PrefabObject
	{
		internal TargetFlag Flag;
		internal string Name;
		internal long ID;
		internal List<PrefabObject> Child = new List<PrefabObject>();
		internal List<PrefabComponent> Components = new List<PrefabComponent>();

		internal void Display()
		{
			Debug.Log($"----{Name} {Flag}");
			Debug.Log($"--------Components");
			foreach (var component in Components)
			{
				Debug.Log($"-------------{component.Name} {component.Flag} {component.Diffs.Count}");
				foreach (var diff in component.Diffs)
				{
					Debug.Log($"---------diff {diff.Name}");
				}

			}
			foreach (var c in Child)
			{
				c.Display();
			}
		}

		internal DiffTreeViewItem Convert()
		{
			var root = new DiffTreeViewItem()
			{
				id = (int) ID,
				displayName = Name,
			};
			root.SetUp(Flag, Components);

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
		internal string GUID;
		internal string Name => Type.Name;
		internal List<PrefabField> Diffs = new List<PrefabField>();
		internal Type Type;

		/// <summary>
		/// 差分のあるフィールドを検索して追加
		/// </summary>
		internal void AddDiffField(List<YamlField> current, List<YamlField> prev)
		{
			foreach (var field in current)
			{
				var index = prev.FindIndex(f => f.Name == field.Name);
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

	internal class Yaml
	{
		internal List<YamlComponent> Components = new List<YamlComponent>();
	}

	internal class YamlComponent
	{
		internal YamlComponent(string id)
		{
			ID = id;
		}

		internal string ID;
		internal string Component;
		internal List<YamlField> Fields = new List<YamlField>();
	}

	internal class YamlField
	{
		internal YamlField(string name)
		{
			Name = name;
		}

		internal string Name;
		internal List<string> Values = new List<string>();
	}
}
