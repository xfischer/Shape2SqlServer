using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.IO.IsolatedStorage;
using System.Diagnostics;
using Microsoft.SqlServer.Types;
using System.Text.RegularExpressions;
using System.Data.SqlClient;
using GeoAPI.CoordinateSystems.Transformations;
using GeoAPI.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using NetTopologySuite.IO;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using NetTopologySuite.CoordinateSystems.Transformations;
using MapBind.Shape2SqlServer.Properties;
using MapBind.IO;
using MapBind.IO.ShapeFile;
using Microsoft.Data.ConnectionUI;
using MapBind.IO.Utils;
using MapBind.Data.Models.SqlServer;
using MapBind.IO.CoordinateSystem;

namespace MapBind.Shape2SqlServer
{
	public partial class frmMain : Form
	{
		private delegate void showErrorDelegate(ShapeImportExceptionEventArgs ex);
		private delegate void progressChangedDelegate(int value, string message);
		private ShapeFileImporter _importer;

		public frmMain()
		{
			InitializeComponent();

			InitTracing();
		}

		private void InitTracing()
		{
			File.Delete("MapBind.Shape2SqlServer.shared.log");
			MapBindTrace.Source.Listeners.Clear();
			TextWriterTraceListener txtListener = new TextWriterTraceListener("MapBind.Shape2SqlServer.shared.log", "MapBind.Shape2SqlServer.TraceListener");
			MapBindTrace.Source.Switch.Level = SourceLevels.All;
			MapBindTrace.Source.Listeners.Remove("Default");
			MapBindTrace.Source.Listeners.Add(txtListener);
			Trace.Listeners.Add(txtListener);
			Trace.AutoFlush = true;



			MapBindTrace.Source.TraceInformation("Trace file created on " + DateTime.Now.ToString());
		}

		#region User events

		private void frmMain_Load(object sender, EventArgs e)
		{
			LoadSettings();

		}

