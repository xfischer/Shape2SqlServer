using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetTopologySuite.IO;
using NetTopologySuite.Geometries;
using GeoAPI.CoordinateSystems.Transformations;
using Microsoft.SqlServer.Types;
using GeoAPI.Geometries;
using System.Data;
using System.Collections;

namespace Shape2SqlServer.Core
{
	internal class ShapeFileReaderBulk : IEnumerable, IDataReader, IDataRecord
	{

		private ShapefileDataReader _shapeFileDataReader;

		private ICoordinateTransformation _coordTransform;
		private EventHandler<ShapeImportExceptionEventArgs> _errorHandler;
		private enSpatialType _spatialType;
		private int _srid;
		private int _curRowIndex;

		public ShapeFileReaderBulk(string shapeFile, ICoordinateTransformation coordTransform, enSpatialType spatialType, int SRID, EventHandler<ShapeImportExceptionEventArgs> errorHandler)
		{
			_coordTransform = coordTransform;
			_spatialType = spatialType;
			_srid = SRID;
			_errorHandler = errorHandler;
			_curRowIndex = -1;

			_shapeFileDataReader = new ShapefileDataReader(shapeFile, GeometryFactory.Default);
		}

		public IGeometry Geometry
		{
			get
			{
				if (_shapeFileDataReader != null && !_shapeFileDataReader.IsClosed)
					return _shapeFileDataReader.Geometry;
				else
					return null;
			}
		}

		#region IDataRecord GetValue
		public object GetValue(int i)
		{
			object v_ret = null;

			if (i >= _shapeFileDataReader.FieldCount)
			{
				try
				{

					// Get shape and reproject if necessary
					IGeometry geom = _shapeFileDataReader.Geometry;
					if (_coordTransform != null)
						geom = ShapeFileHelper.ReprojectGeometry(_shapeFileDataReader.Geometry, _coordTransform);


					// Convert to sqltype
					if (_spatialType == enSpatialType.both)
					{
						v_ret = SqlServerHelper.ConvertToSqlType(geom, _srid, i == _shapeFileDataReader.FieldCount + 1, _curRowIndex);
					}
					else
					{
						v_ret = SqlServerHelper.ConvertToSqlType(geom, _srid, _spatialType == enSpatialType.geography, _curRowIndex);
					}

				}
				catch (Exception ex)
				{
					if (_errorHandler != null)
					{
						var args = new ShapeImportExceptionEventArgs(ex, false, this.DumpCurrentRecord(), this.Geometry, _curRowIndex);
						_errorHandler(this, args);
						if (args.Ignore)
							v_ret = null;
					}
					else
						throw;
				}

			}
			else
			{
				//Type fieldType = _shapeFileDataReader.GetFieldType(i);
				v_ret = _shapeFileDataReader.GetValue(i);				
			}

			return v_ret;
		}
		#endregion


		/// <summary>
		/// Gets the header for the Dbase file.
		/// </summary>
		public DbaseFileHeader DbaseHeader
		{
			get
			{
				return _shapeFileDataReader.DbaseHeader;
			}
		}

		#region base DataReader implementation

		#region IEnumerable Membres

		public IEnumerator GetEnumerator()
		{
			return _shapeFileDataReader.GetEnumerator();
		}

		#endregion

		#region IDataReader Membres

		public void Close()
		{
			_shapeFileDataReader.Close();
		}

		public int Depth
		{
			get { return _shapeFileDataReader.Depth; }
		}

		public DataTable GetSchemaTable()
		{
			return _shapeFileDataReader.GetSchemaTable();
		}

		public bool IsClosed
		{
			get { return _shapeFileDataReader.IsClosed; }
		}

		public bool NextResult()
		{
			return _shapeFileDataReader.NextResult();
		}

		public bool Read()
		{
			if (_shapeFileDataReader.Read())
			{ _curRowIndex++; return true; }
			else return false;

		}

		public int RecordsAffected
		{
			get { return _shapeFileDataReader.RecordsAffected; }
		}

		#endregion

		#region IDisposable Membres

		public void Dispose()
		{
			_shapeFileDataReader.Dispose();
		}

		#endregion

		#region IDataRecord Membres

		public int FieldCount
		{
			get { return _shapeFileDataReader.FieldCount; }
		}

		public bool GetBoolean(int i)
		{
			return _shapeFileDataReader.GetBoolean(i);
		}

		public byte GetByte(int i)
		{
			return _shapeFileDataReader.GetByte(i);
		}

		public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
		{
			return _shapeFileDataReader.GetBytes(i, fieldOffset, buffer, bufferoffset, length);
		}

		public char GetChar(int i)
		{
			return _shapeFileDataReader.GetChar(i);
		}

		public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
		{
			return _shapeFileDataReader.GetChars(i, fieldoffset, buffer, bufferoffset, length);
		}

		public IDataReader GetData(int i)
		{
			return _shapeFileDataReader.GetData(i);
		}

		public string GetDataTypeName(int i)
		{
			return _shapeFileDataReader.GetDataTypeName(i);
		}

		public DateTime GetDateTime(int i)
		{
			return _shapeFileDataReader.GetDateTime(i);
		}

		public decimal GetDecimal(int i)
		{
			return _shapeFileDataReader.GetDecimal(i);
		}

		public double GetDouble(int i)
		{
			return _shapeFileDataReader.GetDouble(i);
		}

		public Type GetFieldType(int i)
		{
			Type ret = null;

			if (i >= _shapeFileDataReader.FieldCount)
			{
				if (_spatialType == enSpatialType.both)
				{
					ret = (i == _shapeFileDataReader.FieldCount + 1) ? typeof(SqlGeography) : typeof(SqlGeometry);
				}
				else
				{
					ret = _spatialType == enSpatialType.geography ? typeof(SqlGeography) : typeof(SqlGeometry);
				}
			}
			else
				ret = _shapeFileDataReader.GetFieldType(i);

			return ret;
		}

		public float GetFloat(int i)
		{
			return _shapeFileDataReader.GetFloat(i);
		}

		public Guid GetGuid(int i)
		{
			return _shapeFileDataReader.GetGuid(i);
		}

		public short GetInt16(int i)
		{
			return _shapeFileDataReader.GetInt16(i);
		}

		public int GetInt32(int i)
		{
			return _shapeFileDataReader.GetInt32(i);
		}

		public long GetInt64(int i)
		{
			return _shapeFileDataReader.GetInt64(i);
		}

		public string GetName(int i)
		{
			return _shapeFileDataReader.GetName(i);
		}

		public int GetOrdinal(string name)
		{
			if (name == "geom")
			{ return _shapeFileDataReader.FieldCount; }
			else if (name == "geom_geom")
			{ return _shapeFileDataReader.FieldCount; }
			else if (name == "geom_geog")
			{ return _shapeFileDataReader.FieldCount + 1; }
			else
				return _shapeFileDataReader.GetOrdinal(name);
		}

		public string GetString(int i)
		{
			return _shapeFileDataReader.GetString(i);
		}




		public int GetValues(object[] values)
		{
			return _shapeFileDataReader.GetValues(values);
		}

		public bool IsDBNull(int i)
		{
			return _shapeFileDataReader.IsDBNull(i);
		}

		public object this[string name]
		{
			get { return _shapeFileDataReader[name]; }
		}

		public object this[int i]
		{
			get { return _shapeFileDataReader[i]; }
		}

		#endregion

		#endregion
	}
}
