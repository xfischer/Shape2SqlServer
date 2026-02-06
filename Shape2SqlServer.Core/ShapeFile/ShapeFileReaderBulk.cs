#nullable enable
using System;
using System.Collections;
using System.Data;
using Microsoft.SqlServer.Types;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using ProjNet.CoordinateSystems.Transformations;

namespace Shape2SqlServer.Core;

internal class ShapeFileReaderBulk : IEnumerable, IDataReader, IDataRecord
{
    private readonly ShapefileDataReader _shapeFileDataReader;
    private readonly ICoordinateTransformation? _coordTransform;
    private readonly EventHandler<ShapeImportExceptionEventArgs>? _errorHandler;
    private readonly enSpatialType _spatialType;
    private readonly int _srid;
    private int _curRowIndex;

    public ShapeFileReaderBulk(string shapeFile, ICoordinateTransformation? coordTransform, enSpatialType spatialType, int SRID, EventHandler<ShapeImportExceptionEventArgs>? errorHandler)
    {
        _coordTransform = coordTransform;
        _spatialType = spatialType;
        _srid = SRID;
        _errorHandler = errorHandler;
        _curRowIndex = -1;

        _shapeFileDataReader = new(shapeFile, GeometryFactory.Default);
    }

    public Geometry? Geometry => !_shapeFileDataReader.IsClosed ? _shapeFileDataReader.Geometry : null;

    /// <summary>
    /// Gets the header for the Dbase file.
    /// </summary>
    public DbaseFileHeader DbaseHeader => _shapeFileDataReader.DbaseHeader;

    #region IDataRecord GetValue
    public object GetValue(int i)
    {
        object? v_ret = null;

        if (i >= _shapeFileDataReader.FieldCount)
        {
            try
            {
                // Get shape and reproject if necessary
                Geometry geom = _shapeFileDataReader.Geometry;
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
            v_ret = _shapeFileDataReader.GetValue(i);
        }

        return v_ret;
    }
    #endregion

    #region base DataReader implementation

    #region IEnumerable Membres

    public IEnumerator GetEnumerator() => _shapeFileDataReader.GetEnumerator();

    #endregion

    #region IDataReader Membres

    public void Close() => _shapeFileDataReader.Close();

    public int Depth => _shapeFileDataReader.Depth;

    public DataTable? GetSchemaTable() => _shapeFileDataReader.GetSchemaTable();

    public bool IsClosed => _shapeFileDataReader.IsClosed;

    public bool NextResult() => _shapeFileDataReader.NextResult();

    public bool Read()
    {
        if (_shapeFileDataReader.Read())
        {
            _curRowIndex++;
            return true;
        }
        else
            return false;
    }

    public int RecordsAffected => _shapeFileDataReader.RecordsAffected;

    #endregion

    #region IDisposable Membres

    public void Dispose() => _shapeFileDataReader.Dispose();

    #endregion

    #region IDataRecord Membres

    public int FieldCount => _shapeFileDataReader.FieldCount;

    public bool GetBoolean(int i) => _shapeFileDataReader.GetBoolean(i);

    public byte GetByte(int i) => _shapeFileDataReader.GetByte(i);

    public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) =>
        _shapeFileDataReader.GetBytes(i, fieldOffset, buffer, bufferoffset, length);

    public char GetChar(int i) => _shapeFileDataReader.GetChar(i);

    public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) =>
        _shapeFileDataReader.GetChars(i, fieldoffset, buffer, bufferoffset, length);

    public IDataReader GetData(int i) => _shapeFileDataReader.GetData(i);

    public string GetDataTypeName(int i) => _shapeFileDataReader.GetDataTypeName(i);

    public DateTime GetDateTime(int i) => _shapeFileDataReader.GetDateTime(i);

    public decimal GetDecimal(int i) => _shapeFileDataReader.GetDecimal(i);

    public double GetDouble(int i) => _shapeFileDataReader.GetDouble(i);

    public Type GetFieldType(int i)
    {
        Type? ret;

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

    public float GetFloat(int i) => _shapeFileDataReader.GetFloat(i);

    public Guid GetGuid(int i) => _shapeFileDataReader.GetGuid(i);

    public short GetInt16(int i) => _shapeFileDataReader.GetInt16(i);

    public int GetInt32(int i) => _shapeFileDataReader.GetInt32(i);

    public long GetInt64(int i) => _shapeFileDataReader.GetInt64(i);

    public string GetName(int i) => _shapeFileDataReader.GetName(i);

    public int GetOrdinal(string name)
    {
        if (name == "geom")
        {
            return _shapeFileDataReader.FieldCount;
        }
        else if (name == "geom_geom")
        {
            return _shapeFileDataReader.FieldCount;
        }
        else if (name == "geom_geog")
        {
            return _shapeFileDataReader.FieldCount + 1;
        }
        else
            return _shapeFileDataReader.GetOrdinal(name);
    }

    public string GetString(int i) => _shapeFileDataReader.GetString(i);

    public int GetValues(object?[] values) => _shapeFileDataReader.GetValues(values);

    public bool IsDBNull(int i) => _shapeFileDataReader.IsDBNull(i);

    public object this[string name] => _shapeFileDataReader[name];

    public object this[int i] => _shapeFileDataReader[i];

    #endregion

    #endregion
}