		private void btnImport_Click(object sender, EventArgs e)
		{
			try
			{

				SaveSettings();

				btnImport.Visible = false;
				btnCancel.Visible = true;
				toolStripProgressBar1.Visible = true;

				this.ImportShapeFile(chkSafeMode.Checked);


			}
			catch (Exception ex)
			{
				MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void btnBrowse_Click(object sender, EventArgs e)
		{
			if (dlgOpen.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				this.InitIHM_ShapeFile(dlgOpen.FileName);
			}
		}

		private void chkReproject_CheckedChanged(object sender, EventArgs e)
		{
			txtCoordSys.Enabled = chkReproject.Checked;
		}

		private void chkSRID_CheckedChanged(object sender, EventArgs e)
		{
			txtSrid.Enabled = chkSRID.Checked;
		}

		private void frmMain_FormClosed(object sender, FormClosedEventArgs e)
		{
			SaveSettings();
		}

		private void btnConString_Click(object sender, EventArgs e)
		{
			string conString = txtConString.Text;
			this.ShowConnectionStringDialog(ref conString);
			txtConString.Text = conString;
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			_importer.CancelAsync();
		}

		private void lnkCSSelector_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			this.ShowSRIDSelector();
		}

		private void lblShapeHeader_Click(object sender, EventArgs e)
		{
			MessageBox.Show(lblShapeHeader.Tag.ToString(), "Coordinate System", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
		#endregion

		#region Private

		#region Helpers

		// For checkedlist binding
		private class ShapeField
		{
			public string Name { get; set; }
			public string Type { get; set; }
			public string FullName { get { return Name + " (" + Type + ")"; } }
		}

		private void LoadSettings()
		{
			this.InitIHM_ShapeFile(Properties.Settings.Default.shapeFile);
			txtConString.Text = Properties.Settings.Default.connectionString;
			txtCoordSys.Text = Properties.Settings.Default.coordSys;
			chkDrop.Checked = Properties.Settings.Default.dropTable;
			chkReproject.Checked = Properties.Settings.Default.reproject;
			chkSafeMode.Checked = Properties.Settings.Default.safeMode;
			chkSRID.Checked = Properties.Settings.Default.setSRID;
			txtSrid.Text = Properties.Settings.Default.SRID;
			switch ((enSpatialType)Properties.Settings.Default.useGeography)
			{
				case enSpatialType.both: radBoth.Checked = true; break;
				case enSpatialType.geography: radGeog.Checked = true; break;
				case enSpatialType.geometry: radGeom.Checked = true; break;
			}
		}

		private void SaveSettings()
		{
			Properties.Settings.Default.shapeFile = txtSHP.Text;
			Properties.Settings.Default.connectionString = txtConString.Text;
			Properties.Settings.Default.coordSys = txtCoordSys.Text;
			Properties.Settings.Default.dropTable = chkDrop.Checked;
			Properties.Settings.Default.reproject = chkReproject.Checked;
			Properties.Settings.Default.safeMode = chkSafeMode.Checked;
			Properties.Settings.Default.setSRID = chkSRID.Checked;
			Properties.Settings.Default.SRID = txtSrid.Text;
			Properties.Settings.Default.useGeography = radGeog.Checked ? (int)enSpatialType.geography : radGeom.Checked ? (int)enSpatialType.geometry : (int)enSpatialType.both;
			Properties.Settings.Default.Save();
		}

		private void ShowConnectionStringDialog(ref string connectionString)
		{
			DataConnectionDialog dlg = new DataConnectionDialog();
			DataSource.AddStandardDataSources(dlg);
			dlg.SelectedDataSource = DataSource.SqlDataSource;
			dlg.SelectedDataProvider = DataProvider.SqlDataProvider;
			try
			{
				dlg.ConnectionString = connectionString;
			}
			catch
			{ }
			finally
			{
				if (DataConnectionDialog.Show(dlg) == DialogResult.OK)
				{
					connectionString = dlg.ConnectionString;
				}
			}
		}

		#endregion

		private void ShowSRIDSelector()
		{
			frmSRIDSelector frmSelector = new frmSRIDSelector();
			if (frmSelector.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				txtCoordSys.Text = frmSelector.SelectedWKT;
				if (!string.IsNullOrEmpty(txtCoordSys.Text))
					chkReproject.Checked = true;
			}
		}

		private void InitIHM_ShapeFile(string shapeFile)
		{
			try
			{
				if (!File.Exists(shapeFile))
					return;

				ShapeFileImporter importer = new ShapeFileImporter(shapeFile);

				txtSHP.Text = shapeFile;
				lblShapeHeader.Text = string.Format("{0} {1} in shapefile\n{2}", importer.RecordCount, importer.ShapeType, importer.Bounds, importer.CoordinateSystem);
				lblShapeHeader.Tag = importer.CoordinateSystem;
				txtTableName.Text = importer.SqlTableName;
				txtIDCol.Text = importer.SqlIDFIeld;
				txtGeomCol.Text = importer.SqlGeomField;
				toolStripProgressBar1.Minimum = 0;
				toolStripProgressBar1.Maximum = importer.RecordCount;
				toolStripProgressBar1.Value = 0;
				toolStripProgressBar1.Step = 1;
				
				//ICoordinateSystem csSource = ProjNet.Converters.WellKnownText.CoordinateSystemWktReader.Parse(importer.CoordinateSystem) as ICoordinateSystem;
				//ICoordinateSystem csTarget = ProjNet.Converters.WellKnownText.CoordinateSystemWktReader.Parse(txtCoordSys.Text) as ICoordinateSystem;

				//bool testEq = csSource.EqualParams(csTarget);
				

				BindingList<ShapeField> bindList = new BindingList<ShapeField>();
				foreach (var kv in importer.Fields)
				{
					bindList.Add(new ShapeField() { Name = kv.Key, Type = kv.Value.Name.ToString() });
				}
				lstColumns.DataSource = bindList;
				lstColumns.DisplayMember = "FullName";
				lstColumns.ValueMember = "Name";

				for (int i = 0; i < lstColumns.Items.Count; i++)
				{
					lstColumns.SetItemChecked(i, true);
				}

			}
			catch (Exception ex)
			{
				MessageBox.Show("Error with shape file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		#endregion

		#region Import Stuff

		private void ImportShapeFile(bool safeMode)
		{
			try
			{

				// txtCoordSys.Text, txtConString.Text, chkDrop.Checked, radGeog.Checked, chkSRID.Checked ? txtSrid.Text : null
				_importer = new ShapeFileImporter(txtSHP.Text);
				_importer.ProgressChanged += new EventHandler<ProgressChangedEventArgs>(_importer_ProgressChanged);
				_importer.Done += new EventHandler(importer_Done);
				_importer.Error += new EventHandler<ShapeImportExceptionEventArgs>(importer_Error);

				toolStripProgressBar1.Value = 0;
				toolStripProgressBar1.Maximum = 100;

				List<string> selectedFields = new List<string>();
				foreach (var item in lstColumns.CheckedItems)
					selectedFields.Add(((ShapeField)item).Name);


				if (safeMode)
					_importer.ImportShapeFile(txtConString.Text,
					chkReproject.Checked ? txtCoordSys.Text : null,
					chkDrop.Checked,
					radGeog.Checked ? enSpatialType.geography : radGeom.Checked ? enSpatialType.geometry : enSpatialType.both,
					chkSRID.Checked ? int.Parse(txtSrid.Text) : 0,
					txtTableName.Text,
					txtIDCol.Text,
					txtGeomCol.Text,
					selectedFields);
				else
					_importer.ImportShapeFile_Direct(txtConString.Text,
						chkReproject.Checked ? txtCoordSys.Text : null,
						chkDrop.Checked,
						radGeog.Checked ? enSpatialType.geography : radGeom.Checked ? enSpatialType.geometry : enSpatialType.both,
						chkSRID.Checked ? int.Parse(txtSrid.Text) : 0,
						txtTableName.Text,
						txtIDCol.Text,
						txtGeomCol.Text,
						selectedFields);


			}
			catch (Exception ex)
			{
				MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			finally
			{

			}

		}

		private void importer_Error(object sender, ShapeImportExceptionEventArgs e)
		{
			showErrorDelegate del = new showErrorDelegate(showError);
			this.Invoke(del, new object[] { e });

		}
		private void showError(ShapeImportExceptionEventArgs ex)
		{
			if (ex.IsTerminating)
			{
				MessageBox.Show(this, "Error: " + ((Exception)ex.ExceptionObject).Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			else
			{
				if (MessageBox.Show(this, string.Format("Error: {0}\n\n" +
									"Current shape:\nIndex: #{1}\n{2}" +
									"\nShape geometry has been written to log file." +
									"\nClick OK to ignore, Cancel to abort."
									, ((Exception)ex.ExceptionObject).Message
									, ex.ShapeIndex
									, ex.ShapeInfo)
									, "Error", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.Cancel)
					_importer.CancelAsync();
				else
					ex.Ignore = true;
			}

		}

		private void importer_Done(object sender, EventArgs e)
		{
			btnCancel.Visible = false;
			btnImport.Visible = true;
			toolStripProgressBar1.Visible = false;

			toolStripProgressBar1.Value = 0;
			toolStripStatusLabel1.Text = "Ready";

			MessageBox.Show(this, "Shape file imported.", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);

			//MapBindTrace.Source.Close();
		}

		void _importer_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			progressChangedDelegate del = new progressChangedDelegate(doChangeProgress);
			this.Invoke(del, new object[] { e.ProgressPercentage, e.UserState.ToString() });
		}

		private void doChangeProgress(int value, string message)
		{
			toolStripProgressBar1.Value = Math.Min(toolStripProgressBar1.Maximum, value);
			toolStripStatusLabel1.Text = message;
		}



		#endregion







	}
}





