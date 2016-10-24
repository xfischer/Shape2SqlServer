using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MapBind.IO.CoordinateSystem;

namespace MapBind.Shape2SqlServer
{
	public partial class frmSRIDSelector : Form
	{
		public frmSRIDSelector()
		{
			InitializeComponent();

			BindingList<SRIDReader.WKTstring> cs = new BindingList<SRIDReader.WKTstring>(SRIDReader.GetSRIDs().ToList());
			cmbSRID.DataSource = cs;
		}

		public string SelectedWKT
		{
			get {
				if (cmbSRID.SelectedValue == null)
					return null;
				else
					return ((SRIDReader.WKTstring)cmbSRID.SelectedValue).WKT;
			}
		}
	

		private void txtSRIDFind_TextChanged(object sender, EventArgs e)
		{
			var query = from cs in SRIDReader.GetSRIDs()
									where cs.WKT.ToUpper().Contains(txtSRIDFind.Text.ToUpper())
									select cs;
			BindingList<SRIDReader.WKTstring> csList = new BindingList<SRIDReader.WKTstring>(query.ToList());
			cmbSRID.DataSource = csList;
		}

		private void btnOK_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		
	}
}
