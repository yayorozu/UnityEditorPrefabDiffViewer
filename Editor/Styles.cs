using System;
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
			AddTexture = Load("CollabCreate Icon");
			SubTexture = Load("CollabDeleted Icon");
			ModifyTexture = Load("CollabChanges Icon");
			PrefabTexture = Load("GameObject Icon");
			NestedPrefabTexture = Load("Prefab Icon");
			InfoTexture = Load("CollabEdit Icon");
			ScriptTexture = Load("d_cs Script Icon");

			HeaderBold = new GUIStyle(EditorStyles.boldLabel);
			HeaderBold.fontSize += 10;
		}

		private static Texture2D Load(string path)
		{
			try
			{
				return EditorResources.Load<Texture2D>(path);
			}
			catch
			{
				return null;
			}
		}
	}
}
