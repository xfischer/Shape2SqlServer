#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using Microsoft.Extensions.Logging;
using Shape2SqlServer.Core;

namespace Shape2SqlServer;

public partial class frmMain : Form
{
	private readonly ILoggerFactory _loggerFactory;
	private readonly ILogger<frmMain> _logger;
	private delegate void showErrorDelegate(ShapeImportExceptionEventArgs ex);
	private delegate void progressChangedDelegate(int value, string message);
	private ShapeFileImporter? _importer;

	public frmMain(ILoggerFactory loggerFactory)
	{
		_loggerFactory = loggerFactory;
		_logger = _loggerFactory.CreateLogger<frmMain>();
		InitializeComponent();
		InitLogging();
	}

	private void InitLogging() =>
		_logger.LogInformation("Application started on {Date}", DateTime.Now);

	#region User events

	private void frmMain_Load(object sender, EventArgs e) => LoadSettings();

	private void btnImport_Click(object sender, EventArgs e)
	{
		try
		{
			SaveSettings();

			btnImport.Visible = false;
			btnCancel.Visible = true;
			toolStripProgressBar1.Visible = true;

			ImportShapeFile(chkSafeMode.Checked);
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}

	private void btnBrowse_Click(object sender, EventArgs e)
	{
		if (dlgOpen.ShowDialog() == DialogResult.OK)
		{
			InitGUI_ShapeFile(dlgOpen.FileName);
		}
	}

	private void chkReproject_CheckedChanged(object sender, EventArgs e) => txtCoordSys.Enabled = chkReproject.Checked;

	private void chkSRID_CheckedChanged(object sender, EventArgs e) => txtSrid.Enabled = chkSRID.Checked;

	private void frmMain_FormClosed(object sender, FormClosedEventArgs e) => SaveSettings();

	private void btnConString_Click(object sender, EventArgs e)
	{
		string conString = txtConString.Text;
		ShowConnectionStringDialog(ref conString);
		txtConString.Text = conString;
	}

	private void btnCancel_Click(object sender, EventArgs e) => _importer?.CancelAsync();

	private void lnkCSSelector_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) => ShowSRIDSelector();

	private void lblShapeHeader_Click(object sender, EventArgs e) =>
		MessageBox.Show(lblShapeHeader.Tag?.ToString() ?? "", "Coordinate System", MessageBoxButtons.OK, MessageBoxIcon.Information);

	#endregion

	#region Private

	#region Helpers

	// For checkedlist binding
	private class ShapeField
	{
		public required string Name { get; set; }
		public required string Type { get; set; }
		public string FullName => $"{Name} ({Type})";
	}

