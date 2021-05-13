using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Yorozu.PrefabDiffViewer
{
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
				root.AddChild(_diff.Convert());
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

		protected override void RowGUI(RowGUIArgs args)
		{
			var item = args.item as DiffTreeViewItem;
			base.RowGUI(args);
			if (item.SubIcon == null)
				return;

			var rect = new Rect(args.rowRect)
			{
				x = 0,
				width = 18f,
			};
			GUI.DrawTexture(rect, item.SubIcon, ScaleMode.ScaleToFit);
		}
	}
}
