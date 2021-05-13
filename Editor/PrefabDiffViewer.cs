using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityEditor.Experimental;
using UnityEditor.IMGUI.Controls;

namespace Yorozu.PrefabDiffViewer
{
	internal static class Styles
	{
		internal static GUIContent AddContent;
		internal static GUIContent SubContent;
		internal static GUIContent ModifyContent;
		internal static GUIContent EmptyContent;

		internal static GUIStyle HeaderBold;

		static Styles()
		{
			AddContent = new GUIContent(EditorResources.Load<Texture2D>("CollabCreate Icon"));
			SubContent = new GUIContent(EditorResources.Load<Texture2D>("CollabDeleted Icon"));
			ModifyContent = new GUIContent(EditorResources.Load<Texture2D>("CollabChanges Icon"));
			EmptyContent = new GUIContent(EditorResources.Load<Texture2D>("d_tranp"));

			HeaderBold = new GUIStyle(EditorStyles.boldLabel);
			HeaderBold.fontSize += 10;
		}
	}

	internal class PrefabDiffViewer : EditorWindow
	{
		[MenuItem("Tools/PrefabDiff")]
		private static void ShowWindow()
		{
			var window = GetWindow<PrefabDiffViewer>("PrefabDiff");
			window.Show();
		}

		private GameObject _diffCheckPrefab;

		[SerializeField]
		private TreeViewState _state;
		private DiffTreeView _treeView;
		private DiffTreeViewItem _currentItem;

		private void Init()
		{
			_state ??= new TreeViewState();
			if (_treeView == null)
			{
				_treeView = new DiffTreeView(_state);
				_treeView.DoubleClickEvent += DoubleClickEvent;
			}
		}

		private void OnGUI()
		{
			Init();

			_diffCheckPrefab =
				(GameObject) EditorGUILayout.ObjectField("Check Prefab", _diffCheckPrefab, typeof(GameObject), false);
			if (GUILayout.Button("Check"))
			{
				CheckDiff();
			}

			using (new EditorGUILayout.HorizontalScope())
			{
				var width = _currentItem != null ? position.width / 2f : position.width;
				var rect = GUILayoutUtility.GetRect(
					GUIContent.none,
					GUIStyle.none,
					GUILayout.ExpandHeight(true),
					GUILayout.Width(width)
				);
				_treeView.OnGUI(rect);

				DrawGameObject();
			}
		}

		private void DrawGameObject()
		{
			if (_currentItem == null)
				return;

			_currentItem.Draw();
		}

		private void CheckDiff()
		{
			if (_diffCheckPrefab == null)
				return;

			var path = AssetDatabase.GetAssetPath(_diffCheckPrefab);
			var diff = Command.Exec($"git diff \'{path}\'");

			// 差分無し
			if (string.IsNullOrEmpty(diff))
			{
				Debug.Log($"{path} is not edit");
				return;
			}

			var fileName = Path.GetFileNameWithoutExtension(path);
			var extension = Path.GetExtension(path);
			var dirName = Path.GetDirectoryName(path);
			var tempPath = Path.Combine(dirName, fileName + "_temp" + extension);

			// 修正前のデータを取得
			var yaml = Command.Exec($"git show \'HEAD:{path}\'");

			if (string.IsNullOrEmpty(yaml))
				return;

			File.WriteAllText(tempPath, yaml);
			AssetDatabase.Refresh();

			var tempPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(tempPath);
			if (tempPrefab != null)
			{
				var diffData = PrefabDiffUtil.GetDiff(_diffCheckPrefab, path, tempPrefab, tempPath);
				_treeView.SetDiff(diffData);
			}

			AssetDatabase.DeleteAsset(tempPath);
			AssetDatabase.Refresh();
		}

		private void DoubleClickEvent(DiffTreeViewItem item)
		{
			_currentItem = item;
		}
	}

	internal class DiffTreeView : TreeView
	{
		internal DiffTreeView(TreeViewState state) : base(state)
		{
			showBorder = true;
			showAlternatingRowBackgrounds = true;
			Reload();
		}

		public DiffTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader) : base(state, multiColumnHeader) { }

		private PrefabDiff _diff;
		public event Action<DiffTreeViewItem> DoubleClickEvent;

		internal void SetDiff(PrefabDiff diff)
		{
			_diff = diff;
			Reload();
		}

		protected override TreeViewItem BuildRoot()
		{
			var root = new TreeViewItem(-1, -1);
			if (_diff != null)
			{
				var child = _diff.Convert();
				root.AddChild(child);
				SetupDepthsFromParentsAndChildren(root);
			}
			else
			{
				SetupParentsAndChildrenFromDepths(root, new List<TreeViewItem>());
			}
			return root;
		}

		private TreeViewItem Find(int id) => GetRows().First(i => i.id == id);

		protected override void DoubleClickedItem(int id)
		{
			var item = Find(id);
			var diffItem = item as DiffTreeViewItem;
			DoubleClickEvent?.Invoke(diffItem);
		}

		protected override bool CanChangeExpandedState(TreeViewItem item)
		{
			return false;
		}
	}

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


