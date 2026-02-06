#nullable enable
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace Shape2SqlServer;

public partial class frmConnectionDialog : Form
{
	private Label lblServer = null!;
	private TextBox txtServer = null!;
	private Label lblDatabase = null!;
	private TextBox txtDatabase = null!;
	private GroupBox grpAuthentication = null!;
	private RadioButton rbWindowsAuth = null!;
	private RadioButton rbSqlAuth = null!;
	private Label lblUsername = null!;
	private TextBox txtUsername = null!;
	private Label lblPassword = null!;
	private TextBox txtPassword = null!;
	private Button btnTest = null!;
	private Label lblStatus = null!;
	private CheckBox chkTrustServerCertificate = null!;
	private Button btnOK = null!;
	private Button btnCancel = null!;

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public string ConnectionString { get; set; } = "";

	public frmConnectionDialog()
	{
		InitializeComponent();
		LoadSettings();
		LoadConnectionString();
	}

	private void InitializeComponent()
	{
		lblServer = new();
		txtServer = new();
		lblDatabase = new();
		txtDatabase = new();
		grpAuthentication = new();
		rbWindowsAuth = new();
		rbSqlAuth = new();
		lblUsername = new();
		txtUsername = new();
		lblPassword = new();
		txtPassword = new();
		btnTest = new();
		lblStatus = new();
		btnOK = new();
		chkTrustServerCertificate = new();
		btnCancel = new();
		grpAuthentication.SuspendLayout();
		SuspendLayout();
		//
		// lblServer
		//
		lblServer.AutoSize = true;
		lblServer.Location = new Point(20, 20);
		lblServer.Name = "lblServer";
		lblServer.Size = new Size(42, 15);
		lblServer.TabIndex = 0;
		lblServer.Text = "Server:";
		//
		// txtServer
		//
		txtServer.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		txtServer.Location = new Point(120, 17);
		txtServer.Name = "txtServer";
		txtServer.Size = new Size(285, 23);
		txtServer.TabIndex = 1;
            // 
            // lblDatabase
            // 
            lblDatabase.AutoSize = true;
            lblDatabase.Location = new Point(20, 50);
            lblDatabase.Name = "lblDatabase";
            lblDatabase.Size = new Size(58, 15);
            lblDatabase.TabIndex = 2;
            lblDatabase.Text = "Database:";
            // 
            // txtDatabase
            // 
            txtDatabase.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtDatabase.Location = new Point(120, 47);
            txtDatabase.Name = "txtDatabase";
            txtDatabase.Size = new Size(285, 23);
            txtDatabase.TabIndex = 3;
            // 
            // grpAuthentication
            // 
            grpAuthentication.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            grpAuthentication.Controls.Add(rbWindowsAuth);
            grpAuthentication.Controls.Add(rbSqlAuth);
            grpAuthentication.Controls.Add(lblUsername);
            grpAuthentication.Controls.Add(txtUsername);
            grpAuthentication.Controls.Add(lblPassword);
            grpAuthentication.Controls.Add(txtPassword);
            grpAuthentication.Location = new Point(20, 80);
            grpAuthentication.Name = "grpAuthentication";
            grpAuthentication.Size = new Size(385, 150);
            grpAuthentication.TabIndex = 4;
            grpAuthentication.TabStop = false;
            grpAuthentication.Text = "Authentication";
            // 
            // rbWindowsAuth
            // 
            rbWindowsAuth.Checked = true;
            rbWindowsAuth.Location = new Point(10, 25);
            rbWindowsAuth.Name = "rbWindowsAuth";
            rbWindowsAuth.Size = new Size(200, 24);
            rbWindowsAuth.TabIndex = 0;
            rbWindowsAuth.TabStop = true;
            rbWindowsAuth.Text = "Windows Authentication";
            rbWindowsAuth.CheckedChanged += rbWindowsAuth_CheckedChanged;
            // 
            // rbSqlAuth
            // 
            rbSqlAuth.Location = new Point(10, 50);
            rbSqlAuth.Name = "rbSqlAuth";
            rbSqlAuth.Size = new Size(200, 24);
            rbSqlAuth.TabIndex = 1;
            rbSqlAuth.Text = "SQL Server Authentication";
            rbSqlAuth.CheckedChanged += rbSqlAuth_CheckedChanged;
            // 
            // lblUsername
            // 
            lblUsername.AutoSize = true;
            lblUsername.Enabled = false;
            lblUsername.Location = new Point(30, 80);
            lblUsername.Name = "lblUsername";
            lblUsername.Size = new Size(63, 15);
            lblUsername.TabIndex = 2;
            lblUsername.Text = "Username:";
            // 
            // txtUsername
            // 
            txtUsername.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtUsername.Enabled = false;
            txtUsername.Location = new Point(100, 77);
            txtUsername.Name = "txtUsername";
            txtUsername.Size = new Size(265, 23);
            txtUsername.TabIndex = 3;
            // 
            // lblPassword
            // 
            lblPassword.AutoSize = true;
            lblPassword.Enabled = false;
            lblPassword.Location = new Point(30, 110);
            lblPassword.Name = "lblPassword";
            lblPassword.Size = new Size(60, 15);
            lblPassword.TabIndex = 4;
            lblPassword.Text = "Password:";
            // 
            // txtPassword
            // 
            txtPassword.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtPassword.Enabled = false;
            txtPassword.Location = new Point(100, 107);
            txtPassword.Name = "txtPassword";
            txtPassword.PasswordChar = '*';
            txtPassword.Size = new Size(265, 23);
            txtPassword.TabIndex = 5;
            // 
            // btnTest
            // 
            btnTest.Location = new Point(20, 240);
            btnTest.Name = "btnTest";
            btnTest.Size = new Size(120, 23);
            btnTest.TabIndex = 5;
            btnTest.Text = "Test Connection";
            btnTest.Click += btnTest_Click;
            // 
            // lblStatus
            // 
            lblStatus.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lblStatus.Location = new Point(150, 243);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(255, 113);
            lblStatus.TabIndex = 6;
            // 
            // chkTrustServerCertificate
            // 
            chkTrustServerCertificate.AutoSize = true;
            chkTrustServerCertificate.Checked = true;
            chkTrustServerCertificate.Location = new Point(20, 270);
            chkTrustServerCertificate.Name = "chkTrustServerCertificate";
            chkTrustServerCertificate.Size = new Size(200, 19);
            chkTrustServerCertificate.TabIndex = 9;
            chkTrustServerCertificate.Text = "Trust Server Certificate";
            // 
            // btnOK
            // 
            btnOK.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnOK.DialogResult = DialogResult.OK;
            btnOK.Location = new Point(235, 374);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(80, 23);
            btnOK.TabIndex = 7;
            btnOK.Text = "OK";
            btnOK.Click += btnOK_Click;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(325, 374);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(80, 23);
            btnCancel.TabIndex = 8;
            btnCancel.Text = "Cancel";
            // 
            // frmConnectionDialog
            // 
            AcceptButton = btnOK;
            CancelButton = btnCancel;
            ClientSize = new Size(429, 415);
            Controls.Add(lblServer);
            Controls.Add(txtServer);
            Controls.Add(lblDatabase);
            Controls.Add(txtDatabase);
            Controls.Add(grpAuthentication);
            Controls.Add(btnTest);
            Controls.Add(lblStatus);
            Controls.Add(chkTrustServerCertificate);
            Controls.Add(btnOK);
            Controls.Add(btnCancel);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "frmConnectionDialog";
            StartPosition = FormStartPosition.CenterParent;
            Text = "SQL Server Connection";
            grpAuthentication.ResumeLayout(false);
            grpAuthentication.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

	private void LoadConnectionString()
	{
		if (!string.IsNullOrEmpty(ConnectionString))
		{
			try
			{
				SqlConnectionStringBuilder builder = new(ConnectionString);
				txtServer.Text = builder.DataSource ?? "";
				txtDatabase.Text = builder.InitialCatalog ?? "";
				rbWindowsAuth.Checked = builder.IntegratedSecurity;
				rbSqlAuth.Checked = !builder.IntegratedSecurity;
				txtUsername.Text = builder.UserID ?? "";
				txtPassword.Text = builder.Password ?? "";
				chkTrustServerCertificate.Checked = builder.TrustServerCertificate;
			}
			catch
			{
				// If connection string is invalid, just leave fields empty
			}
		}
	}

	private void LoadSettings()
	{
		// Load persisted settings
		if (!string.IsNullOrEmpty(Properties.Settings.Default.sqlServer))
		{
			txtServer.Text = Properties.Settings.Default.sqlServer;
		}
		if (!string.IsNullOrEmpty(Properties.Settings.Default.sqlDatabase))
		{
			txtDatabase.Text = Properties.Settings.Default.sqlDatabase;
		}
		rbWindowsAuth.Checked = Properties.Settings.Default.sqlUseWindowsAuth;
		rbSqlAuth.Checked = !Properties.Settings.Default.sqlUseWindowsAuth;
		if (!string.IsNullOrEmpty(Properties.Settings.Default.sqlUsername))
		{
			txtUsername.Text = Properties.Settings.Default.sqlUsername;
		}
		chkTrustServerCertificate.Checked = Properties.Settings.Default.sqlTrustServerCertificate;
	}

	private void SaveSettings()
	{
		// Save settings for next time
		Properties.Settings.Default.sqlServer = txtServer.Text;
		Properties.Settings.Default.sqlDatabase = txtDatabase.Text;
		Properties.Settings.Default.sqlUseWindowsAuth = rbWindowsAuth.Checked;
		Properties.Settings.Default.sqlUsername = txtUsername.Text;
		Properties.Settings.Default.sqlTrustServerCertificate = chkTrustServerCertificate.Checked;
		Properties.Settings.Default.Save();
	}

	private void rbWindowsAuth_CheckedChanged(object? sender, EventArgs e)
	{
		bool sqlAuth = !rbWindowsAuth.Checked;
		lblUsername.Enabled = sqlAuth;
		txtUsername.Enabled = sqlAuth;
		lblPassword.Enabled = sqlAuth;
		txtPassword.Enabled = sqlAuth;
	}

	private void rbSqlAuth_CheckedChanged(object? sender, EventArgs e)
	{
		bool sqlAuth = rbSqlAuth.Checked;
		lblUsername.Enabled = sqlAuth;
		txtUsername.Enabled = sqlAuth;
		lblPassword.Enabled = sqlAuth;
		txtPassword.Enabled = sqlAuth;
	}

	private void btnTest_Click(object? sender, EventArgs e)
	{
		try
		{
			lblStatus.Text = "Testing connection...";
			lblStatus.ForeColor = Color.Black;
			Application.DoEvents();

			using SqlConnection conn = new(BuildConnectionString());
			conn.Open();
			lblStatus.Text = "Connection successful!";
			lblStatus.ForeColor = Color.Green;
		}
		catch (Exception ex)
		{
			lblStatus.Text = $"Connection failed: {ex.Message}";
			lblStatus.ForeColor = Color.Red;
		}
	}

	private void btnOK_Click(object? sender, EventArgs e)
	{
		SaveSettings();
		ConnectionString = BuildConnectionString();
	}

	private string BuildConnectionString()
	{
		SqlConnectionStringBuilder builder = new()
		{
			DataSource = txtServer.Text,
			InitialCatalog = txtDatabase.Text,
			IntegratedSecurity = rbWindowsAuth.Checked,
			TrustServerCertificate = chkTrustServerCertificate.Checked
		};

		if (rbSqlAuth.Checked)
		{
			builder.UserID = txtUsername.Text;
			builder.Password = txtPassword.Text;
		}

		return builder.ConnectionString;
	}
}
