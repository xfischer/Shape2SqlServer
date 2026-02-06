#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using Microsoft.Data.SqlClient;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using ProjNet;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using System.ComponentModel.Design;

namespace Shape2SqlServer.Core;

/// <summary>
/// Handles import of Shape files into SQL Server
/// </summary>
public sealed class ShapeFileImporter
{
    private readonly ILogger<ShapeFileImporter> _logger;
    private string _shapeFile;
    private Dictionary<string, Type> _fields = [];
    private ICoordinateTransformation? _transform;

    private BackgroundWorker? _worker;

    /// <summary>
    /// Event raised when progress is changed
    /// </summary>
    public event EventHandler<ProgressChangedEventArgs>? ProgressChanged;
    /// <summary>
    /// Event raised when import is done
    /// </summary>
    public event EventHandler? Done;
    /// <summary>
    /// Event raised when an error occurs during import (event args contains useful details)
    /// </summary>
    public event EventHandler<ShapeImportExceptionEventArgs>? Error;

    #region Properties
    private string? _coordinateSystem;
    /// <summary>
    /// Coordinate system used
    /// </summary>
    public string CoordinateSystem
    {
        get
        {
            if (_coordinateSystem == null)
            {
                //Lambert93 "PROJCS[\"RGF93 / Lambert-93\",GEOGCS[\"RGF93\",DATUM[\"Reseau_Geodesique_Francais_1993\",SPHEROID[\"GRS 1980\",6378137,298.257222101,AUTHORITY[\"EPSG\",\"7019\"]],TOWGS84[0,0,0,0,0,0,0],AUTHORITY[\"EPSG\",\"6171\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.01745329251994328,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4171\"]],PROJECTION[\"Lambert_Conformal_Conic_2SP\"],PARAMETER[\"standard_parallel_1\",49],PARAMETER[\"standard_parallel_2\",44],PARAMETER[\"latitude_of_origin\",46.5],PARAMETER[\"central_meridian\",3],PARAMETER[\"false_easting\",700000],PARAMETER[\"false_northing\",6600000],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AUTHORITY[\"EPSG\",\"2154\"]]";

                using StreamReader reader = File.OpenText(ShapeFileHelper.GetProjectFile(_shapeFile));
                _coordinateSystem = reader.ReadToEnd();
            }

            return _coordinateSystem;
        }
    }

    private string? _tableName;
    /// <summary>
    /// Destination table
    /// </summary>
    public string? SqlTableName => _tableName;

    private string? _idField;
    /// <summary>
    /// Name of primary key field
    /// </summary>
    public string? SqlIDFIeld => _idField;

    private string? _geomField;
    /// <summary>
    /// Name of geometry field
    /// </summary>
    public string? SqlGeomField => _geomField;

    private Envelope? _bounds;
    /// <summary>
    /// Geometric boundarys of the shape file
    /// </summary>
    public Envelope? Bounds => _bounds;

    private ShapeGeometryType _shapeType;
    /// <summary>
    /// Shape type
    /// </summary>
    public ShapeGeometryType ShapeType => _shapeType;

    private int _recordCount;
    /// <summary>
    /// Number of records in the shape file
    /// </summary>
    public int RecordCount => _recordCount;

    /// <summary>
    /// Columns in the shape file
    /// </summary>
    public Dictionary<string, Type> Fields => _fields;

    #endregion

    /// <summary>
    /// Creates an instance of the ShapeFileImporter from a given shape file
    /// </summary>
    /// <param name="shapeFileName">Shapefile to import</param>
    /// <param name="logger">Optional logger instance for dependency injection</param>
    public ShapeFileImporter(string shapeFileName, ILogger<ShapeFileImporter>? logger = null)
    {
        _logger = logger ?? Shape2SqlServerLoggerFactory.CreateLogger<ShapeFileImporter>();
        try
        {
            _shapeFile = shapeFileName;

            Init();
        }
        catch (Exception ex)
        {
            Raise_Error(new ShapeImportExceptionEventArgs(ex, true));
            throw;
        }
    }

