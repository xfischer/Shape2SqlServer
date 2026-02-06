#nullable disable

namespace Shape2SqlServer;

partial class frmMain
{
	/// <summary>
	/// Variable nécessaire au concepteur.
	/// </summary>
	private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Nettoyage des ressources utilisées.
		/// </summary>
		/// <param name="disposing">true si les ressources managées doivent être supprimées ; sinon, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Code généré par le Concepteur Windows Form

		/// <summary>
		/// Méthode requise pour la prise en charge du concepteur - ne modifiez pas
		/// le contenu de cette méthode avec l'éditeur de code.
		/// </summary>
		private void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            this.btnImport = new System.Windows.Forms.Button();
            this.txtSHP = new System.Windows.Forms.TextBox();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.dlgOpen = new System.Windows.Forms.OpenFileDialog();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.btnConString = new System.Windows.Forms.Button();
            this.txtConString = new System.Windows.Forms.TextBox();
            this.lblShapeHeader = new System.Windows.Forms.Label();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripProgressBar1 = new System.Windows.Forms.ToolStripProgressBar();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.lstColumns = new System.Windows.Forms.CheckedListBox();
            this.txtIDCol = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.txtGeomCol = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.radBoth = new System.Windows.Forms.RadioButton();
            this.lnkCSSelector = new System.Windows.Forms.LinkLabel();
            this.txtSrid = new System.Windows.Forms.TextBox();
            this.chkSRID = new System.Windows.Forms.CheckBox();
            this.txtCoordSys = new System.Windows.Forms.TextBox();
            this.radGeog = new System.Windows.Forms.RadioButton();
            this.radGeom = new System.Windows.Forms.RadioButton();
            this.chkReproject = new System.Windows.Forms.CheckBox();
            this.chkDrop = new System.Windows.Forms.CheckBox();
            this.txtTableName = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.btnCancel = new System.Windows.Forms.Button();
            this.chkSafeMode = new System.Windows.Forms.CheckBox();
            this.txtSchema = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.statusStrip1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnImport
            // 
            this.btnImport.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnImport.Location = new System.Drawing.Point(12, 381);
            this.btnImport.Name = "btnImport";
            this.btnImport.Size = new System.Drawing.Size(127, 23);
            this.btnImport.TabIndex = 0;
            this.btnImport.Text = "Import to database";
            this.btnImport.UseVisualStyleBackColor = true;
            this.btnImport.Click += new System.EventHandler(this.btnImport_Click);
            // 
            // txtSHP
            // 
            this.txtSHP.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSHP.Location = new System.Drawing.Point(107, 12);
            this.txtSHP.Name = "txtSHP";
            this.txtSHP.Size = new System.Drawing.Size(332, 20);
            this.txtSHP.TabIndex = 1;
            // 
            // btnBrowse
            // 
            this.btnBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowse.Location = new System.Drawing.Point(445, 10);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(30, 23);
            this.btnBrowse.TabIndex = 2;
            this.btnBrowse.Text = "...";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // dlgOpen
            // 
            this.dlgOpen.FileName = "openFileDialog1";
            this.dlgOpen.Filter = "Shape Files (*.shp)|*.shp";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(57, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Shape File";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(9, 71);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(62, 13);
            this.label2.TabIndex = 6;
            this.label2.Text = "SQL Server";
            // 
            // btnConString
            // 
            this.btnConString.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnConString.Location = new System.Drawing.Point(445, 66);
            this.btnConString.Name = "btnConString";
            this.btnConString.Size = new System.Drawing.Size(30, 23);
            this.btnConString.TabIndex = 5;
            this.btnConString.Text = "...";
            this.btnConString.UseVisualStyleBackColor = true;
            this.btnConString.Click += new System.EventHandler(this.btnConString_Click);
            // 
            // txtConString
            // 
            this.txtConString.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtConString.Location = new System.Drawing.Point(107, 68);
            this.txtConString.Name = "txtConString";
            this.txtConString.Size = new System.Drawing.Size(332, 20);
            this.txtConString.TabIndex = 4;
            // 
            // lblShapeHeader
            // 
            this.lblShapeHeader.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblShapeHeader.Location = new System.Drawing.Point(107, 35);
            this.lblShapeHeader.Name = "lblShapeHeader";
            this.lblShapeHeader.Size = new System.Drawing.Size(332, 30);
            this.lblShapeHeader.TabIndex = 11;
            this.lblShapeHeader.Click += new System.EventHandler(this.lblShapeHeader_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.toolStripProgressBar1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 417);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(487, 22);
            this.statusStrip1.TabIndex = 12;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(39, 17);
            this.toolStripStatusLabel1.Text = "Ready";
            this.toolStripStatusLabel1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // toolStripProgressBar1
            // 
            this.toolStripProgressBar1.Name = "toolStripProgressBar1";
            this.toolStripProgressBar1.Size = new System.Drawing.Size(200, 16);
            this.toolStripProgressBar1.Visible = false;
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.lstColumns);
            this.groupBox1.Controls.Add(this.txtIDCol);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.txtGeomCol);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Location = new System.Drawing.Point(281, 105);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(194, 270);
            this.groupBox1.TabIndex = 13;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Attributes";
            // 
            // lstColumns
            // 
            this.lstColumns.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lstColumns.CheckOnClick = true;
            this.lstColumns.Location = new System.Drawing.Point(17, 83);
            this.lstColumns.Name = "lstColumns";
            this.lstColumns.Size = new System.Drawing.Size(163, 139);
            this.lstColumns.TabIndex = 5;
            // 
            // txtIDCol
            // 
            this.txtIDCol.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtIDCol.Location = new System.Drawing.Point(143, 50);
            this.txtIDCol.Name = "txtIDCol";
            this.txtIDCol.Size = new System.Drawing.Size(35, 20);
            this.txtIDCol.TabIndex = 4;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(17, 53);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(84, 13);
            this.label6.TabIndex = 3;
            this.label6.Text = "ID column name";
            // 
            // txtGeomCol
            // 
            this.txtGeomCol.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtGeomCol.Location = new System.Drawing.Point(143, 23);
            this.txtGeomCol.Name = "txtGeomCol";
            this.txtGeomCol.Size = new System.Drawing.Size(35, 20);
            this.txtGeomCol.TabIndex = 2;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(19, 26);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(118, 13);
            this.label5.TabIndex = 0;
            this.label5.Text = "Geometry column name";
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.groupBox2.Controls.Add(this.txtSchema);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.radBoth);
            this.groupBox2.Controls.Add(this.lnkCSSelector);
            this.groupBox2.Controls.Add(this.txtSrid);
            this.groupBox2.Controls.Add(this.chkSRID);
            this.groupBox2.Controls.Add(this.txtCoordSys);
            this.groupBox2.Controls.Add(this.radGeog);
            this.groupBox2.Controls.Add(this.radGeom);
            this.groupBox2.Controls.Add(this.chkReproject);
            this.groupBox2.Controls.Add(this.chkDrop);
            this.groupBox2.Controls.Add(this.txtTableName);
            this.groupBox2.Controls.Add(this.label7);
            this.groupBox2.Location = new System.Drawing.Point(12, 105);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(263, 270);
            this.groupBox2.TabIndex = 14;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Import settings";
            // 
            // radBoth
            // 
            this.radBoth.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.radBoth.AutoSize = true;
            this.radBoth.Location = new System.Drawing.Point(15, 124);
            this.radBoth.Name = "radBoth";
            this.radBoth.Size = new System.Drawing.Size(68, 17);
            this.radBoth.TabIndex = 17;
            this.radBoth.Text = "Use both";
            this.radBoth.UseVisualStyleBackColor = true;
            // 
            // lnkCSSelector
            // 
            this.lnkCSSelector.AutoSize = true;
            this.lnkCSSelector.Location = new System.Drawing.Point(220, 53);
            this.lnkCSSelector.Name = "lnkCSSelector";
            this.lnkCSSelector.Size = new System.Drawing.Size(36, 13);
            this.lnkCSSelector.TabIndex = 16;
            this.lnkCSSelector.TabStop = true;
            this.lnkCSSelector.Text = "Find...";
            this.lnkCSSelector.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkCSSelector_LinkClicked);
            // 
            // txtSrid
            // 
            this.txtSrid.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.txtSrid.Enabled = false;
            this.txtSrid.Location = new System.Drawing.Point(95, 151);
            this.txtSrid.Name = "txtSrid";
            this.txtSrid.Size = new System.Drawing.Size(46, 20);
            this.txtSrid.TabIndex = 15;
            this.txtSrid.Text = "4326";
            // 
            // chkSRID
            // 
            this.chkSRID.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chkSRID.AutoSize = true;
            this.chkSRID.Location = new System.Drawing.Point(15, 154);
            this.chkSRID.Name = "chkSRID";
            this.chkSRID.Size = new System.Drawing.Size(71, 17);
            this.chkSRID.TabIndex = 14;
            this.chkSRID.Text = "Set SRID";
            this.chkSRID.UseVisualStyleBackColor = true;
            this.chkSRID.CheckedChanged += new System.EventHandler(this.chkSRID_CheckedChanged);
            // 
            // txtCoordSys
            // 
            this.txtCoordSys.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtCoordSys.Enabled = false;
            this.txtCoordSys.Location = new System.Drawing.Point(29, 75);
            this.txtCoordSys.Multiline = true;
            this.txtCoordSys.Name = "txtCoordSys";
            this.txtCoordSys.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtCoordSys.Size = new System.Drawing.Size(227, 0);
            this.txtCoordSys.TabIndex = 11;
            this.txtCoordSys.Text = resources.GetString("txtCoordSys.Text");
            // 
            // radGeog
            // 
            this.radGeog.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.radGeog.AutoSize = true;
            this.radGeog.Checked = true;
            this.radGeog.Location = new System.Drawing.Point(15, 101);
            this.radGeog.Name = "radGeog";
            this.radGeog.Size = new System.Drawing.Size(97, 17);
            this.radGeog.TabIndex = 10;
            this.radGeog.TabStop = true;
            this.radGeog.Text = "Use geography";
            this.radGeog.UseVisualStyleBackColor = true;
            // 
            // radGeom
            // 
            this.radGeom.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.radGeom.AutoSize = true;
            this.radGeom.Location = new System.Drawing.Point(15, 78);
            this.radGeom.Name = "radGeom";
            this.radGeom.Size = new System.Drawing.Size(90, 17);
            this.radGeom.TabIndex = 9;
            this.radGeom.Text = "Use geometry";
            this.radGeom.UseVisualStyleBackColor = true;
            // 
            // chkReproject
            // 
            this.chkReproject.AutoSize = true;
            this.chkReproject.Location = new System.Drawing.Point(15, 52);
            this.chkReproject.Name = "chkReproject";
            this.chkReproject.Size = new System.Drawing.Size(175, 17);
            this.chkReproject.TabIndex = 4;
            this.chkReproject.Text = "Reproject on coordinate system";
            this.chkReproject.UseVisualStyleBackColor = true;
            this.chkReproject.CheckedChanged += new System.EventHandler(this.chkReproject_CheckedChanged);
            // 
            // chkDrop
            // 
            this.chkDrop.AutoSize = true;
            this.chkDrop.Checked = true;
            this.chkDrop.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkDrop.Location = new System.Drawing.Point(15, 26);
            this.chkDrop.Name = "chkDrop";
            this.chkDrop.Size = new System.Drawing.Size(112, 17);
            this.chkDrop.TabIndex = 3;
            this.chkDrop.Text = "Drop table if exists";
            this.chkDrop.UseVisualStyleBackColor = true;
            // 
            // txtTableName
            // 
            this.txtTableName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtTableName.Location = new System.Drawing.Point(95, 232);
            this.txtTableName.Name = "txtTableName";
            this.txtTableName.Size = new System.Drawing.Size(161, 20);
            this.txtTableName.TabIndex = 2;
            // 
            // label7
            // 
            this.label7.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(12, 235);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(63, 13);
            this.label7.TabIndex = 0;
            this.label7.Text = "Table name";
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnCancel.Location = new System.Drawing.Point(12, 381);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(127, 23);
            this.btnCancel.TabIndex = 15;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Visible = false;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // chkSafeMode
            // 
            this.chkSafeMode.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chkSafeMode.AutoSize = true;
            this.chkSafeMode.Location = new System.Drawing.Point(145, 385);
            this.chkSafeMode.Name = "chkSafeMode";
            this.chkSafeMode.Size = new System.Drawing.Size(77, 17);
            this.chkSafeMode.TabIndex = 16;
            this.chkSafeMode.Text = "Safe mode";
            this.chkSafeMode.UseVisualStyleBackColor = true;
            // 
            // txtSchema
            // 
            this.txtSchema.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSchema.Location = new System.Drawing.Point(95, 204);
            this.txtSchema.Name = "txtSchema";
            this.txtSchema.Size = new System.Drawing.Size(161, 20);
            this.txtSchema.TabIndex = 19;
            this.txtSchema.Text = "dbo";
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 207);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(46, 13);
            this.label3.TabIndex = 18;
            this.label3.Text = "Schema";
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(487, 439);
            this.Controls.Add(this.chkSafeMode);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.lblShapeHeader);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btnConString);
            this.Controls.Add(this.txtConString);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnBrowse);
            this.Controls.Add(this.txtSHP);
            this.Controls.Add(this.btnImport);
            this.Controls.Add(this.btnCancel);
            this.MinimumSize = new System.Drawing.Size(495, 478);
            this.Name = "frmMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "ShapeFile 2 SQL";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.frmMain_FormClosed);
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

	private System.Windows.Forms.Button btnImport = null!;
	private System.Windows.Forms.TextBox txtSHP = null!;
	private System.Windows.Forms.Button btnBrowse = null!;
	private System.Windows.Forms.OpenFileDialog dlgOpen = null!;
	private System.Windows.Forms.Label label1 = null!;
	private System.Windows.Forms.Label label2 = null!;
	private System.Windows.Forms.Button btnConString = null!;
	private System.Windows.Forms.TextBox txtConString = null!;
	private System.Windows.Forms.Label lblShapeHeader = null!;
	private System.Windows.Forms.StatusStrip statusStrip1 = null!;
	private System.Windows.Forms.GroupBox groupBox1 = null!;
	private System.Windows.Forms.Label label5 = null!;
	private System.Windows.Forms.TextBox txtGeomCol = null!;
	private System.Windows.Forms.TextBox txtIDCol = null!;
	private System.Windows.Forms.Label label6 = null!;
	private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1 = null!;
	private System.Windows.Forms.ToolStripProgressBar toolStripProgressBar1 = null!;
	private System.Windows.Forms.CheckedListBox lstColumns = null!;
	private System.Windows.Forms.GroupBox groupBox2 = null!;
	private System.Windows.Forms.TextBox txtTableName = null!;
	private System.Windows.Forms.Label label7 = null!;
	private System.Windows.Forms.CheckBox chkReproject = null!;
	private System.Windows.Forms.CheckBox chkDrop = null!;
	private System.Windows.Forms.TextBox txtCoordSys = null!;
	private System.Windows.Forms.TextBox txtSrid = null!;
	private System.Windows.Forms.CheckBox chkSRID = null!;
	private System.Windows.Forms.RadioButton radGeog = null!;
	private System.Windows.Forms.RadioButton radGeom = null!;
	private System.Windows.Forms.Button btnCancel = null!;
	private System.Windows.Forms.LinkLabel lnkCSSelector = null!;
	private System.Windows.Forms.CheckBox chkSafeMode = null!;
	private System.Windows.Forms.RadioButton radBoth = null!;
	private System.Windows.Forms.TextBox txtSchema = null!;
	private System.Windows.Forms.Label label3 = null!;
}

