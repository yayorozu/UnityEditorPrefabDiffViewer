using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Yorozu.PrefabDiffViewer
{
	internal static class PrefabDiffUtil
	{
		/// <summary>
		/// Prefab の diff データを作成
		/// </summary>
		internal static PrefabDiff GetDiff(string path)
		{
			// Unity内のパスへ変換
			var assetPath = path;
			if (!assetPath.StartsWith("Assets/"))
			{
				var index = path.IndexOf("Assets/");
				assetPath = path.Substring(index);
			}

			var diff = Command.Exec($"git diff \'{assetPath}\'");
			// 差分無し
			if (string.IsNullOrEmpty(diff))
			{
				Debug.Log($"{assetPath} is not Update");
				return null;
			}

			var target = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
			if (target == null)
			{
				Debug.Log($"{assetPath} is illegal");
				return null;
			}

			var fileName = Path.GetFileNameWithoutExtension(assetPath);
			var extension = Path.GetExtension(assetPath);
			var dirName = Path.GetDirectoryName(assetPath);
			var tempPath = Path.Combine(dirName, fileName + "_temp" + extension);

			// 修正前のデータを取得
			// ここはunity からのパスじゃなくてフルパスが必要
			var yaml = Command.Exec($"git show \'HEAD:{path}\'");
			if (string.IsNullOrEmpty(yaml))
			{
				// 一応両方で確認
				yaml = Command.Exec($"git show \'HEAD:{assetPath}\'");
				if (string.IsNullOrEmpty(yaml))
				{
					Debug.Log($"{assetPath} prev yaml is empty");
					return null;
				}
			}

			File.WriteAllText(tempPath, yaml);
			AssetDatabase.Refresh();

			PrefabDiff diffData = null;
			var tempPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(tempPath);
			if (tempPrefab != null)
			{
				var info = Recursive(target.transform, TargetFlag.Add);
				// 混ぜる
				AddRecursive(tempPrefab.transform, info);

				var currentYaml = PrefabYamlUtil.Parse(assetPath);
				var prevYaml = PrefabYamlUtil.Parse(tempPath);
				CheckFieldDiff(info, currentYaml, prevYaml);

				diffData = new PrefabDiff(info);
			}

			AssetDatabase.DeleteAsset(tempPath);
			AssetDatabase.Refresh();

			return diffData;
		}

		/// <summary>
		/// Create Prefab Data
		/// </summary>
		private static PrefabObject Recursive(Transform transform, TargetFlag flag)
		{
			var info = new PrefabObject(transform.name, flag);
			{
				if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(transform.gameObject, out string guid, out long id))
				{
					info.ID = id;
				}
			}

			var nestedRoot = PrefabUtility.GetNearestPrefabInstanceRoot(transform);
			info.IsNestedPrefab = nestedRoot != null;

			var components = transform.GetComponents<Component>();
			foreach (var component in components)
			{
				if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(component, out string guid, out long id))
					continue;

				var c = new PrefabComponent(component.GetType(), id, flag);
				info.Components.Add(c);
			}

			var count = transform.childCount;
			for (var i = 0; i < count; i++)
			{
				var child = transform.GetChild(i);
				info.Child.Add(Recursive(child, flag));
			}

			return info;
		}

		/// <summary>
		/// 差分確認
		/// </summary>
		private static void AddRecursive(Transform transform, PrefabObject info)
		{
			info.Flag = TargetFlag.None;
			var components = transform.GetComponents<Component>();
			foreach (var component in components)
			{
				if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(component, out var guid, out long id))
					continue;

				var index = info.Components.FindIndex(cc => cc.ID == id);
				if (index >= 0)
				{
					info.Components[index].Flag = TargetFlag.None;
					continue;
				}

				var c = new PrefabComponent(component.GetType(), id, TargetFlag.Sub);
				info.Components.Add(c);
			}

			var count = transform.childCount;
			for (var i = 0; i < count; i++)
			{
				var child = transform.GetChild(i);
				if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(child.gameObject, out var guid, out long id))
					continue;

				var index = info.Child.FindIndex(c => c.ID == id);
				if (index >= 0)
				{
					AddRecursive(child, info.Child[index]);
				}
				else
				{
					// 前にしかない
					info.Child.Add(Recursive(child, TargetFlag.Sub));
				}
			}
		}

		/// <summary>
		/// Yaml データを元に 値の変化を確認
		/// </summary>
		private static void CheckFieldDiff(PrefabObject info, Yaml current, Yaml prev)
		{
			if (info.Flag != TargetFlag.None)
				return;

			foreach (var component in info.Components)
			{
				if (component.Flag != TargetFlag.None)
					continue;

				var c = current.Components.FirstOrDefault(cc => cc.ID == component.ID);
				var p = prev.Components.FirstOrDefault(cc => cc.ID == component.ID);
				// NestedPrefab は yaml にない
				if (c == null || p == null)
				{
					continue;
				}

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
}
