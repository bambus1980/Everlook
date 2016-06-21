﻿//
//  MainWindow.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2016 Jarl Gullberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.IO;
using System.Linq;
using System.Drawing;

using Application = Gtk.Application;
using UI = Gtk.Builder.ObjectAttribute;
using GLib;
using Gdk;
using Gtk;

using Everlook.Export.Directory;
using Everlook.Export.Image;
using Everlook.Configuration;
using Everlook.Renderables;
using Everlook.Explorer;
using Everlook.Viewport;
using Everlook.Utility;

using Warcraft.Core;
using Warcraft.BLP;


namespace Everlook
{
	/// <summary>
	/// Main UI class for Everlook. The "partial" qualifier is not strictly needed, but prevents the compiler from
	/// generating errors about the autoconnected members that relate to UI elements.
	/// </summary>
	public partial class MainWindow: Gtk.Window
	{
		/*
			Main UI elements
		*/
		[UI] private readonly ToolButton AboutButton;
		[UI] private readonly AboutDialog AboutDialog;
		[UI] private readonly ToolButton PreferencesButton;

		[UI] private readonly Gtk.Image MainDrawingArea;

		/*
			Export queue elements
		*/
		[UI] private readonly TreeView ExportQueueTreeView;
		[UI] private readonly ListStore ExportQueueListStore;

		[UI] private readonly Menu QueueContextMenu;
		[UI] private readonly ImageMenuItem RemoveQueueItem;

		/*
			Game explorer elements
		*/
		[UI] private readonly TreeView GameExplorerTreeView;
		[UI] private readonly TreeStore GameExplorerTreeStore;
		[UI] private readonly TreeModelFilter GameExplorerTreeFilter;
		[UI] private readonly TreeModelSort GameExplorerTreeSorter;

		[UI] private readonly Menu FileContextMenu;
		[UI] private readonly ImageMenuItem ExtractItem;
		[UI] private readonly ImageMenuItem ExportItem;
		[UI] private readonly ImageMenuItem OpenItem;
		[UI] private readonly ImageMenuItem CopyItem;
		[UI] private readonly ImageMenuItem QueueItem;

		/*
			General item control elements
		*/

		[UI] private readonly Notebook ItemControlNotebook;

		/*
			Image control elements
		*/
		[UI] private readonly ComboBox MipLevelComboBox;
		[UI] private readonly ListStore MipLevelListStore;
		[UI] private readonly CheckButton RenderAlphaCheckButton;
		[UI] private readonly CheckButton RenderRedCheckButton;
		[UI] private readonly CheckButton RenderGreenCheckButton;
		[UI] private readonly CheckButton RenderBlueCheckButton;

		/*
			Model control elements
		*/

		/*
			Animation control elements
		*/

		/*
			Audio control elements
		*/

		/// <summary>
		/// Static reference to the configuration handler.
		/// </summary>
		private readonly EverlookConfiguration Config = EverlookConfiguration.Instance;

		/// <summary>
		/// Background viewport renderer. Handles all rendering in the viewport.
		/// </summary>
		private readonly ViewportRenderer viewportRenderer = new ViewportRenderer();

		/// <summary>
		/// Background file explorer tree builder. Handles enumeration of files in the archives.
		/// </summary>
		private readonly ExplorerBuilder explorerBuilder = new ExplorerBuilder();



