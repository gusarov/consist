﻿using Consist.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Consist.View
{
	/// <summary>
	/// Interaction logic for Explorer.xaml
	/// </summary>
	public partial class Explorer : UserControl
	{
		public Explorer()
		{
			InitializeComponent();
			DataContext = RootDataContext.Instance.TreeRoot;
		}

		private void StackPanel_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ClickCount > 1)
			{
				var model = (RecordViewModel) ((StackPanel) sender).DataContext;
				model.IsExpanded = !model.IsExpanded;
				/*
				var pos = e.GetPosition((StackPanel)sender);

				var list = new List<DependencyObject>();
				VisualTreeHelper.HitTest((StackPanel) sender,
					x => { return HitTestFilterBehavior.Continue; },
					x =>
					{
						list.Add(x.VisualHit);
						return HitTestResultBehavior.Continue;
					},
					new PointHitTestParameters(pos));
					*/

			}
		}

		private void TreeListView_KeyDown(object sender, KeyEventArgs e)
		{
			var tlv = (TreeListView) sender;
			if (e .Key == Key.Right)
			{
				var m = tlv.SelectedItem;
				var v = tlv.SelectedValue;
			}
		}

		T FindAncestor<T>(FrameworkElement fe)
		{
			if (fe is T t)
			{
				return t;
			}

			if (fe is null)
			{
				return default;
			}
			return FindAncestor<T>((FrameworkElement)fe.Parent);
		}

		private void StackPanel_KeyDown(object sender, KeyEventArgs e)
		{
			var panel = (StackPanel)sender;
			if (e.Key == Key.Right)
			{
				var tlv = FindAncestor<TreeListView>(panel);
				var m = tlv.SelectedItem;
				var v = tlv.SelectedValue;
			}
		}
	}
}
