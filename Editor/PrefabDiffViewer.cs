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
		[SerializeField]
		private TreeViewState _state2;

		private DiffTreeView _treeView;
		private DetailTreeView _treeView2;

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
			if (_state2 == null)
				_state2 = new TreeViewState();

			if (_treeView == null)
			{
				_treeView = new DiffTreeView(_state);
				_treeView.DetailEvent += DetailEvent;
			}

			if (_treeView2 == null)
			{
				_treeView2 = new DetailTreeView(_state2);
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
				var rect = GUILayoutUtility.GetRect(
					GUIContent.none,
					GUIStyle.none,
					GUILayout.ExpandHeight(true),
					GUILayout.ExpandWidth(true)
				);

				rect.width /= 2f;
				_treeView.OnGUI(rect);
				rect.x += rect.width;
				_treeView2.OnGUI(rect);
			}
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

		private void DetailEvent(DiffTreeViewItem item)
		{
			_treeView2?.SetItem(item);
		}
	}
}


