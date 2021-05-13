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
}
