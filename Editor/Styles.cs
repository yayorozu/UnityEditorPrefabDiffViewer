using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;

namespace Yorozu.PrefabDiffViewer
{
	internal static class Styles
	{
		internal static GUIContent AddContent;
		internal static GUIContent SubContent;
		internal static GUIContent ModifyContent;

		internal static Texture2D PrefabTexture;
		internal static Texture2D NestedPrefabTexture;

		internal static GUIStyle HeaderBold;

		static Styles()
		{
			AddContent = new GUIContent(EditorResources.Load<Texture2D>("CollabCreate Icon"));
			SubContent = new GUIContent(EditorResources.Load<Texture2D>("CollabDeleted Icon"));
			ModifyContent = new GUIContent(EditorResources.Load<Texture2D>("CollabChanges Icon"));

			PrefabTexture = EditorResources.Load<Texture2D>("GameObject Icon");
			NestedPrefabTexture = EditorResources.Load<Texture2D>("Prefab Icon");

			HeaderBold = new GUIStyle(EditorStyles.boldLabel);
			HeaderBold.fontSize += 10;
		}
	}
}