	private void LoadSettings()
	{
		InitGUI_ShapeFile(Properties.Settings.Default.shapeFile ?? "");
		txtConString.Text = Properties.Settings.Default.connectionString ?? "";
		txtCoordSys.Text = Properties.Settings.Default.coordSys ?? "";
		chkDrop.Checked = Properties.Settings.Default.dropTable;
		chkReproject.Checked = Properties.Settings.Default.reproject;
		chkSafeMode.Checked = Properties.Settings.Default.safeMode;
		chkSRID.Checked = Properties.Settings.Default.setSRID;
		txtSrid.Text = Properties.Settings.Default.SRID ?? "";
		txtSchema.Text = Properties.Settings.Default.schema ?? "";
		chkCreateSpatialIndex.Checked = Properties.Settings.Default.createSpatialIndex;

		switch ((enSpatialType)Properties.Settings.Default.useGeography)
		{
			case enSpatialType.both:
				radBoth.Checked = true;
				break;
			case enSpatialType.geography:
				radGeog.Checked = true;
				break;
			case enSpatialType.geometry:
				radGeom.Checked = true;
				break;
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
		Properties.Settings.Default.schema = txtSchema.Text;
		Properties.Settings.Default.createSpatialIndex = chkCreateSpatialIndex.Checked;
		Properties.Settings.Default.useGeography = radGeog.Checked ? (int)enSpatialType.geography
			: radGeom.Checked ? (int)enSpatialType.geometry
			: (int)enSpatialType.both;
		Properties.Settings.Default.Save();
	}

	private void ShowConnectionStringDialog(ref string connectionString)
	{
		using frmConnectionDialog dlg = new();
		dlg.ConnectionString = connectionString;
		if (dlg.ShowDialog(this) == DialogResult.OK)
		{
			connectionString = dlg.ConnectionString;
		}
	}

	#endregion

	private void ShowSRIDSelector()
	{
		using frmSRIDSelector frmSelector = new();
		if (frmSelector.ShowDialog() == DialogResult.OK)
		{
			txtCoordSys.Text = frmSelector.SelectedWKT;
			if (!string.IsNullOrEmpty(txtCoordSys.Text))
				chkReproject.Checked = true;
		}
	}

	private void InitGUI_ShapeFile(string shapeFile)
	{
		try
		{
			if (!File.Exists(shapeFile))
				return;

			ShapeFileImporter importer = new(shapeFile, _loggerFactory.CreateLogger<ShapeFileImporter>());

			txtSHP.Text = shapeFile;
			lblShapeHeader.Text = $"{importer.RecordCount} {importer.ShapeType} in shapefile\n{importer.Bounds}, {importer.CoordinateSystem}";
			lblShapeHeader.Tag = importer.CoordinateSystem;
			txtTableName.Text = importer.SqlTableName;
			txtIDCol.Text = importer.SqlIDFIeld;
			txtGeomCol.Text = importer.SqlGeomField;
			toolStripProgressBar1.Minimum = 0;
			toolStripProgressBar1.Maximum = importer.RecordCount;
			toolStripProgressBar1.Value = 0;
			toolStripProgressBar1.Step = 1;

			BindingList<ShapeField> bindList = [];
			foreach (var kv in importer.Fields)
			{
				bindList.Add(new ShapeField { Name = kv.Key, Type = kv.Value.Name.ToString() });
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
			MessageBox.Show($"Error with shape file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}

		#endregion

	#region Import Stuff

	private void ImportShapeFile(bool safeMode)
	{
		try
		{
			_importer = new ShapeFileImporter(txtSHP.Text, _loggerFactory.CreateLogger<ShapeFileImporter>());
			_importer.ProgressChanged += _importer_ProgressChanged;
			_importer.Done += importer_Done;
			_importer.Error += importer_Error;

			toolStripProgressBar1.Value = 0;
			toolStripProgressBar1.Maximum = 100;

			List<string> selectedFields = [];
			foreach (var item in lstColumns.CheckedItems)
				selectedFields.Add(((ShapeField)item).Name);

			enSpatialType spatialType = radGeog.Checked ? enSpatialType.geography
				: radGeom.Checked ? enSpatialType.geometry
				: enSpatialType.both;

			if (safeMode)
				_importer.ImportShapeFile(
					txtConString.Text,
					chkReproject.Checked ? txtCoordSys.Text : null,
					chkDrop.Checked,
					spatialType,
					chkSRID.Checked ? int.Parse(txtSrid.Text) : 0,
					txtTableName.Text,
					txtSchema.Text,
					txtIDCol.Text,
					txtGeomCol.Text,
					selectedFields,
					chkCreateSpatialIndex.Checked);
			else
				_importer.ImportShapeFile_Direct(
					txtConString.Text,
					chkReproject.Checked ? txtCoordSys.Text : null,
					chkDrop.Checked,
					spatialType,
					chkSRID.Checked ? int.Parse(txtSrid.Text) : 0,
					txtTableName.Text,
					txtSchema.Text,
					txtIDCol.Text,
					txtGeomCol.Text,
					selectedFields,
					chkCreateSpatialIndex.Checked);
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}

	private void importer_Error(object? sender, ShapeImportExceptionEventArgs e)
	{
		showErrorDelegate del = new(showError);
		Invoke(del, [e]);
	}

	private void showError(ShapeImportExceptionEventArgs ex)
	{
		if (ex.IsTerminating)
		{
			MessageBox.Show(this, $"Error: {((Exception)ex.ExceptionObject).Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
		else
		{
			string message = $"Error: {((Exception)ex.ExceptionObject).Message}\n\n" +
							 $"Current shape:\nIndex: #{ex.ShapeIndex}\n{ex.ShapeInfo}" +
							 "\nShape geometry has been written to log file." +
							 "\nClick OK to ignore, Cancel to abort.";

			if (MessageBox.Show(this, message, "Error", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.Cancel)
				_importer?.CancelAsync();
			else
				ex.Ignore = true;
		}
	}

	private void importer_Done(object? sender, EventArgs e)
	{
		btnCancel.Visible = false;
		btnImport.Visible = true;
		toolStripProgressBar1.Visible = false;

		toolStripProgressBar1.Value = 0;
		toolStripStatusLabel1.Text = "Ready";

		MessageBox.Show(this, "Shape file imported.", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
	}

	void _importer_ProgressChanged(object? sender, ProgressChangedEventArgs e)
	{
		progressChangedDelegate del = new(doChangeProgress);
		Invoke(del, [e.ProgressPercentage, e.UserState?.ToString() ?? ""]);
	}

	private void doChangeProgress(int value, string message)
	{
		toolStripProgressBar1.Value = Math.Min(toolStripProgressBar1.Maximum, value);
		toolStripStatusLabel1.Text = message;
	}

	#endregion
}





