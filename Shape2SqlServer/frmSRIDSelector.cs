#nullable enable
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using Shape2SqlServer.Core;

namespace Shape2SqlServer;

public partial class frmSRIDSelector : Form
{
	public frmSRIDSelector()
	{
		InitializeComponent();

		BindingList<SRIDReader.WKTstring> cs = new(SRIDReader.GetSRIDs().ToList());
		cmbSRID.DataSource = cs;
	}

	public string? SelectedWKT =>
		cmbSRID.SelectedValue is SRIDReader.WKTstring wkt ? wkt.WKT : null;

	private void txtSRIDFind_TextChanged(object? sender, EventArgs e)
	{
		var query = from cs in SRIDReader.GetSRIDs()
					where cs.WKT.ToUpper().Contains(txtSRIDFind.Text.ToUpper())
					select cs;
		BindingList<SRIDReader.WKTstring> csList = new(query.ToList());
		cmbSRID.DataSource = csList;
	}

	private void btnOK_Click(object? sender, EventArgs e) => Close();
}
