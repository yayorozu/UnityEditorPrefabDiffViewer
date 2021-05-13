using System;
using System.Collections.Generic;
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

		[SerializeField]
		private TreeViewState _state;
		private DiffTreeView _treeView;
		private DiffTreeViewItem _currentItem;
		private string[] _diffPrefabPaths;
		private int _prefabIndex;

		private void OnEnable()
		{
			FindDiffPrefabs();
		}

		private void Init()
		{
			if (_state == null)
				_state = new TreeViewState();

			if (_treeView == null)
			{
				_treeView = new DiffTreeView(_state);
				_treeView.DoubleClickEvent += DoubleClickEvent;
			}
		}

		private void FindDiffPrefabs()
		{
			var log = Command.Exec("git diff --name-only");
			var split =log.Split('\n');
			var list = new List<string>{""};
			foreach (var path in split)
			{
				if (!path.Contains("Assets/") || !path.EndsWith(".prefab"))
					continue;

				list.Add(path);
			}

			_diffPrefabPaths = list.ToArray();
			if (_prefabIndex > _diffPrefabPaths.Length)
				_prefabIndex = 0;
		}

		private void OnGUI()
		{
			Init();

			using (new EditorGUILayout.HorizontalScope())
			{
				_prefabIndex = EditorGUILayout.Popup("Check Prefab", _prefabIndex, _diffPrefabPaths);
				if (GUILayout.Button("Reload", GUILayout.Width(100)))
				{
					FindDiffPrefabs();
				}
			}

			if (GUILayout.Button("Check Diff"))
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
			if (_prefabIndex > _diffPrefabPaths.Length)
				return;

			if (string.IsNullOrEmpty(_diffPrefabPaths[_prefabIndex]))
				return;

			var diffData = PrefabDiffUtil.GetDiff(_diffPrefabPaths[_prefabIndex]);
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


