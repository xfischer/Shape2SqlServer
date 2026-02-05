using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Shape2SqlServer
{
	static class Program
	{
		/// <summary>
		/// Point d'entrée principal de l'application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			// Modern Microsoft.SqlServer.Types package (v170+) handles native assembly loading automatically
			// SqlServerTypes.Utilities.LoadNativeAssemblies(AppDomain.CurrentDomain.BaseDirectory);
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new frmMain());
		}
	}
}
