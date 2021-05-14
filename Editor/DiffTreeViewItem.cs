using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Yorozu.PrefabDiffViewer
{
	internal class DiffTreeViewItem : TreeViewItem
	{
		internal Texture2D SubIcon;

		private List<PrefabComponent> _components;
		private int _id;

		internal void SetUp(TargetFlag flag, List<PrefabComponent> components, bool isNestedPrefab)
		{
			_components = components;
			switch (flag)
			{
				case TargetFlag.Add:
					SubIcon = Styles.AddTexture;
					break;
				case TargetFlag.Sub:
					SubIcon = Styles.SubTexture;
					break;
				case TargetFlag.None:
					// 追加削除が無い場合差分を見る
					var isModify =
						components.Any(c => c.Flag == TargetFlag.Add) |
						components.Any(c => c.Flag == TargetFlag.Sub) |
						components.Any(c => c.Diffs.Count > 0);

					if (isModify)
						SubIcon = Styles.ModifyTexture;
					break;
			}

			icon = isNestedPrefab ? Styles.NestedPrefabTexture : Styles.PrefabTexture;
		}

		private void ResetId()
		{
			_id = 0;
		}

		private int GetId() => _id++;

		internal void SetChild(TreeViewItem root)
		{
			ResetId();
			GetDiffItem(root, "Add Components", TargetFlag.Add, Styles.AddTexture);
			GetDiffItem(root, "Sub Components", TargetFlag.Sub, Styles.SubTexture);
			GetDiffItem(root, "Modify Components", TargetFlag.Modify, Styles.ModifyTexture, SetModifyValue);
		}

		private void GetDiffItem(
			TreeViewItem root,
			string label,
			TargetFlag flag,
			Texture2D texture,
			Action<PrefabComponent, TreeViewItem> action = null
		)
		{
			var finds = _components.Where(c => c.Flag == flag);
			if (!finds.Any())
				return;

			var item = new TreeViewItem
			{
				id = GetId(),
				displayName = label,
				icon = texture,
			};

			foreach (var find in finds)
			{
				var content = EditorGUIUtility.ObjectContent(null, find.Type);
				var child = new TreeViewItem
				{
					id = GetId(),
					displayName = find.Name,
					icon = (Texture2D) content.image,
				};

				if (child.icon == null)
					child.icon = Styles.ScriptTexture;

				action?.Invoke(find, child);
				item.AddChild(child);
			}

			root.AddChild(item);
		}

		private void SetModifyValue(PrefabComponent component, TreeViewItem root)
		{
			foreach (var diff in component.Diffs)
			{
				var item = new TreeViewItem
				{
					id = GetId(),
					displayName = ObjectNames.NicifyVariableName(diff.Name),
					icon = Styles.InfoTexture,
				};

				foreach (var value in diff.PrevValues)
				{
					item.AddChild(new TreeViewItem {id = GetId(), displayName = value,});
				}

				item.AddChild(new TreeViewItem {id = GetId(), displayName = "↓",});
				foreach (var value in diff.CurrentValues)
				{
					item.AddChild(new TreeViewItem {id = GetId(), displayName = value,});
				}

				root.AddChild(item);
			}
		}
	}
}
