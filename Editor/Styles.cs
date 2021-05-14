using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;

namespace Yorozu.PrefabDiffViewer
{
	internal static class Styles
	{
		internal static Texture2D AddTexture;
		internal static Texture2D SubTexture;
		internal static Texture2D ModifyTexture;
		internal static Texture2D PrefabTexture;
		internal static Texture2D NestedPrefabTexture;
		internal static Texture2D InfoTexture;
		internal static Texture2D ScriptTexture;

		internal static GUIStyle HeaderBold;

		static Styles()
		{
			AddTexture = EditorResources.Load<Texture2D>("CollabCreate Icon");
			SubTexture = EditorResources.Load<Texture2D>("CollabDeleted Icon");
			ModifyTexture = EditorResources.Load<Texture2D>("CollabChanges Icon");
			PrefabTexture = EditorResources.Load<Texture2D>("GameObject Icon");
			NestedPrefabTexture = EditorResources.Load<Texture2D>("Prefab Icon");
			InfoTexture = EditorResources.Load<Texture2D>("CollabEdit Icon");
			ScriptTexture = EditorResources.Load<Texture2D>("d_cs Script Icon");

			HeaderBold = new GUIStyle(EditorStyles.boldLabel);
			HeaderBold.fontSize += 10;
		}
	}
}
