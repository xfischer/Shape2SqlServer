using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using GeoAPI.CoordinateSystems;
using GeoAPI.CoordinateSystems.Transformations;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using ProjNet.CoordinateSystems.Transformations;
using MapBind.IO.Utils;
using System.Diagnostics;
using MapBind.Data.Models.SqlServer;

namespace MapBind.IO.ShapeFile
{
	public sealed class ShapeFileImporter
	{

		private string _shapeFile;
		private Dictionary<string, Type> _fields;
		private ICoordinateTransformation _transform;

		private BackgroundWorker _worker;

		public event EventHandler<ProgressChangedEventArgs> ProgressChanged;
		public event EventHandler Done;
		public event EventHandler<ShapeImportExceptionEventArgs> Error;

		#region Properties
		private string _coordinateSystem;
		public string CoordinateSystem
		{
			get
			{
				if (_coordinateSystem == null)
				{
					//Lambert93 "PROJCS[\"RGF93 / Lambert-93\",GEOGCS[\"RGF93\",DATUM[\"Reseau_Geodesique_Francais_1993\",SPHEROID[\"GRS 1980\",6378137,298.257222101,AUTHORITY[\"EPSG\",\"7019\"]],TOWGS84[0,0,0,0,0,0,0],AUTHORITY[\"EPSG\",\"6171\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.01745329251994328,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4171\"]],PROJECTION[\"Lambert_Conformal_Conic_2SP\"],PARAMETER[\"standard_parallel_1\",49],PARAMETER[\"standard_parallel_2\",44],PARAMETER[\"latitude_of_origin\",46.5],PARAMETER[\"central_meridian\",3],PARAMETER[\"false_easting\",700000],PARAMETER[\"false_northing\",6600000],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AUTHORITY[\"EPSG\",\"2154\"]]";

					using (StreamReader reader = File.OpenText(ShapeFileHelper.GetProjectFile(_shapeFile)))
						_coordinateSystem = reader.ReadToEnd();
				}

				return _coordinateSystem;
			}
		}

		private string _tableName;
		public string SqlTableName
		{
			get { return _tableName; }
		}

		private string _idField;
		public string SqlIDFIeld
		{
			get { return _idField; }
		}

		private string _geomField;
		public string SqlGeomField
		{
			get { return _geomField; }
		}

		private Envelope _bounds;
		public Envelope Bounds
		{
			get { return _bounds; }
		}

		private ShapeGeometryType _shapeType;
		public ShapeGeometryType ShapeType
		{
			get { return _shapeType; }
		}

		private int _recordCount;
		public int RecordCount
		{
			get { return _recordCount; }
		}


		public Dictionary<string, Type> Fields
		{
			get { return _fields; }
		}

		#endregion

		public ShapeFileImporter(string shapeFileName)
		{
			try
			{
				_shapeFile = shapeFileName;

				this.Init();
			}
			catch (Exception ex)
			{
				this.Raise_Error(new ShapeImportExceptionEventArgs(ex, true));
				throw;
			}
		}

