﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Consist.View
{
	public class TreeListView : TreeView
	{
		static TreeListView()
		{
			//Override the default style and the default control template
			DefaultStyleKeyProperty.OverrideMetadata(typeof(TreeListView), new FrameworkPropertyMetadata(typeof(TreeListView)));
			
		}

		/// <summary>
		/// Initialize a new instance of TreeListView.
		/// </summary>
		public TreeListView()
		{
			Columns = new GridViewColumnCollection();
		}

		#region Properties
		/// <summary>
		/// Gets or sets the collection of System.Windows.Controls.GridViewColumn 
		/// objects that is defined for this TreeListView.
		/// </summary>
		public GridViewColumnCollection Columns
		{
			get { return (GridViewColumnCollection)GetValue(ColumnsProperty); }
			set { SetValue(ColumnsProperty, value); }
		}

		/// <summary>
		/// Gets or sets whether columns in a TreeListView can be
		/// reordered by a drag-and-drop operation. This is a dependency property.
		/// </summary>
		public bool AllowsColumnReorder
		{
			get { return (bool)GetValue(AllowsColumnReorderProperty); }
			set { SetValue(AllowsColumnReorderProperty, value); }
		}
		#endregion

		#region Static Dependency Properties
		// Using a DependencyProperty as the backing store for AllowsColumnReorder.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty AllowsColumnReorderProperty =
			DependencyProperty.Register("AllowsColumnReorder", typeof(bool), typeof(TreeListView), new UIPropertyMetadata(null));

		// Using a DependencyProperty as the backing store for Columns.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ColumnsProperty =
			DependencyProperty.Register("Columns", typeof(GridViewColumnCollection),
			typeof(TreeListView),
			new UIPropertyMetadata(null));
		#endregion
	}
}
