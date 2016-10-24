using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace MapBind.Data.Models.Style
{
	public sealed class LayerStyle
	{
		public Color FillColor { get; private set; }
		public Color StrokeColor { get; private set; }
		public float StrokeThickness { get; private set; }

		public LayerStyle(Color fillColor, Color strokeColor, float strokeThickness)
		{
			this.FillColor = fillColor;
			this.StrokeColor = strokeColor;
			this.StrokeThickness = strokeThickness;
		}
	}
}
