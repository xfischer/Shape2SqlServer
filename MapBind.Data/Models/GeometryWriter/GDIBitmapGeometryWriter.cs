using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using MapBind.Data.Models.Style;
using System.Diagnostics;
using Microsoft.SqlServer.Types;

namespace MapBind.Data.Models.GeometryWriter
{
	internal sealed class GDIBitmapGeometryWriter : GeometryWriterBase<Bitmap, Point>, IDisposable
	{

		#region Private Members

		private Bitmap _bmp;
		private Graphics _graphics;
		private GraphicsPath _gpFill, _gpStroke;
		private Brush _fillBrush;
		private Pen _strokePen;
		private HashSet<Point> _pixels;
		private int _width, _height;
		private LayerStyle _layerStyle;
		private SmoothingMode _smoothingMode;
		private CoordinateConverters.CoordinateConverterBase _coordConverter;


		private Metrics _metrics;
		#endregion

		#region Initialization

		#region Constructor
		public GDIBitmapGeometryWriter(SmoothingMode smoothingMode, CoordinateConverters.CoordinateConverterBase coordConverter)
			: base()
		{
			_pixels = new HashSet<Point>();
			_smoothingMode = smoothingMode;
			_coordConverter = coordConverter;
		}

		public void Init(int outputWidth, int outputHeight, Style.LayerStyle layerStyle, Metrics metrics)
		{
			_metrics = metrics;
			_width = outputWidth;
			_height = outputHeight;
			_layerStyle = layerStyle;


			_bmp = new Bitmap(_width, _height);
			if (_layerStyle == null)
				_layerStyle = new LayerStyle(Color.OrangeRed, Color.White, 1);

			_graphics = Graphics.FromImage(_bmp);
			_graphics.Clip = new Region(new Rectangle(0, 0, _width, _height));
			_graphics.SmoothingMode = _smoothingMode; // existe aussi HighSpeed ou HighQuality


			_gpFill = new GraphicsPath();
			_gpStroke = new GraphicsPath();
			_fillBrush = new SolidBrush(_layerStyle.FillColor);
			_strokePen = new Pen(_layerStyle.StrokeColor, _layerStyle.StrokeThickness);

		}


		#endregion

		#endregion

		#region GeometryWriterBase override

		public override void WritePolygon(SqlGeometry polygon)
		{
			try
			{

				SqlGeometry extRing = polygon.STExteriorRing();
				List<SqlGeometry> intRings = base.GetPolygonInteriorRings(polygon);

				switch (extRing.STNumPoints().Value)
				{
					case 0:
						break;
					case 1:
						this.WritePoint(extRing.STPointN(1));
						break;
					case 2:
						this.WriteLineString(extRing);
						break;
					default:

						//_gpFill.CloseFigure();
						//_gpStroke.CloseFigure();

						List<Point> extRingPoints = this.ConvertAndAccumulateDistinctPoints(extRing);

						switch (extRingPoints.Count)
						{
							case 0: break;
							case 1:
								this.WritePoint_Internal(extRingPoints[0].X, extRingPoints[0].Y);
								break;
							case 2:
								_gpStroke.AddLines(extRingPoints.ToArray());
								break;
							default:
								Point[] extRingPointArray = extRingPoints.ToArray();

								_gpFill.AddPolygon(extRingPointArray);
								_gpStroke.AddPolygon(extRingPointArray);
								break;
						}

						// Polygones intérieurs
						if (intRings != null)
						{

							foreach (SqlGeometry interiorRing in intRings)
							{
								List<Point> intRingPoints = this.ConvertAndAccumulateDistinctPoints(interiorRing);

								switch (intRingPoints.Count)
								{
									case 0: break;
									case 1:
										//bmp.SetPixel(extRingPointArray[0].X, extRingPointArray[0].Y, strokeColor.Color);
										//graphicsPath.AddLine(v_polyInterieurPoints[0], v_polyInterieurPoints[0]);
										break;
									case 2:
										_gpStroke.AddLine(intRingPoints[0], intRingPoints[1]);
										break;
									default:
										Point[] coords = intRingPoints.ToArray();
										_gpFill.AddPolygon(coords);
										_gpStroke.AddPolygon(coords);
										break;
								}
							}
						}

						//_gpFill.CloseFigure();
						//_gpStroke.CloseFigure();


						break;
				}

			}
			catch (Exception v_ex)
			{
				throw;
			}
		}

