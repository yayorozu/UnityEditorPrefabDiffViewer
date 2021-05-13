using UnityEditor;
using UnityEngine;
using UnityEditor.IMGUI.Controls;

namespace Yorozu.PrefabDiffViewer
{
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

			var diffData = PrefabDiffUtil.GetDiff(_diffCheckPrefab);
			if (diffData != null)
			{
				_treeView.SetDiff(diffData);
			}
		}

		private void DoubleClickEvent(DiffTreeViewItem item)
		{
			_currentItem = item;
		}
	}
}


