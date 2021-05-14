using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;

namespace Yorozu.PrefabDiffViewer
{
	internal class DetailTreeView : TreeView
	{
		public DetailTreeView(TreeViewState state) : base(state)
		{
			showBorder = true;
			showAlternatingRowBackgrounds = true;
			Reload();
		}

		public DetailTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader) : base(state, multiColumnHeader) { }

		private DiffTreeViewItem _item;

		public void SetItem(DiffTreeViewItem item)
		{
			_item = item;
			Reload();
			ExpandAll();
		}

		protected override TreeViewItem BuildRoot()
		{
			var root = new TreeViewItem(-1, -1);
			if (_item != null)
			{
				_item.SetChild(root);
				SetupDepthsFromParentsAndChildren(root);
			}

			if (root.children == null)
			{
				SetupParentsAndChildrenFromDepths(root, new List<TreeViewItem>());
			}
			return root;
		}
	}
}