		public override void WriteMultiPolygon(Microsoft.SqlServer.Types.SqlGeometry geom)
		{
			try
			{
				for (int i = 1; i <= geom.STNumGeometries().Value; i++)
				{
					this.WritePolygon(geom.STGeometryN(i));
				}
			}
			catch (Exception)
			{
				throw;
			}
		}

		public override void WritePoint(Microsoft.SqlServer.Types.SqlGeometry geom)
		{
			double x, y;
			_coordConverter.TransformPoint(geom.STX.Value, geom.STY.Value, out x, out y);
			this.WritePoint_Internal(x, y);
		}

		public override void WriteMultiPoint(Microsoft.SqlServer.Types.SqlGeometry geom)
		{
			for (int i = 1; i <= geom.STNumPoints().Value; i++)
				this.WritePoint(geom.STPointN(i));
		}

		public override void WriteMultiLineString(Microsoft.SqlServer.Types.SqlGeometry geom)
		{
			for (int i = 1; i <= geom.STNumGeometries().Value; i++)
				this.WriteLineString(geom.STGeometryN(i));
		}

		public override void WriteLineString(Microsoft.SqlServer.Types.SqlGeometry geom)
		{
			_gpStroke.StartFigure();
			DrawLineString(this.ConvertAndAccumulateDistinctPoints(geom));
		}

		public override Point ConvertPoint(SqlGeometry point)
		{
			double x, y;
			_coordConverter.TransformPoint(point.STX.Value, point.STY.Value, out x, out y);
			return new Point((int)x, (int)y);
		}

		public override Bitmap GetOutput()
		{
			// Draw pixels
			Trace.WriteLine(string.Format("numPixels: {0}", _pixels.Count));
			foreach (Point pt in _pixels)
				_bmp.SetPixel(pt.X, pt.Y, _strokePen.Color);

			_graphics.FillPath(_fillBrush, _gpFill);
			_graphics.DrawPath(_strokePen, _gpStroke);

			// Check if image is empty
			if (Utils.IsPNGBitmapEmpty(_bmp))
				_bmp.Tag = false;

			return _bmp;
		}

		#endregion

		#region Helpers

		private List<Point> ConvertAndAccumulateDistinctPoints(SqlGeometry pointList)
		{
			List<Point> list = new List<Point>();

			double lastX = 0, lastY = 0;
			int numPoints = pointList.STNumPoints().Value;
			for (int i = 1; i <= numPoints; i++)
			{
				SqlGeometry point = pointList.STPointN(i);
				double x, y;
				_coordConverter.TransformPoint(point.STX.Value, point.STY.Value, out x, out y);
				if (i == 1 || !(lastX == x && lastY == y))
					list.Add(new Point((int)x, (int)y));

				lastX = x;
				lastY = y;
			}

			return list;

		}

		private void WritePoint_Internal(double x, double y)
		{
			// Check if point is in image
			if (x < 0
					|| y < 0
					|| x > _width - 1				
					|| y > _height - 1)
				return;

			_pixels.Add(new Point((int)x, (int)y));
		}


		#region DrawLineString
		private void DrawLineString(List<Point> linePoints)
		{
			try
			{
				if (linePoints.Count == 1)
				{
					Point pt = linePoints[0];
					this.WritePoint_Internal(pt.X, pt.Y);
				}
				else if (linePoints.Count > 0)
				{
					_gpStroke.AddLines(linePoints.ToArray());
				}
			}
			catch (Exception v_ex)
			{
				throw;
			}
		}
		#endregion

		#endregion

		#region IDisposable Membres

		public override void Dispose()
		{
			DisposeWithoutThrow(_strokePen);
			DisposeWithoutThrow(_fillBrush);
			DisposeWithoutThrow(_gpStroke);
			DisposeWithoutThrow(_gpFill);
			DisposeWithoutThrow(_graphics);
			//DisposeWithoutThrow(_bmp);
		}

		private void DisposeWithoutThrow(IDisposable obj)
		{
			try
			{
				if (obj != null)
					obj.Dispose();
			}
			finally
			{
				obj = null;
			}
		}

		#endregion


	}
}
