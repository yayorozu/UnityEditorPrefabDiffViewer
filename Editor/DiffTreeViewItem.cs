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
		private List<PrefabComponent> _components;

		internal void SetUp(TargetFlag flag, List<PrefabComponent> components)
		{
			_components = components;
			switch (flag)
			{
				case TargetFlag.Add:
					icon = (Texture2D) Styles.AddContent.image;
					break;
				case TargetFlag.Sub:
					icon = (Texture2D) Styles.SubContent.image;
					break;
				case TargetFlag.None:
					// 追加削除が無い場合差分を見る
					var isModify =
						components.Any(c => c.Flag == TargetFlag.Add) |
						components.Any(c => c.Flag == TargetFlag.Sub) |
						components.Any(c => c.Diffs.Count > 0);

					icon = isModify
						? (Texture2D) Styles.ModifyContent.image
						: (Texture2D) Styles.EmptyContent.image;
					break;
			}
		}

		/// <summary>
		/// EditorWindow描画
		/// </summary>
		internal void Draw()
		{
			using (new EditorGUILayout.VerticalScope())
			{
				EditorGUILayout.LabelField(displayName, Styles.HeaderBold);
				GUILayout.Space(10);
				DrawComponentDiff("Add Components", TargetFlag.Add, DrawComponents);
				DrawComponentDiff("Sub Components", TargetFlag.Sub, DrawComponents);
				DrawComponentDiff("Modify Components", TargetFlag.Modify, components =>
				{
					foreach (var component in components)
					{
						DrawComponent(component);
						using (new EditorGUI.IndentLevelScope())
						{
							foreach (var diff in component.Diffs)
							{
								EditorGUILayout.LabelField($"{ObjectNames.NicifyVariableName(diff.Name)}");
								using (new EditorGUI.IndentLevelScope())
								{
									foreach (var value in diff.PrevValues)
									{
										EditorGUILayout.LabelField(value);
									}
									EditorGUILayout.LabelField("↓");
									foreach (var value in diff.CurrentValues)
									{
										EditorGUILayout.LabelField(value);
									}
								}
							}
						}
					}
				});
			}
		}

		private void DrawComponentDiff(string label, TargetFlag flag, Action<IEnumerable<PrefabComponent>> draw)
		{
			var finds = _components.Where(c => c.Flag == flag);
			if (!finds.Any())
				return;

			EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
			using (new EditorGUI.IndentLevelScope())
			{
				draw?.Invoke(finds);
			}
			GUILayout.Space(5);
		}

		private void DrawComponents(IEnumerable<PrefabComponent> components)
		{
			foreach (var c in components)
			{
				DrawComponent(c);
			}
		}

		private void DrawComponent(PrefabComponent component)
		{
			var content = EditorGUIUtility.ObjectContent(null, component.Type);
			content.text = component.Name;
			EditorGUILayout.LabelField(content);
		}
	}
}