		public void ImportShapeFile(string connectionString,
																string targetCoordSystem,
																bool recreateTable,
																enSpatialType spatialType,
																int SRID,
																string tableName,
																																string schema,
																string IdColName,
																string geomcolName,
																List<string> fieldsToImport)
		{



			_worker = new BackgroundWorker();
			_worker.WorkerSupportsCancellation = true;
			_worker.WorkerReportsProgress = true;
			_worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(_worker_RunWorkerCompleted);
			_worker.ProgressChanged += new ProgressChangedEventHandler(_worker_ProgressChanged);
			_worker.DoWork += new DoWorkEventHandler(delegate (object sender, DoWorkEventArgs e)
			{
				try
				{
					#region Work

					MapBindTrace.Source.TraceEvent(TraceEventType.Information, 1, "Worker started");
					_worker.ReportProgress(0, "Starting...");

					#region Init ICoordinateTransformation
					_transform = null;
					if (!string.IsNullOrWhiteSpace(targetCoordSystem))
					{
						//string v_targetCoordSys =  "GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563,AUTHORITY[\"EPSG\",\"7030\"]],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.01745329251994328,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4326\"]]";

						ICoordinateSystem csSource = ProjNet.Converters.WellKnownText.CoordinateSystemWktReader.Parse(this.CoordinateSystem) as ICoordinateSystem;
						ICoordinateSystem csTarget = ProjNet.Converters.WellKnownText.CoordinateSystemWktReader.Parse(targetCoordSystem) as ICoordinateSystem;

						_transform = new CoordinateTransformationFactory().CreateFromCoordinateSystems(csSource, csTarget);
					}
					#endregion Init ICoordinateTransformation


					using (ShapefileDataReader shapeDataReader = new ShapefileDataReader(_shapeFile, GeometryFactory.Default))
					{
						using (SqlConnection db = new SqlConnection(connectionString))
						{
							MapBindTrace.Source.TraceEvent(TraceEventType.Information, 1, "Opening SQL connection");
							db.Open();

							SqlTransaction transaction = db.BeginTransaction(IsolationLevel.Serializable);


							try
							{

								#region Create destination table



								DbaseFieldDescriptor[] fields = (from field in shapeDataReader.DbaseHeader.Fields
																								 where fieldsToImport.Contains(field.Name)
																								 select field).ToArray();
								List<SqlColumnDescriptor> sqlFields = ShapeFileHelper.TranslateDbfTypesToSql(fields);

								MapBindTrace.Source.TraceEvent(TraceEventType.Information, 1, "Create SQL table " + tableName);
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

										IGeometry geom = shapeDataReader.Geometry;
										IGeometry geomOut = null; // BUGGY GeometryTransform.TransformGeometry(GeometryFactory.Default, geom, trans.MathTransform);
										if (_transform == null)
											geomOut = geom;
										else
											geomOut = ShapeFileHelper.ReprojectGeometry(geom, _transform);

										#region Prepare insert

										// Set geom SRID
										geomOut.SRID = SRID;

										List<object> SqlNativeGeomList = new List<object>();
										List<object> properties = new List<object>();

										switch (spatialType)
										{
											#region geography
											case enSpatialType.geography:
												try
												{
													SqlNativeGeomList.Add(SqlServerHelper.ConvertToSqlType(geomOut, SRID, true, numRecord));
												}
												catch (Exception exGeomConvert)
												{
													MapBindTrace.Source.TraceData(TraceEventType.Error, 1, "Error Converting geography : ", exGeomConvert);
													var args = new ShapeImportExceptionEventArgs(exGeomConvert, false, shapeDataReader.DumpCurrentRecord(), shapeDataReader.Geometry, numRecord);
													if (this.Raise_Error(args))
													{
														if (args.Ignore)
															SqlNativeGeomList = new List<object>();
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
													SqlNativeGeomList.Add(SqlServerHelper.ConvertToSqlType(geomOut, SRID, false, numRecord));
												}
												catch (Exception exGeomConvert)
												{
													MapBindTrace.Source.TraceData(TraceEventType.Error, 1, "Error Converting geometry : ", exGeomConvert);
													var args = new ShapeImportExceptionEventArgs(exGeomConvert, false, shapeDataReader.DumpCurrentRecord(), shapeDataReader.Geometry, numRecord);
													if (this.Raise_Error(args))
													{
														if (args.Ignore)
															SqlNativeGeomList = new List<object>();
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
													SqlNativeGeomList.Add(SqlServerHelper.ConvertToSqlType(geomOut, SRID, false, numRecord));
													geomConverted = true;
													SqlNativeGeomList.Add(SqlServerHelper.ConvertToSqlType(geomOut, SRID, true, numRecord));
												}
												catch (Exception exGeomConvert)
												{
													MapBindTrace.Source.TraceData(TraceEventType.Error, 1, "Error Converting geometry or geography : ", exGeomConvert);
													var args = new ShapeImportExceptionEventArgs(exGeomConvert, false, shapeDataReader.DumpCurrentRecord(), shapeDataReader.Geometry, numRecord);
													if (this.Raise_Error(args))
													{
														if (args.Ignore)
														{
															if (geomConverted)
																SqlNativeGeomList.Add(null);
															else
																SqlNativeGeomList.AddRange(new object[] { null, null });
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
										_worker.ReportProgress((int)((numRecord * 100f) / this.RecordCount), string.Format("Reading {0} records", numRecord));

										#endregion
									}
									catch (Exception exGeom)
									{
										MapBindTrace.Source.TraceData(TraceEventType.Error, 1, "Error Converting geometry : ", exGeom);
										this.Raise_Error(new ShapeImportExceptionEventArgs(exGeom, true, shapeDataReader.DumpCurrentRecord(), shapeDataReader.Geometry, numRecord));
									}
								}

								#endregion Read shape file

								#region Bulk insert

								if (!_worker.CancellationPending)
								{
									using (SqlBulkCopy bulk = new SqlBulkCopy(db, SqlBulkCopyOptions.Default, transaction))
									{
										try
										{
											bulk.DestinationTableName = tableName;
											bulk.NotifyAfter = 10;
											bulk.SqlRowsCopied += (o, args) =>
											{
												if (_worker.CancellationPending)
													args.Abort = true;
												else
													_worker.ReportProgress((int)((args.RowsCopied * 100f) / this.RecordCount), string.Format("Writing {0} records", args.RowsCopied));

											};

											_worker.ReportProgress(0, string.Format("Writing {0} records", 0));
											bulk.WriteToServer(dataTable);
											bulk.Close();
										}
										catch (OperationAbortedException ex)
										{
											MapBindTrace.Source.TraceData(TraceEventType.Error, 1, "Error inserting: ", ex);
											bulk.Close();
										}

									}
								}

								#endregion

								if (_worker.CancellationPending)
								{
									MapBindTrace.Source.TraceEvent(TraceEventType.Warning, 1, "Rolling back transaction");
									transaction.Rollback();
								}
								else
								{
									#region Create spatial index

									MapBindTrace.Source.TraceEvent(TraceEventType.Information, 1, "Create spatial index");
									_worker.ReportProgress(100, "Creating index...");

									// Create spatial index
									string sqlScriptCreateIndex = SqlServerModel.GenerateCreateSpatialIndexScript(tableName, schema, geomcolName, SqlServerHelper.GetBoundingBox(this.Bounds), spatialType, enSpatialIndexGridDensity.MEDIUM);
									SqlCommand v_createdIndexcmd = new SqlCommand(sqlScriptCreateIndex, db, transaction);
									v_createdIndexcmd.CommandTimeout = 3600;
									v_createdIndexcmd.ExecuteNonQuery();

									#endregion

									MapBindTrace.Source.TraceEvent(TraceEventType.Information, 1, "Commit transaction");
									transaction.Commit();

								}

							}
							catch (Exception ex)
							{
								MapBindTrace.Source.TraceData(TraceEventType.Error, 2, "Error: ", ex);

								MapBindTrace.Source.TraceEvent(TraceEventType.Warning, 2, "Rolling back transaction");
								transaction.Rollback();
								if (!this.Raise_Error(new ShapeImportExceptionEventArgs(ex, true)))
									throw;
							}


							MapBindTrace.Source.TraceEvent(TraceEventType.Verbose, 2, "closing DB");
							db.Close();
						}
					}
					#endregion
				}
				catch (Exception ex)
				{
					MapBindTrace.Source.TraceData(TraceEventType.Error, 3, "Error: ", ex);

					this.Raise_Error(new ShapeImportExceptionEventArgs(ex, true));

				}

			});

			Trace.CorrelationManager.StartLogicalOperation("ImportShapeFile Worker");
			_worker.RunWorkerAsync();
			Trace.CorrelationManager.StopLogicalOperation();

		}

		public void ImportShapeFile_Direct(string connectionString,
																string targetCoordSystem,
																bool recreateTable,
																enSpatialType spatialType,
																int SRID,
																																string tableName,
																																string schema,
																																string IdColName,
																string geomcolName,
																List<string> fieldsToImport)
		{



			_worker = new BackgroundWorker();
			_worker.WorkerSupportsCancellation = true;
			_worker.WorkerReportsProgress = true;
			_worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(_worker_RunWorkerCompleted);
			_worker.ProgressChanged += new ProgressChangedEventHandler(_worker_ProgressChanged);
			_worker.DoWork += new DoWorkEventHandler(delegate (object sender, DoWorkEventArgs e)
			{
				try
				{
					#region Work
					TraceSource traceSource = new TraceSource("MapBind.IO");
					int recordIndex = 1;
					MapBindTrace.Source.TraceEvent(TraceEventType.Information, 1, "Worker started");
					_worker.ReportProgress(0, "Starting...");

					#region Init ICoordinateTransformation
					_transform = null;
					if (!string.IsNullOrWhiteSpace(targetCoordSystem))
					{
						//string v_targetCoordSys =  "GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563,AUTHORITY[\"EPSG\",\"7030\"]],AUTHORITY[\"EPSG\",\"6326\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.01745329251994328,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4326\"]]";

						ICoordinateSystem csSource = ProjNet.Converters.WellKnownText.CoordinateSystemWktReader.Parse(this.CoordinateSystem) as ICoordinateSystem;
						ICoordinateSystem csTarget = ProjNet.Converters.WellKnownText.CoordinateSystemWktReader.Parse(targetCoordSystem) as ICoordinateSystem;

						_transform = new CoordinateTransformationFactory().CreateFromCoordinateSystems(csSource, csTarget);
					}
					#endregion Init ICoordinateTransformation


					using (SqlConnection db = new SqlConnection(connectionString))
					{

						db.Open();
						SqlTransaction transaction = db.BeginTransaction(IsolationLevel.Serializable);

						using (SqlBulkCopy bulk = new SqlBulkCopy(db, SqlBulkCopyOptions.Default, transaction))
						{
							using (ShapeFileReaderBulk shapeDataReader = new ShapeFileReaderBulk(_shapeFile, _transform, spatialType, SRID, Internal_RaiseError))
							{

								try
								{
									bulk.DestinationTableName = tableName;
									bulk.BulkCopyTimeout = 0;
									bulk.NotifyAfter = 1;
									bulk.SqlRowsCopied += (o, args) =>
									{
										recordIndex++;

										if (_worker.CancellationPending)
											args.Abort = true;
										else
											_worker.ReportProgress((int)((args.RowsCopied * 100f) / this.RecordCount), string.Format("Writing {0} records", args.RowsCopied));

									};

									MapBindTrace.Source.TraceEvent(TraceEventType.Information, 1, string.Format("Writing {0} records", 0));
									_worker.ReportProgress(0, string.Format("Writing {0} records", 0));

									#region Column mappings
									List<DbaseFieldDescriptor> fieldsList = new List<DbaseFieldDescriptor>();
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
									List<SqlColumnDescriptor> sqlFields = ShapeFileHelper.TranslateDbfTypesToSql(fieldsList.ToArray());

									MapBindTrace.Source.TraceEvent(TraceEventType.Information, 1, "Creating table " + tableName);
									string sqlScriptCreateTable = SqlServerModel.GenerateCreateTableScript(tableName, schema, sqlFields, spatialType, recreateTable, geomcolName, IdColName);
									DataTable dataTable = SqlServerModel.GenerateDataTable(tableName, sqlFields, spatialType, recreateTable, geomcolName, IdColName);
									new SqlCommand(sqlScriptCreateTable, db, transaction).ExecuteNonQuery();
									#endregion

									bool bulkInError = false;
									try
									{
										bulk.WriteToServer(shapeDataReader);
									}
									catch (OperationAbortedException)
									{
										bulkInError = true;
										MapBindTrace.Source.TraceEvent(TraceEventType.Error, 1, "SqlBulkImport throw OperationAbortedException");
									}
									catch (Exception exBulk)
									{
										MapBindTrace.Source.TraceEvent(TraceEventType.Error, 1, "SqlBulkImport throw Exception" + exBulk.Message);
										this.Raise_Error(new ShapeImportExceptionEventArgs(exBulk, true, shapeDataReader.DumpCurrentRecord(), shapeDataReader.Geometry, recordIndex));
										bulkInError = true;

									}

									bulk.Close();

									if (_worker.CancellationPending || bulkInError)
									{
										MapBindTrace.Source.TraceEvent(TraceEventType.Warning, 1, "Rolling back transaction");
										transaction.Rollback();
									}
									else
									{
										#region Create spatial index

										MapBindTrace.Source.TraceEvent(TraceEventType.Information, 1, "Creating spatial index...");
										_worker.ReportProgress(100, "Creating index...");

										// Create spatial index
										Envelope bounds = ShapeFileHelper.ReprojectEnvelope(_transform, this.Bounds);
										string sqlScriptCreateIndex = SqlServerModel.GenerateCreateSpatialIndexScript(tableName, schema, geomcolName, SqlServerHelper.GetBoundingBox(bounds), spatialType, enSpatialIndexGridDensity.MEDIUM);
										SqlCommand v_createdIndexcmd = new SqlCommand(sqlScriptCreateIndex, db, transaction);
										v_createdIndexcmd.CommandTimeout = 3600;
										v_createdIndexcmd.ExecuteNonQuery();

										#endregion

										MapBindTrace.Source.TraceEvent(TraceEventType.Information, 1, "Commit transaction");
										transaction.Commit();

									}

								}
								catch (Exception ex)
								{
									MapBindTrace.Source.TraceEvent(TraceEventType.Error, 1, "Error : " + ex.Message);
									MapBindTrace.Source.TraceEvent(TraceEventType.Warning, 1, "Rolling back transaction");
									transaction.Rollback();
									this.Raise_Error(new ShapeImportExceptionEventArgs(ex, true));
								}
								finally
								{
									MapBindTrace.Source.TraceEvent(TraceEventType.Verbose, 1, "SqlBulkCopy.Close()");
									bulk.Close();
								}
							}
						}

						MapBindTrace.Source.TraceEvent(TraceEventType.Verbose, 1, "db.Close()");
						db.Close();
					}


					#endregion
				}
				catch (Exception ex)
				{
					MapBindTrace.Source.TraceEvent(TraceEventType.Error, 1, "Unhandled exception : " + ex.Message);
					this.Raise_Error(new ShapeImportExceptionEventArgs(ex, true));
				}

			});

			Trace.CorrelationManager.StartLogicalOperation("ImportShapeFile_Direct Worker");
			_worker.RunWorkerAsync();
			Trace.CorrelationManager.StartLogicalOperation("ImportShapeFile_Direct Worker");

		}

		public void CancelAsync()
		{
			_worker.CancelAsync();
		}

		#region Events

		private void Internal_RaiseError(object sender, ShapeImportExceptionEventArgs eventArgs)
		{
			this.Raise_Error(eventArgs);
		}
		private bool Raise_Error(ShapeImportExceptionEventArgs args)
		{
			bool ret = false;
			try
			{
				if (args.ShapeInfo == null)
				{
					MapBindTrace.Source.TraceEvent(TraceEventType.Error, 1, string.Format("{0}"
													, ((Exception)args.ExceptionObject).Message));
				}
				else
				{
					MapBindTrace.Source.TraceEvent(TraceEventType.Error, 1, string.Format("{0} Index={1}, Attributes={2}, \r\nGeom={3}, \r\nReversedGeom={4}"
														, ((Exception)args.ExceptionObject).Message
														, args.ShapeIndex
														, args.ShapeInfo.Replace("\n", ", ")
														, args.ShapeGeom.ToString()
														, args.ShapeGeom.Reverse().ToString()));
				}

				if (Error != null)
					Error(this, args);
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

		private void _worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			if (this.ProgressChanged != null)
				this.ProgressChanged(this, e);
		}

		private void _worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			MapBindTrace.Source.TraceEvent(TraceEventType.Information, 1, "Worker completed");
			if (Done != null) Done(this, new EventArgs());
		}

		#endregion

		#region Private

		private void Init()
		{
			MapBindTrace.Source.TraceInformation("Trace started");

			// Check files
			ShapeFileHelper.CheckFiles(_shapeFile);

			// REVERSE
			SqlServerHelper.REVERSE_GEOMETRIES = null;

			// construct Sql table name
			_tableName = SqlServerModel.CleanSQLName(Path.GetFileNameWithoutExtension(_shapeFile));

			// Init reader
			using (ShapefileDataReader reader = new ShapefileDataReader(_shapeFile, GeometryFactory.Default))
			{
				// Get Shape info
				_bounds = reader.ShapeHeader.Bounds;
				_shapeType = reader.ShapeHeader.ShapeType;
				_recordCount = reader.RecordCount;

				_fields = new Dictionary<string, Type>();
				foreach (var field in reader.DbaseHeader.Fields)
				{
					_fields.Add(field.Name, field.Type);
				}

				_idField = SqlServerModel.GenerateUniqueColName("ID", ShapeFileHelper.TranslateDbfTypesToSql(reader.DbaseHeader.Fields), _tableName);
				_geomField = SqlServerModel.GenerateUniqueColName("geom", ShapeFileHelper.TranslateDbfTypesToSql(reader.DbaseHeader.Fields), _tableName);
			}



		}

		#region ShapeFile Reader converters

		private Polygon transformPolygon(ICoordinateTransformation trans, Polygon polygon)
		{
			if (trans == null)
				return polygon;

			Coordinate[] extRing = this.transformCoordinates(trans, polygon.ExteriorRing.Coordinates);

			List<LinearRing> holes = new List<LinearRing>();
			foreach (var hole in polygon.Holes)
			{
				Coordinate[] holeCoords = this.transformCoordinates(trans, hole.Coordinates);
				holes.Add(new LinearRing(holeCoords));
			}

			return new Polygon(new LinearRing(extRing), holes.ToArray());
		}

		private Coordinate[] transformCoordinates(ICoordinateTransformation trans, Coordinate[] source)
		{
			if (trans == null)
				return source;

			List<Coordinate> coordlist = new List<Coordinate>();
			foreach (var c in source)
			{
				double[] coords = trans.MathTransform.Transform(new double[] { c.X, c.Y, c.Z });
				coordlist.Add(new Coordinate(coords[0], coords[1], coords[2]));
			}

			return coordlist.Reverse<Coordinate>().ToArray();
		}

		#endregion


		#endregion

	}
}