    /// <summary>
    /// Launches import in safe mode (exceptions can be handled)
    /// </summary>
    /// <param name="connectionString">Destination database connection string</param>
    /// <param name="targetCoordSystem">Coordinate system used for reprojection</param>
    /// <param name="recreateTable">Whether to recreate the destination table</param>
    /// <param name="spatialType">Spatial type of the geometry</param>
    /// <param name="SRID">Spatial reference ID</param>
    /// <param name="tableName">Name of the destination table</param>
    /// <param name="schema">SQL Server destination schema</param>
    /// <param name="IdColName">Name of the primary key field</param>
    /// <param name="fieldsToImport">List of fields to import</param>
    /// <param name="geomcolName">Name of the geometry column</param>
    public void ImportShapeFile(string connectionString,
                                                            string? targetCoordSystem,
                                                            bool recreateTable,
                                                            enSpatialType spatialType,
                                                            int SRID,
                                                            string tableName,
                                                            string schema,
                                                            string IdColName,
                                                            string geomcolName,
                                                            List<string> fieldsToImport)
    {
        _worker = new();
        _worker.WorkerSupportsCancellation = true;
        _worker.WorkerReportsProgress = true;
        _worker.RunWorkerCompleted += _worker_RunWorkerCompleted;
        _worker.ProgressChanged += _worker_ProgressChanged;
        _worker.DoWork += (sender, e) =>
        {
            try
            {
                #region Work

                _logger.LogInformation("Worker started");
                _worker.ReportProgress(0, "Starting...");

                #region Init ICoordinateTransformation
                _transform = null;
                if (!string.IsNullOrWhiteSpace(targetCoordSystem))
                {
                    //string v_targetCoordSys =  "GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563,AUTHORITY[\"EPSG\",\"7030\"]],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.01745329251994328,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4326\"]]";

                    var csFactory = new CoordinateSystemFactory();
                    var csSource = csFactory.CreateFromWkt(CoordinateSystem);
                    var csTarget = csFactory.CreateFromWkt(targetCoordSystem);

                    _transform = new CoordinateTransformationFactory().CreateFromCoordinateSystems(csSource, csTarget);
                }
                #endregion Init ICoordinateTransformation


                using (ShapefileDataReader shapeDataReader = new(_shapeFile, GeometryFactory.Default))
                {
                    using (SqlConnection db = new(connectionString))
                    {
                        _logger.LogInformation("Opening SQL connection");
                        db.Open();

                        SqlTransaction transaction = db.BeginTransaction(IsolationLevel.Serializable);


                        try
                        {

                            #region Create destination table



                            DbaseFieldDescriptor[] fields = (from field in shapeDataReader.DbaseHeader.Fields
                                                             where fieldsToImport.Contains(field.Name)
                                                             select field).ToArray();
                            List<SqlColumnDescriptor> sqlFields = ShapeFileHelper.TranslateDbfTypesToSql(fields);

                            _logger.LogInformation($"Create SQL table {tableName}");
                            string sqlScriptCreateTable = SqlServerModel.GenerateCreateTableScript(tableName, schema, sqlFields, spatialType, recreateTable, geomcolName, IdColName);
                            DataTable dataTable = SqlServerModel.GenerateDataTable(tableName, sqlFields, spatialType, recreateTable, geomcolName, IdColName);
                            new SqlCommand(sqlScriptCreateTable, db, transaction).ExecuteNonQuery();

                            #endregion

                            #region Read shape file

                            int numRecord = 0;
                            while (shapeDataReader.Read())
                            {
                                numRecord++;
                                try
                                {
                                    #region Shape feature import

                                    if (_worker.CancellationPending)
                                        break;

                                    Geometry geom = shapeDataReader.Geometry;
                                    Geometry? geomOut = null; // BUGGY GeometryTransform.TransformGeometry(GeometryFactory.Default, geom, trans.MathTransform);
                                    if (_transform == null)
                                        geomOut = geom;
                                    else
                                        geomOut = ShapeFileHelper.ReprojectGeometry(geom, _transform);

                                    #region Prepare insert

                                    // Set geom SRID
                                    geomOut.SRID = SRID;

                                    List<object?> SqlNativeGeomList = [];
                                    List<object> properties = [];

                                    switch (spatialType)
                                    {
                                        #region geography
                                        case enSpatialType.geography:
                                            try
                                            {
                                                SqlNativeGeomList.Add(SqlServerHelper.ConvertToSqlType(geomOut, SRID, true, numRecord, _logger));
                                            }
                                            catch (Exception exGeomConvert)
                                            {
                                                _logger.LogError(exGeomConvert, "Error Converting geography");
                                                var args = new ShapeImportExceptionEventArgs(exGeomConvert, false, shapeDataReader.DumpCurrentRecord(), shapeDataReader.Geometry, numRecord);
                                                if (Raise_Error(args))
                                                {
                                                    if (args.Ignore)
                                                        SqlNativeGeomList = [];
                                                    else
                                                        break;
                                                }
                                                else
                                                    break;
                                            }

                                            break;
                                        #endregion
                                        #region geometry
                                        case enSpatialType.geometry:
                                            try
                                            {
                                                SqlNativeGeomList.Add(SqlServerHelper.ConvertToSqlType(geomOut, SRID, false, numRecord, _logger));
                                            }
                                            catch (Exception exGeomConvert)
                                            {
                                                _logger.LogError(exGeomConvert, "Error Converting geometry");
                                                var args = new ShapeImportExceptionEventArgs(exGeomConvert, false, shapeDataReader.DumpCurrentRecord(), shapeDataReader.Geometry, numRecord);
                                                if (Raise_Error(args))
                                                {
                                                    if (args.Ignore)
                                                        SqlNativeGeomList = [];
                                                    else
                                                        break;
                                                }
                                                else
                                                    break;
                                            }

                                            break;
                                        #endregion
                                        #region both
                                        case enSpatialType.both:

                                            bool geomConverted = false;
                                            try
                                            {
                                                SqlNativeGeomList.Add(SqlServerHelper.ConvertToSqlType(geomOut, SRID, false, numRecord, _logger));
                                                geomConverted = true;
                                                SqlNativeGeomList.Add(SqlServerHelper.ConvertToSqlType(geomOut, SRID, true, numRecord, _logger));
                                            }
                                            catch (Exception exGeomConvert)
                                            {
                                                _logger.LogError(exGeomConvert, "Error Converting geometry or geography");
                                                var args = new ShapeImportExceptionEventArgs(exGeomConvert, false, shapeDataReader.DumpCurrentRecord(), shapeDataReader.Geometry, numRecord);
                                                if (Raise_Error(args))
                                                {
                                                    if (args.Ignore)
                                                    {
                                                        if (geomConverted)
                                                            SqlNativeGeomList.Add(null);
                                                        else
                                                            SqlNativeGeomList.AddRange([null, null]);
                                                    }
                                                    else
                                                        break;
                                                }
                                                else
                                                    break;
                                            }

                                            break;
                                            #endregion

                                    }


                                    // Get Attributes
                                    for (int i = 0; i < fields.Length; i++)
                                        properties.Add(shapeDataReader[fields[i].Name]);


                                    // Fill in-memory datatable
                                    DataRow row = SqlServerModel.GetNewDataTableRow(dataTable, tableName, SqlNativeGeomList, properties);
                                    dataTable.Rows.Add(row);

                                    #endregion

                                    //if (numRecord % 10 == 0)
                                    _worker.ReportProgress((int)((numRecord * 100f) / RecordCount), $"Reading {numRecord} records");

                                    #endregion
                                }
                                catch (Exception exGeom)
                                {
                                    _logger.LogError(exGeom, "Error Converting geometry");
                                    Raise_Error(new ShapeImportExceptionEventArgs(exGeom, true, shapeDataReader.DumpCurrentRecord(), shapeDataReader.Geometry, numRecord));
                                }
                            }

                            #endregion Read shape file

                            #region Bulk insert

                            if (!_worker.CancellationPending)
                            {
                                using (SqlBulkCopy bulk = new(db, SqlBulkCopyOptions.Default, transaction))
                                {
                                    try
                                    {
                                        bulk.DestinationTableName = SqlServerModel.GenerateFullTableName(tableName, schema);
                                        bulk.BulkCopyTimeout = 3600; // 1 hour timeout
                                        bulk.NotifyAfter = 10;
                                        bulk.SqlRowsCopied += (o, args) =>
                                        {
                                            if (_worker.CancellationPending)
                                                args.Abort = true;
                                            else
                                                _worker.ReportProgress((int)((args.RowsCopied * 100f) / RecordCount), $"Writing {args.RowsCopied} records");

                                        };

                                        _worker.ReportProgress(0, "Writing 0 records");
                                        bulk.WriteToServer(dataTable);
                                        bulk.Close();
                                    }
                                    catch (SqlException ex)
                                    {
                                        _logger.LogError(ex, "Error inserting");
                                        bulk.Close();
                                    }

                                }
                            }

                            #endregion

                            if (_worker.CancellationPending)
                            {
                                _logger.LogWarning("Rolling back transaction");
                                transaction.Rollback();
                            }
                            else
                            {
                                #region Create spatial index

                                _logger.LogInformation("Create spatial index");
                                _worker.ReportProgress(100, "Creating index...");

                                // Create spatial index
                                string sqlScriptCreateIndex = SqlServerModel.GenerateCreateSpatialIndexScript(tableName, schema, geomcolName, SqlServerHelper.GetBoundingBox(Bounds!), spatialType, enSpatialIndexGridDensity.MEDIUM);
                                SqlCommand v_createdIndexcmd = new(sqlScriptCreateIndex, db, transaction);
                                v_createdIndexcmd.CommandTimeout = 3600;
                                v_createdIndexcmd.ExecuteNonQuery();

                                #endregion

                                _logger.LogInformation("Commit transaction");
                                transaction.Commit();

                            }

                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error");

                            _logger.LogWarning("Rolling back transaction");
                            transaction.Rollback();
                            if (!Raise_Error(new ShapeImportExceptionEventArgs(ex, true)))
                                throw;
                        }


                        _logger.LogTrace("closing DB");
                        db.Close();
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error");

                Raise_Error(new ShapeImportExceptionEventArgs(ex, true));

            }

        };

        _worker.RunWorkerAsync();

    }

    /// <summary>
    /// Launches import in direct mode (faster, but exceptions cannot be handled)
    /// </summary>
    /// <param name="connectionString"></param>
    /// <param name="targetCoordSystem"></param>
    /// <param name="recreateTable"></param>
    /// <param name="spatialType"></param>
    /// <param name="SRID"></param>
    /// <param name="tableName"></param>
    /// <param name="schema"></param>
    /// <param name="IdColName"></param>
    /// <param name="geomcolName"></param>
    /// <param name="fieldsToImport"></param>
    public void ImportShapeFile_Direct(string connectionString,
                                                            string? targetCoordSystem,
                                                            bool recreateTable,
                                                            enSpatialType spatialType,
                                                            int SRID,
                                                            string tableName,
                                                            string schema,
                                                            string IdColName,
                                                            string geomcolName,
                                                            List<string> fieldsToImport)
    {
        _worker = new();
        _worker.WorkerSupportsCancellation = true;
        _worker.WorkerReportsProgress = true;
        _worker.RunWorkerCompleted += _worker_RunWorkerCompleted;
        _worker.ProgressChanged += _worker_ProgressChanged;
        _worker.DoWork += (sender, e) =>
        {
            try
            {
                #region Work
                int recordIndex = 1;
                _logger.LogInformation("Worker started");
                _worker.ReportProgress(0, "Starting...");

                #region Init ICoordinateTransformation
                _transform = null;
                if (!string.IsNullOrWhiteSpace(targetCoordSystem))
                {
                    //string v_targetCoordSys =  "GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563,AUTHORITY[\"EPSG\",\"7030\"]],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.01745329251994328,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4326\"]]";

                    var csFactory = new CoordinateSystemFactory();
                    var csSource = csFactory.CreateFromWkt(CoordinateSystem);
                    var csTarget = csFactory.CreateFromWkt(targetCoordSystem);

                    _transform = new CoordinateTransformationFactory().CreateFromCoordinateSystems(csSource, csTarget);
                }
                #endregion Init ICoordinateTransformation


                using (SqlConnection db = new(connectionString))
                {

                    db.Open();
                    SqlTransaction transaction = db.BeginTransaction(IsolationLevel.Serializable);

                    using (SqlBulkCopy bulk = new(db, SqlBulkCopyOptions.Default, transaction))
                    {
                        using (ShapeFileReaderBulk shapeDataReader = new(_shapeFile, _transform, spatialType, SRID, _logger, Internal_RaiseError))
                        {

                            try
                            {
                                bulk.DestinationTableName = SqlServerModel.GenerateFullTableName(tableName, schema);
                                bulk.BulkCopyTimeout = 0;
                                bulk.NotifyAfter = 1;
                                bulk.SqlRowsCopied += (o, args) =>
                                {
                                    recordIndex++;

                                    if (_worker.CancellationPending)
                                        args.Abort = true;
                                    else
                                        _worker.ReportProgress((int)((args.RowsCopied * 100f) / RecordCount), $"Writing {args.RowsCopied} records");

                                };

                                _logger.LogInformation("Writing 0 records");
                                _worker.ReportProgress(0, "Writing 0 records");

                                #region Column mappings
                                List<DbaseFieldDescriptor> fieldsList = [];
                                int idxSource = 0;
                                int idxDest = 1;
                                foreach (DbaseFieldDescriptor desc in shapeDataReader.DbaseHeader.Fields)
                                {
                                    if (fieldsToImport.Contains(desc.Name))
                                    {
                                        bulk.ColumnMappings.Add(desc.Name, SqlServerModel.CleanSQLName(desc.Name));
                                        fieldsList.Add(desc);
                                        idxDest++;
                                    }
                                    idxSource++;
                                }
                                switch (spatialType)
                                {
                                    case enSpatialType.geometry:
                                    case enSpatialType.geography:
                                        bulk.ColumnMappings.Add("geom", geomcolName);
                                        break;
                                    case enSpatialType.both:
                                        bulk.ColumnMappings.Add("geom_geom", geomcolName + "_geom");
                                        bulk.ColumnMappings.Add("geom_geog", geomcolName + "_geog");
                                        break;
                                }
                                #endregion Column mappings

                                #region create table
                                List<SqlColumnDescriptor> sqlFields = ShapeFileHelper.TranslateDbfTypesToSql([.. fieldsList]);

                                _logger.LogInformation($"Creating table {tableName}");
                                string sqlScriptCreateTable = SqlServerModel.GenerateCreateTableScript(tableName, schema, sqlFields, spatialType, recreateTable, geomcolName, IdColName);
                                DataTable dataTable = SqlServerModel.GenerateDataTable(tableName, sqlFields, spatialType, recreateTable, geomcolName, IdColName);
                                new SqlCommand(sqlScriptCreateTable, db, transaction).ExecuteNonQuery();
                                #endregion

                                bool bulkInError = false;
                                try
                                {
                                    bulk.WriteToServer(shapeDataReader);
                                }
                                catch (SqlException)
                                {
                                    bulkInError = true;
                                    _logger.LogError("SqlBulkImport throw SqlException");
                                }
                                catch (Exception exBulk)
                                {
                                    _logger.LogError(exBulk, "SqlBulkImport throw Exception");
                                    Raise_Error(new ShapeImportExceptionEventArgs(exBulk, true, shapeDataReader.DumpCurrentRecord(), shapeDataReader.Geometry, recordIndex));
                                    bulkInError = true;

                                }

                                bulk.Close();

                                if (_worker.CancellationPending || bulkInError)
                                {
                                    _logger.LogWarning("Rolling back transaction");
                                    transaction.Rollback();
                                }
                                else
                                {
                                    #region Create spatial index

                                    _logger.LogInformation("Creating spatial index...");
                                    _worker.ReportProgress(100, "Creating index...");

                                    // Create spatial index
                                    Envelope bounds = ShapeFileHelper.ReprojectEnvelope(_transform, Bounds!);
                                    string sqlScriptCreateIndex = SqlServerModel.GenerateCreateSpatialIndexScript(tableName, schema, geomcolName, SqlServerHelper.GetBoundingBox(bounds), spatialType, enSpatialIndexGridDensity.MEDIUM);
                                    SqlCommand v_createdIndexcmd = new(sqlScriptCreateIndex, db, transaction);
                                    v_createdIndexcmd.CommandTimeout = 3600;
                                    v_createdIndexcmd.ExecuteNonQuery();

                                    #endregion

                                    _logger.LogInformation("Commit transaction");
                                    transaction.Commit();

                                }

                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error");
                                _logger.LogWarning("Rolling back transaction");
                                transaction.Rollback();
                                Raise_Error(new ShapeImportExceptionEventArgs(ex, true));
                            }
                            finally
                            {
                                _logger.LogTrace("SqlBulkCopy.Close()");
                                bulk.Close();
                            }
                        }
                    }

                    _logger.LogTrace("db.Close()");
                    db.Close();
                }


                #endregion
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");
                Raise_Error(new ShapeImportExceptionEventArgs(ex, true));
            }

        };

        _worker.RunWorkerAsync();

    }

    /// <summary>
    /// Cancels the import process
    /// </summary>
    public void CancelAsync()
    {
        _worker?.CancelAsync();
    }

    #region Events

    private void Internal_RaiseError(object? sender, ShapeImportExceptionEventArgs eventArgs)
    {
        Raise_Error(eventArgs);
    }
    private bool Raise_Error(ShapeImportExceptionEventArgs args)
    {
        bool ret = false;
        try
        {
            if (args.ShapeInfo == null)
            {
                _logger.LogError($"{((Exception)args.ExceptionObject).Message}");
            }
            else
            {
                if (args.ShapeGeom == null)
                    _logger.LogError($"{((Exception)args.ExceptionObject).Message} Index={args.ShapeIndex}, Attributes={args.ShapeInfo.Replace("\n", ", ")}, \r\nGeom=<NULL>");
                else
                    _logger.LogError($"{((Exception)args.ExceptionObject).Message} Index={args.ShapeIndex}, Attributes={args.ShapeInfo.Replace("\n", ", ")}, \r\nGeom={args.ShapeGeom}, \r\nReversedGeom={args.ShapeGeom.Reverse()}");
            }

            Error?.Invoke(this, args);
            ret = true;
        }
        catch (Exception)
        {
            ret = false;
        }

        return ret;
    }

    #endregion

    #region Worker events

    private void _worker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
    {
        ProgressChanged?.Invoke(this, e);
    }

    private void _worker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
    {
        _logger.LogInformation("Worker completed");
        Done?.Invoke(this, new EventArgs());
    }

    #endregion

    #region Private

    private void Init()
    {
        _logger.LogInformation("Trace started");

        // Check files
        ShapeFileHelper.CheckFiles(_shapeFile);

        // REVERSE
        SqlServerHelper.REVERSE_GEOMETRIES = null;

        // construct Sql table name
        _tableName = SqlServerModel.CleanSQLName(Path.GetFileNameWithoutExtension(_shapeFile));

        // Init reader
        using (ShapefileDataReader reader = new(_shapeFile, GeometryFactory.Default))
        {
            // Get Shape info
            _bounds = reader.ShapeHeader.Bounds;
            _shapeType = reader.ShapeHeader.ShapeType;
            _recordCount = reader.RecordCount;

            _fields = [];
            foreach (var field in reader.DbaseHeader.Fields)
            {
                _fields.Add(field.Name, field.Type);
            }

            _idField = SqlServerModel.GenerateUniqueColName("ID", ShapeFileHelper.TranslateDbfTypesToSql(reader.DbaseHeader.Fields), _tableName);
            _geomField = SqlServerModel.GenerateUniqueColName("geom", ShapeFileHelper.TranslateDbfTypesToSql(reader.DbaseHeader.Fields), _tableName);
        }



    }

    #region ShapeFile Reader converters

    private Coordinate[] transformCoordinates(ICoordinateTransformation? trans, Coordinate[] source)
    {
        if (trans == null)
            return source;

        List<Coordinate> coordlist = [];
        foreach (var c in source)
        {
            double[] coords = trans.MathTransform.Transform([c.X, c.Y, c.Z]);
            var coord = new Coordinate(coords[0], coords[1]) { Z = coords[2] };
            coordlist.Add(coord);
        }

        return coordlist.Reverse<Coordinate>().ToArray();
    }

    #endregion


    #endregion

}