		/// <summary>
		/// Creates an instance of the MainWindow class, loading the glade XML UI as needed.
		/// </summary>
		public static MainWindow Create()
		{
			Builder builder = new Builder(null, "Everlook.interfaces.Everlook.glade", null);
			return new MainWindow(builder, builder.GetObject("MainWindow").Handle);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Everlook.MainWindow"/> class.
		/// </summary>
		/// <param name="builder">Builder.</param>
		/// <param name="handle">Handle.</param>
		protected MainWindow(Builder builder, IntPtr handle)
			: base(handle)
		{
			builder.Autoconnect(this);
			DeleteEvent += OnDeleteEvent;

			// TODO: Allow user to configure timeout value
			Timeout.Add(20, OnGLibLoopIdle, Priority.DefaultIdle);

			AboutButton.Clicked += OnAboutButtonClicked;
			PreferencesButton.Clicked += OnPreferencesButtonClicked;

			MainDrawingArea.OverrideBackgroundColor(StateFlags.Normal, Config.GetViewportBackgroundColour());

			GameExplorerTreeView.RowExpanded += OnGameExplorerRowExpanded;
			GameExplorerTreeView.Selection.Changed += OnGameExplorerSelectionChanged;
			GameExplorerTreeView.ButtonPressEvent += OnGameExplorerButtonPressed;

			GameExplorerTreeFilter.VisibleFunc = FilterGameExplorerRow;
			GameExplorerTreeSorter.SetSortFunc(1, SortGameExplorerRow);
			GameExplorerTreeSorter.SetSortColumnId(1, SortType.Descending);

			ExportQueueTreeView.ButtonPressEvent += OnExportQueueButtonPressed;

			ExtractItem.Activated += OnExtractContextItemActivated;
			ExportItem.Activated += OnExportItemContextItemActivated;
			OpenItem.Activated += OnOpenContextItemActivated;
			CopyItem.Activated += OnCopyContextItemActivated;
			QueueItem.Activated += OnQueueContextItemActivated;

			RemoveQueueItem.Activated += OnQueueRemoveContextItemActivated;

			viewportRenderer.FrameRendered += OnFrameRendered;
			viewportRenderer.Start();

			explorerBuilder.PackageGroupAdded += OnPackageGroupAdded;
			explorerBuilder.PackageEnumerated += OnPackageEnumerated;
			//explorerBuilder.EnumerationFinished += OnReferenceEnumerated;
			explorerBuilder.Start();

			/*
				Set up item control sections
			*/
			// Image
			MipLevelComboBox.Changed += OnSelectedMipLevelChanged;

			RenderAlphaCheckButton.Sensitive = false;
			RenderRedCheckButton.Sensitive = false;
			RenderGreenCheckButton.Sensitive = false;
			RenderBlueCheckButton.Sensitive = false;

			// Model

			// Animation

			// Audio
		}

		/// <summary>
		/// Enables the specified control page and brings it to the front.
		/// </summary>
		/// <param name="Page">Page.</param>
		protected void EnableControlPage(ControlPage Page)
		{
			if (Enum.IsDefined(typeof(ControlPage), Page))
			{
				ItemControlNotebook.Page = (int)Page;

				if (Page == ControlPage.Image)
				{
					MipLevelComboBox.Sensitive = true;
					RenderAlphaCheckButton.Sensitive = true;
					RenderRedCheckButton.Sensitive = true;
					RenderGreenCheckButton.Sensitive = true;
					RenderBlueCheckButton.Sensitive = true;
				}
				else if (Page == ControlPage.Model)
				{

				}
				else if (Page == ControlPage.Animation)
				{

				}
				else if (Page == ControlPage.Audio)
				{

				}
			}
		}

		/// <summary>
		/// Disables the specified control page.
		/// </summary>
		/// <param name="Page">Page.</param>
		protected void DisableControlPage(ControlPage Page)
		{
			if (Enum.IsDefined(typeof(ControlPage), Page))
			{
				if (Page == ControlPage.Image)
				{
					MipLevelComboBox.Sensitive = false;
					RenderAlphaCheckButton.Sensitive = false;
					RenderRedCheckButton.Sensitive = false;
					RenderGreenCheckButton.Sensitive = false;
					RenderBlueCheckButton.Sensitive = false;
				}
				else if (Page == ControlPage.Model)
				{

				}
				else if (Page == ControlPage.Animation)
				{

				}
				else if (Page == ControlPage.Audio)
				{

				}
			}
		}

		/// <summary>
		/// Filters the provided game explorer row.
		/// </summary>
		/// <returns><c>true</c>, if the row should be shown in the explorer view, <c>false</c> otherwise.</returns>
		/// <param name="model">Model.</param>
		/// <param name="iter">Iter.</param>
		protected bool FilterGameExplorerRow(ITreeModel model, TreeIter iter)
		{
			return true;
		}

		/// <summary>
		/// Sorts the game explorer row. If <paramref name="iterA"/> should be sorted before
		/// <paramref name="iterB"/>
		/// </summary>
		/// <returns>The sorting priority of the row. This value can be -1, 0 or 1 if
		/// A sorts before B, A sorts with B or A sorts after B, respectively.</returns>
		/// <param name="model">Model.</param>
		/// <param name="iterA">Iter a.</param>
		/// <param name="iterB">Iter b.</param>
		protected int SortGameExplorerRow(ITreeModel model, TreeIter iterA, TreeIter iterB)
		{
			const int SORT_A_BEFORE_B = -1;
			const int SORT_A_WITH_B = 0;
			const int SORT_A_AFTER_B = 1;

			NodeType typeofA = (NodeType)model.GetValue(iterA, 4);
			NodeType typeofB = (NodeType)model.GetValue(iterB, 4);

			if (typeofA < typeofB)
			{
				return SORT_A_AFTER_B;
			}
			if (typeofA > typeofB)
			{
				return SORT_A_BEFORE_B;
			}

			string AComparisonString = (string)model.GetValue(iterA, 1);

			string BComparisonString = (string)model.GetValue(iterB, 1);

			int result = String.CompareOrdinal(AComparisonString, BComparisonString);

			if (result <= SORT_A_BEFORE_B)
			{
				return SORT_A_AFTER_B;
			}

			if (result >= SORT_A_AFTER_B)
			{
				return SORT_A_BEFORE_B;
			}

			return SORT_A_WITH_B;

		}

		/// <summary>
		/// Idle functionality. This code is called as a way of lazily loading rows into the UI
		/// without causing lockups due to sheer data volume.
		/// </summary>
		protected bool OnGLibLoopIdle()
		{
			const bool KEEP_CALLING = true;
			const bool STOP_CALLING = false;

			//if (explorerBuilder.EnumeratedReferences.Count > 0 && bAllowedToAddNewRow)
			if (explorerBuilder.EnumeratedReferences.Count > 0)
			{
				// There's content to be added to the UI

				// Get the last reference in the list.
				ItemReference newContent = explorerBuilder.EnumeratedReferences[explorerBuilder.EnumeratedReferences.Count - 1];

				if (newContent == null)
				{
					explorerBuilder.EnumeratedReferences.RemoveAt(explorerBuilder.EnumeratedReferences.Count - 1);
					return KEEP_CALLING;
				}

				if (newContent.IsFile)
				{
					AddFileNode(newContent.ParentReference, newContent);
				}
				else if (newContent.IsDirectory)
				{
					AddDirectoryNode(newContent.ParentReference, newContent);
				}

				explorerBuilder.EnumeratedReferences.Remove(newContent);
			}

			return KEEP_CALLING;
		}

		/// <summary>
		/// Handles the export item context item activated event.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		protected void OnExportItemContextItemActivated(object sender, EventArgs e)
		{
			TreeIter selectedIter;
			GameExplorerTreeView.Selection.GetSelected(out selectedIter);

			ItemReference fileReference = GetItemReferenceFromStoreIter(GetStoreIterFromSorterIter(selectedIter));
			if (fileReference != null && !String.IsNullOrEmpty(fileReference.ItemPath))
			{

				WarcraftFileType fileType = fileReference.GetReferencedFileType();
				switch (fileType)
				{
					case WarcraftFileType.Directory:
						{
							if (fileReference.IsFullyEnumerated)
							{
								using (EverlookDirectoryExportDialog ExportDialog = EverlookDirectoryExportDialog.Create(fileReference))
								{
									if (ExportDialog.Run() == (int)ResponseType.Ok)
									{
										ExportDialog.RunExport();
									}
									ExportDialog.Destroy();
								}
							}
							else
							{
								// TODO: Implement wait message when the directory and its subdirectories have not yet been enumerated.
							}
							break;
						}
					case WarcraftFileType.BinaryImage:
						{
							using (EverlookImageExportDialog ExportDialog = EverlookImageExportDialog.Create(fileReference))
							{
								if (ExportDialog.Run() == (int)ResponseType.Ok)
								{
									ExportDialog.RunExport();
								}
								ExportDialog.Destroy();
							}
							break;
						}
					default:
						{
							break;
						}
				}
			}
		}

		/// <summary>
		/// Handles extraction of files from the archive triggered by a context menu press.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		protected void OnExtractContextItemActivated(object sender, EventArgs e)
		{
			TreeIter selectedIter;
			GameExplorerTreeView.Selection.GetSelected(out selectedIter);

			ItemReference fileReference = GetItemReferenceFromStoreIter(GetStoreIterFromSorterIter(selectedIter));

			string cleanFilepath = Utilities.ConvertPathSeparatorsToCurrentNative(fileReference.ItemPath);
			string exportpath;
			if (Config.GetShouldKeepFileDirectoryStructure())
			{
				exportpath = Config.GetDefaultExportDirectory() + cleanFilepath;
				Directory.CreateDirectory(Directory.GetParent(exportpath).FullName);
			}
			else
			{
				string filename = System.IO.Path.GetFileName(cleanFilepath);
				exportpath = Config.GetDefaultExportDirectory() + filename;
			}

			byte[] file = fileReference.Extract();
			if (file != null)
			{
				File.WriteAllBytes(exportpath, file);
			}
		}

		/// <summary>
		/// Handles changing the mip level that is being rendered in the viewport based
		/// on the user selection.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		protected void OnSelectedMipLevelChanged(object sender, EventArgs e)
		{
			if (viewportRenderer.IsActive)
			{
				int qualityLevel = MipLevelComboBox.Active;

				if (qualityLevel >= 0)
				{
					viewportRenderer.SetRequestedQualityLevel((uint)qualityLevel);
				}
			}
		}

		/// <summary>
		/// Handles opening of files from the archive triggered by a context menu press.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		protected void OnOpenContextItemActivated(object sender, EventArgs e)
		{
			TreeIter selectedIter;
			GameExplorerTreeView.Selection.GetSelected(out selectedIter);

			ItemReference itemReference = GetItemReferenceFromStoreIter(GetStoreIterFromSorterIter(selectedIter));
			if (!itemReference.IsFile)
			{
				GameExplorerTreeView.ExpandRow(GameExplorerTreeSorter.GetPath(selectedIter), false);
			}
		}

		/// <summary>
		/// Handles copying of the file path of a selected item in the archive, triggered by a
		/// context menu press.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		protected void OnCopyContextItemActivated(object sender, EventArgs e)
		{
			Clipboard clipboard = Clipboard.Get(Atom.Intern("CLIPBOARD", false));

			TreeIter selectedIter;
			GameExplorerTreeView.Selection.GetSelected(out selectedIter);

			clipboard.Text = GetItemReferenceFromStoreIter(GetStoreIterFromSorterIter(selectedIter)).ItemPath;
		}

		/// <summary>
		/// Handles queueing of a selected file in the archive, triggered by a context
		/// menu press.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		protected void OnQueueContextItemActivated(object sender, EventArgs e)
		{
			TreeIter selectedIter;
			GameExplorerTreeView.Selection.GetSelected(out selectedIter);

			ItemReference itemReference = GetItemReferenceFromStoreIter(GetStoreIterFromSorterIter(selectedIter));

			string cleanFilepath = Utilities.ConvertPathSeparatorsToCurrentNative(itemReference.ItemPath);

			if (String.IsNullOrEmpty(cleanFilepath))
			{
				cleanFilepath = itemReference.PackageName;
			}
			else if (String.IsNullOrEmpty(System.IO.Path.GetFileName(cleanFilepath)))
			{
				cleanFilepath = Directory.GetParent(cleanFilepath).FullName.Replace(Directory.GetCurrentDirectory(), "");
			}

			ExportQueueListStore.AppendValues(cleanFilepath, cleanFilepath, "Queued");
		}

		/// <summary>
		/// Displays the About dialog to the user.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		protected void OnAboutButtonClicked(object sender, EventArgs e)
		{
			AboutDialog.Run();
			AboutDialog.Hide();
		}

		/// <summary>
		/// Displays the preferences dialog to the user.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		protected void OnPreferencesButtonClicked(object sender, EventArgs e)
		{
			using (EverlookPreferences PreferencesDialog = EverlookPreferences.Create())
			{
				if (PreferencesDialog.Run() == (int)ResponseType.Ok)
				{
					PreferencesDialog.SavePreferences();
					ReloadRuntimeValues();
				}

				PreferencesDialog.Destroy();
			}
		}

		/// <summary>
		/// Reloads visible runtime values that the user can change in the preferences, such as the colour
		/// of the viewport or the loaded packages.
		/// </summary>
		protected void ReloadRuntimeValues()
		{
			MainDrawingArea.OverrideBackgroundColor(StateFlags.Normal, Config.GetViewportBackgroundColour());

			if (explorerBuilder.HasPackageDirectoryChanged())
			{
				GameExplorerTreeStore.Clear();
				explorerBuilder.Reload();
			}
		}

		/// <summary>
		/// Handles the Frame Rendered event. Takes the input frame from the rendering thread and
		/// draws it in the viewport on the GUI thread.
		/// </summary>
		/// <param name="sender">Sending object (a viewport rendering thread).</param>
		/// <param name="e">Frame renderer arguments, containing the frame and frame delta.</param>
		protected void OnFrameRendered(object sender, FrameRendererEventArgs e)
		{
			Application.Invoke(delegate
				{
					if (MainDrawingArea.Pixbuf != null)
					{
						MainDrawingArea.Pixbuf.Dispose();
					}

					MainDrawingArea.Pixbuf = e.Frame;
				});
		}

		/// <summary>
		/// Handles expansion of rows in the game explorer, enumerating any subfolders and
		/// files present under that row.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		protected void OnGameExplorerRowExpanded(object sender, RowExpandedArgs e)
		{
			// Whenever a row is expanded, enumerate the subfolders of that row.
			ItemReference parentReference = GetItemReferenceFromStoreIter(GetStoreIterFromVisiblePath(e.Path));
			foreach (ItemReference childReference in parentReference.ChildReferences)
			{
				if (childReference.IsDirectory && !childReference.IsEnumerated)
				{
					explorerBuilder.SubmitWork(childReference);
				}
			}
		}

		/// <summary>
		/// Gets a <see cref="TreeIter"/> that's valid for the <see cref="GameExplorerTreeStore"/> from a
		/// <see cref="TreePath"/> visible to the user in the UI.
		/// </summary>
		/// <param name="path">The TreePath.</param>
		/// <returns>A <see cref="TreeIter"/>.</returns>
		private TreeIter GetStoreIterFromVisiblePath(TreePath path)
		{
			TreeIter sorterIter;
			GameExplorerTreeSorter.GetIter(out sorterIter, path);
			return GetStoreIterFromSorterIter(sorterIter);
		}

		/// <summary>
		/// Gets a <see cref="TreeIter"/> that's valid for the <see cref="GameExplorerTreeStore"/> from a TreeIter
		/// valid for the <see cref="GameExplorerTreeSorter"/>.
		/// </summary>
		/// <param name="sorterIter">The GameExplorerTreeSorter iter.</param>
		/// <returns>A <see cref="TreeIter"/>.</returns>
		private TreeIter GetStoreIterFromSorterIter(TreeIter sorterIter)
		{
			TreeIter filterIter = GameExplorerTreeSorter.ConvertIterToChildIter(sorterIter);
			return GetStoreIterFromFilterIter(filterIter);
		}

		/// <summary>
		/// Gets a <see cref="TreeIter"/> that's valid for the <see cref="GameExplorerTreeStore"/> from a TreeIter
		/// valid for the <see cref="GameExplorerTreeFilter"/>.
		/// </summary>
		/// <param name="filterIter">The GameExplorerTreeFilter iter.</param>
		/// <returns>A <see cref="TreeIter"/>.</returns>
		private TreeIter GetStoreIterFromFilterIter(TreeIter filterIter)
		{
			return GameExplorerTreeFilter.ConvertIterToChildIter(filterIter);
		}

		/// <summary>
		/// Handles selection of files in the game explorer, displaying them to the user and routing
		/// whatever rendering functionality the file needs to the viewport.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		protected void OnGameExplorerSelectionChanged(object sender, EventArgs e)
		{
			TreeIter selectedIter;
			GameExplorerTreeView.Selection.GetSelected(out selectedIter);

			ItemReference fileReference = GetItemReferenceFromStoreIter(GetStoreIterFromSorterIter(selectedIter));
			if (fileReference != null && fileReference.IsFile)
			{
				string fileName = System.IO.Path.GetFileName(Utilities.ConvertPathSeparatorsToCurrentNative(fileReference.ItemPath));
				switch (fileReference.GetReferencedFileType())
				{
					case WarcraftFileType.AddonManifest:
						{
							break;
						}
					case WarcraftFileType.AddonManifestSignature:
						{
							break;
						}
					case WarcraftFileType.MoPaQArchive:
						{
							break;
						}
					case WarcraftFileType.ConfigurationFile:
						{
							break;
						}
					case WarcraftFileType.DatabaseContainer:
						{
							break;
						}
					case WarcraftFileType.Shader:
						{
							break;
						}
					case WarcraftFileType.TerrainWater:
						{
							break;
						}
					case WarcraftFileType.TerrainLiquid:
						{
							break;
						}
					case WarcraftFileType.TerrainLevel:
						{
							break;
						}
					case WarcraftFileType.TerrainTable:
						{
							break;
						}
					case WarcraftFileType.TerrainData:
						{
							break;
						}
					case WarcraftFileType.BinaryImage:
						{
							if (!viewportRenderer.IsActive)
							{
								viewportRenderer.Start();
							}

							byte[] fileData = fileReference.Extract();
							if (fileData != null)
							{
								try
								{
									BLP blp = new BLP(fileData);
									RenderableBLP image = new RenderableBLP(blp);
									viewportRenderer.SetRenderTarget(image);

									MipLevelListStore.Clear();
									foreach (string mipString in blp.GetMipMapLevelStrings())
									{
										MipLevelListStore.AppendValues(mipString);
									}

									viewportRenderer.SetRequestedQualityLevel(0);
									MipLevelComboBox.Active = 0;

									EnableControlPage(ControlPage.Image);
								}
								catch (FileLoadException fex)
								{
									Console.WriteLine("FileLoadException when opening BLP: " + fex.Message);
								}
							}

							break;
						}
					case WarcraftFileType.Hashmap:
						{
							break;
						}
					case WarcraftFileType.GameObjectModel:
						{
							break;
						}
					case WarcraftFileType.WorldObjectModel:
						{
							break;
						}
				}

				// Try some "normal" files
				if (fileName.EndsWith(".jpg") || fileName.EndsWith(".gif") || fileName.EndsWith(".png"))
				{
					if (!viewportRenderer.IsActive)
					{
						viewportRenderer.Start();
					}

					byte[] fileData = fileReference.Extract();
					if (fileData != null)
					{
						using (MemoryStream ms = new MemoryStream(fileData))
						{
							Bitmap Image = new Bitmap(ms);
							RenderableBitmap Renderable = new RenderableBitmap(Image);

							viewportRenderer.SetRenderTarget(Renderable);
						}

						// Normal image files don't have mipmap levels
						MipLevelListStore.Clear();
						EnableControlPage(ControlPage.Image);
					}
				}
			}
		}

		/// <summary>
		/// Handles context menu spawning for the game explorer.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		[ConnectBefore]
		protected void OnGameExplorerButtonPressed(object sender, ButtonPressEventArgs e)
		{
			TreePath path;
			GameExplorerTreeView.GetPathAtPos((int)e.Event.X, (int)e.Event.Y, out path);

			ItemReference currentItemReference = null;
			if (path != null)
			{
				TreeIter iter = GetStoreIterFromVisiblePath(path);
				currentItemReference = GetItemReferenceFromStoreIter(iter);
			}

			if (e.Event.Type == EventType.ButtonPress && e.Event.Button == 3)
			{
				if (currentItemReference == null || String.IsNullOrEmpty(currentItemReference.ItemPath))
				{
					ExtractItem.Sensitive = false;
					ExportItem.Sensitive = false;
					OpenItem.Sensitive = false;
					QueueItem.Sensitive = false;
					CopyItem.Sensitive = false;
				}
				else
				{
					if (!currentItemReference.IsFile)
					{
						ExtractItem.Sensitive = false;
						ExportItem.Sensitive = true;
						OpenItem.Sensitive = true;
						QueueItem.Sensitive = true;
						CopyItem.Sensitive = true;
					}
					else
					{
						ExtractItem.Sensitive = true;
						ExportItem.Sensitive = true;
						OpenItem.Sensitive = true;
						QueueItem.Sensitive = true;
						CopyItem.Sensitive = true;
					}
				}


				FileContextMenu.ShowAll();
				FileContextMenu.Popup();
			}
		}

		/// <summary>
		/// Handles context menu spawning for the export queue.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		[ConnectBefore]
		protected void OnExportQueueButtonPressed(object sender, ButtonPressEventArgs e)
		{
			TreePath path;
			ExportQueueTreeView.GetPathAtPos((int)e.Event.X, (int)e.Event.Y, out path);

			ItemReference currentReference = null;
			if (path != null)
			{
				TreeIter iter;
				ExportQueueListStore.GetIterFromString(out iter, path.ToString());
				currentReference = GetItemReferenceFromStoreIter(GetStoreIterFromSorterIter(iter));
			}

			if (e.Event.Type == EventType.ButtonPress && e.Event.Button == 3)
			{
				if (currentReference == null || String.IsNullOrEmpty(currentReference.ItemPath))
				{
					RemoveQueueItem.Sensitive = false;
				}
				else
				{
					RemoveQueueItem.Sensitive = true;
				}

				QueueContextMenu.ShowAll();
				QueueContextMenu.Popup();
			}
		}

		/// <summary>
		/// Handles removal of items from the export queue, triggered by a context menu press.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		protected void OnQueueRemoveContextItemActivated(object sender, EventArgs e)
		{
			TreeIter selectedIter;
			ExportQueueTreeView.Selection.GetSelected(out selectedIter);

			ExportQueueListStore.Remove(ref selectedIter);
		}

		/// <summary>
		/// Handles the package group added event from the explorer builder.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		protected void OnPackageGroupAdded(object sender, ItemEnumeratedEventArgs e)
		{
			Application.Invoke(delegate
				{
					AddPackageGroupNode(e.Item);
				});
		}

		/// <summary>
		/// Adds a package group node to the game explorer view
		/// </summary>
		/// <param name="groupReference">Group reference.</param>
		private void AddPackageGroupNode(ItemReference groupReference)
		{
			// Add the group node
			TreeIter PackageGroupNode = GameExplorerTreeStore.AppendValues("user-home",
				                            groupReference.Group.GroupName, "", "Virtual file tree", (int)NodeType.PackageGroup);
			explorerBuilder.PackageItemNodeMapping.Add(groupReference, PackageGroupNode);
			explorerBuilder.PackageNodeItemMapping.Add(PackageGroupNode, groupReference);

			VirtualItemReference virtualGroupReference = groupReference as VirtualItemReference;
			if (virtualGroupReference != null)
			{
				explorerBuilder.PackageGroupVirtualNodeMapping.Add(groupReference.Group, virtualGroupReference);
			}

			// Add the package folder subnode
			TreeIter PackageFolderNode = GameExplorerTreeStore.AppendValues(PackageGroupNode,
				                             "applications-other", "Packages", "", "Individual packages", (int)NodeType.PackageFolder);
			explorerBuilder.PackageItemNodeMapping.Add(groupReference.ChildReferences.First(), PackageFolderNode);
			explorerBuilder.PackageNodeItemMapping.Add(PackageFolderNode, groupReference.ChildReferences.First());
		}

		/// <summary>
		/// Handles the package enumerated event from the explorer builder.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		protected void OnPackageEnumerated(object sender, ItemEnumeratedEventArgs e)
		{
			Application.Invoke(delegate
				{
					AddPackageNode(e.Item.ParentReference, e.Item);
				});
		}

		/// <summary>
		/// Adds a package node to the game explorer view.
		/// </summary>
		/// <param name="parentReference">Parent reference where the package should be added.</param>
		/// <param name="packageReference">Item reference pointing to the package.</param>
		private void AddPackageNode(ItemReference parentReference, ItemReference packageReference)
		{
			// I'm a new root node
			TreeIter parentNode;
			explorerBuilder.PackageItemNodeMapping.TryGetValue(parentReference, out parentNode);

			if (GameExplorerTreeStore.IterIsValid(parentNode))
			{
				// Add myself to that node
				if (!explorerBuilder.PackageItemNodeMapping.ContainsKey(packageReference))
				{
					TreeIter PackageNode = GameExplorerTreeStore.AppendValues(parentNode,
						                       "package-x-generic", packageReference.PackageName, "", "", (int)NodeType.Package);
					explorerBuilder.PackageItemNodeMapping.Add(packageReference, PackageNode);
					explorerBuilder.PackageNodeItemMapping.Add(PackageNode, packageReference);
				}
			}

			// Map package nodes to virtual root nodes
			VirtualItemReference virtualGroupReference;
			if (explorerBuilder.PackageGroupVirtualNodeMapping.TryGetValue(packageReference.Group, out virtualGroupReference))
			{
				// TODO: Investigate possible bug
				explorerBuilder.AddVirtualMapping(packageReference, virtualGroupReference);
			}
		}

		/// <summary>
		/// Adds a directory node to the game explorer view, attachedt to the provided parent
		/// package and directory.
		/// </summary>
		/// <param name="parentReference">Parent reference where the new directory should be added.</param>
		/// <param name="childReference">Child reference representing the directory.</param>
		private void AddDirectoryNode(ItemReference parentReference, ItemReference childReference)
		{
			TreeIter parentNode;
			explorerBuilder.PackageItemNodeMapping.TryGetValue(parentReference, out parentNode);

			if (GameExplorerTreeStore.IterIsValid(parentNode))
			{
				// Add myself to that node
				if (!explorerBuilder.PackageItemNodeMapping.ContainsKey(childReference))
				{
					TreeIter node = CreateDirectoryTreeNode(parentNode, childReference);
					explorerBuilder.PackageItemNodeMapping.Add(childReference, node);
					explorerBuilder.PackageNodeItemMapping.Add(node, childReference);
				}
			}

			// Now, let's add (or append to) the virtual node
			VirtualItemReference virtualParentReference = explorerBuilder.GetVirtualReference(parentReference);

			if (virtualParentReference != null)
			{
				TreeIter virtualParentNode;
				explorerBuilder.PackageItemNodeMapping.TryGetValue(virtualParentReference, out virtualParentNode);

				if (GameExplorerTreeStore.IterIsValid(virtualParentNode))
				{

					VirtualItemReference virtualChildReference = explorerBuilder.GetVirtualReference(childReference);
					//explorerBuilder.VirtualReferenceMapping.TryGetValue(childReference, out virtualChildReference);

					if (virtualChildReference != null)
					{
						// Append this directory reference as an additional overridden hard reference
						virtualChildReference.OverriddenHardReferences.Add(childReference);
					}
					else
					{
						if (childReference.GetReferencedItemName() == "WTF")
						{
							Console.WriteLine("");
						}
						virtualChildReference = new VirtualItemReference(virtualParentReference, childReference.Group, childReference);

						if (!virtualParentReference.ChildReferences.Contains(virtualChildReference))
						{
							virtualParentReference.ChildReferences.Add(virtualChildReference);

							// Create a new virtual reference and a node that maps to it.
							if (!explorerBuilder.PackageItemNodeMapping.ContainsKey(virtualChildReference))
							{

								TreeIter node = CreateDirectoryTreeNode(virtualParentNode, virtualChildReference);

								explorerBuilder.PackageItemNodeMapping.Add(virtualChildReference, node);
								explorerBuilder.PackageNodeItemMapping.Add(node, virtualChildReference);

								// Needs to be a path, not a reference
								explorerBuilder.AddVirtualMapping(childReference, virtualChildReference);
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Creates a node in the <see cref="GameExplorerTreeView"/> for the specified directory reference, as
		/// a child below the specified parent node.
		/// </summary>
		/// <param name="parentNode">The parent node where the new node should be attached.</param>
		/// <param name="directory">The <see cref="ItemReference"/> describing the directory.</param>
		/// <returns>A <see cref="TreeIter"/> pointing to the new directory node.</returns>
		private TreeIter CreateDirectoryTreeNode(TreeIter parentNode, ItemReference directory)
		{
			return GameExplorerTreeStore.AppendValues(parentNode,
				Stock.Directory, directory.GetReferencedItemName(), "", "", (int)NodeType.Directory);
		}

		/// <summary>
		/// Adds a file node to the game explorer view, attached to the provided parent
		/// package and directory.
		/// </summary>
		/// <param name="parentReference">Parent file reference</param>
		/// <param name="childReference">Child file reference.</param>
		private void AddFileNode(ItemReference parentReference, ItemReference childReference)
		{
			TreeIter parentNode;
			explorerBuilder.PackageItemNodeMapping.TryGetValue(parentReference, out parentNode);

			if (GameExplorerTreeStore.IterIsValid(parentNode))
			{
				// Add myself to that node
				if (!explorerBuilder.PackageItemNodeMapping.ContainsKey(childReference))
				{
					parentReference.ChildReferences.Add(childReference);

					TreeIter node = CreateFileTreeNode(parentNode, childReference);

					explorerBuilder.PackageItemNodeMapping.Add(childReference, node);
					explorerBuilder.PackageNodeItemMapping.Add(node, childReference);
				}
			}

			// Now, let's add (or append to) the virtual node
			VirtualItemReference virtualParentReference = explorerBuilder.GetVirtualReference(parentReference);

			if (virtualParentReference != null)
			{
				TreeIter virtualParentNode;
				explorerBuilder.PackageItemNodeMapping.TryGetValue(virtualParentReference, out virtualParentNode);

				if (GameExplorerTreeStore.IterIsValid(virtualParentNode))
				{

					VirtualItemReference virtualChildReference = explorerBuilder.GetVirtualReference(childReference);

					if (virtualChildReference != null)
					{
						// Append this directory reference as an additional overridden hard reference
						virtualChildReference.OverriddenHardReferences.Add(childReference);
					}
					else
					{
						virtualChildReference = new VirtualItemReference(virtualParentReference, childReference.Group, childReference);

						if (!virtualParentReference.ChildReferences.Contains(virtualChildReference))
						{
							virtualParentReference.ChildReferences.Add(virtualChildReference);
							// Create a new virtual reference and a node that maps to it.
							if (!explorerBuilder.PackageItemNodeMapping.ContainsKey(virtualChildReference))
							{
								TreeIter node = CreateFileTreeNode(virtualParentNode, virtualChildReference);

								explorerBuilder.PackageItemNodeMapping.Add(virtualChildReference, node);
								explorerBuilder.PackageNodeItemMapping.Add(node, virtualChildReference);

								explorerBuilder.AddVirtualMapping(childReference, virtualChildReference);
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Creates a node in the <see cref="GameExplorerTreeView"/> for the specified file reference, as
		/// a child below the specified parent node.
		/// </summary>
		/// <param name="parentNode">The parent node where the new node should be attached.</param>
		/// <param name="file">The <see cref="ItemReference"/> describing the file.</param>
		/// <returns>A <see cref="TreeIter"/> pointing to the new directory node.</returns>
		private TreeIter CreateFileTreeNode(TreeIter parentNode, ItemReference file)
		{
			return GameExplorerTreeStore.AppendValues(parentNode, Utilities.GetIconForFiletype(file.ItemPath),
				file.GetReferencedItemName(), "", "", (int)NodeType.File);
		}

		/// <summary>
		/// Unlocks a folder reference for exploration once it's been completely enumerated at the top level.
		/// </summary>
		/// <param name="sender">The sending object.</param>
		/// <param name="e">The event arguments containing the list of enumeration results.</param>
		protected void OnReferenceEnumerated(object sender, ItemEnumeratedEventArgs e)
		{

		}

		/// <summary>
		/// Converts a <see cref="TreeIter"/> into an <see cref="ItemReference"/>. The reference object is queried
		/// from the explorerBuilder's internal store.
		/// </summary>
		/// <returns>The ItemReference object pointed to by the TreeIter.</returns>
		/// <param name="iter">The TreeIter.</param>
		private ItemReference GetItemReferenceFromStoreIter(TreeIter iter)
		{
			ItemReference reference;
			if (explorerBuilder.PackageNodeItemMapping.TryGetValue(iter, out reference))
			{
				return reference;
			}

			return null;
		}

		/// <summary>
		/// Handles application shutdown procedures - terminating render threads, cleaning
		/// up the UI, etc.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="a">The alpha component.</param>
		protected void OnDeleteEvent(object sender, DeleteEventArgs a)
		{
			if (viewportRenderer.IsActive)
			{
				viewportRenderer.Stop();
			}

			if (explorerBuilder.IsActive)
			{
				explorerBuilder.Stop();
				explorerBuilder.Dispose();
			}

			Application.Quit();
			a.RetVal = true;
		}
	}

	/// <summary>
	/// Available control pages in the Everlook UI.
	/// </summary>
	public enum ControlPage
	{
		/// <summary>
		/// Image control page. Handles mip levels and rendered channels.
		/// </summary>
		Image = 0,

		/// <summary>
		/// Model control page. Handles vertex joining, geoset rendering and other model
		/// settings.
		/// </summary>
		Model = 1,

		/// <summary>
		/// Animation control page. Handles active animations and their settings.
		/// </summary>
		Animation = 2,

		/// <summary>
		/// Audio control page. Handles playback of audio.
		/// </summary>
		Audio = 3
	}
}