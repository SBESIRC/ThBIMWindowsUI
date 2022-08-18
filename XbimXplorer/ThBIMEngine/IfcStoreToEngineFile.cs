using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using THBimEngine.Domain;
using THBimEngine.Presention;
using Xbim.Common;
using Xbim.Common.Federation;
using Xbim.Common.Geometry;
using Xbim.Ifc;
using Xbim.Presentation.LayerStyling;
using Xbim.Presentation.Modelpositioning;

namespace XbimXplorer.ThBIMEngine
{
    class IfcStoreToEngineFile
	{
		public event ProgressChangedEventHandler ProgressChanged;
		protected IfcStore ifcModel;
		XbimModelRelativeTranformer _modelPositioner = new XbimModelRelativeTranformer();
		Dictionary<IModel, XbimMatrix3D> _currentModelPositions = new Dictionary<IModel, XbimMatrix3D>();
		private XbimRect3D _viewBounds { get; set; }
		public double ModelRegionTolerance { get; set; } = 5;
		public IfcStoreToEngineFile()
		{
		}
		public Dictionary<int, int> LoadGeometry(IfcStore model, string midFilePath)
		{
			var readGeomtry = new IfcStoreReadGeomtry();
			readGeomtry.ProgressChanged += ProgressChanged;
			var excludedTypes = model.DefaultExclusions(null);
			var geoIndexIfcIndexMap = new Dictionary<int, int>();
			var allGeoModels = new List<GeometryMeshModel>();
			var allGeoPointNormals = new List<PointNormal>();
			if (null == model || model.Instances == null)
				return geoIndexIfcIndexMap;
			if (model.IsFederation)
			{
				LoadFederationGeometry(model);
				foreach (var refModel in model.ReferencedModels) 
				{
					var thisGeo = readGeomtry.ReadGeomtry(refModel.Model as IfcStore, out List<PointNormal> thisPointVectors);
					var ptOffSet = allGeoPointNormals.Count;
					foreach (var item in thisPointVectors)
					{
						item.PointIndex += ptOffSet;
					}
					foreach (var item in thisGeo)
					{
						item.CIndex += ptOffSet;
						foreach (var tr in item.FaceTriangles)
						{
							for (int i = 0; i < tr.ptIndex.Count; i++)
								tr.ptIndex[i] += ptOffSet;
						}
					}
					allGeoModels.AddRange(thisGeo);
					allGeoPointNormals.AddRange(thisPointVectors);
				}
			}
			else 
			{
				allGeoModels = readGeomtry.ReadGeomtry(model, out allGeoPointNormals);
			}
			if (null != ProgressChanged)
				ProgressChanged(this, new ProgressChangedEventArgs(100, "Reading Shape End"));
			if (null != allGeoModels && allGeoModels.Count > 0) 
			{
				WriteMidDataMultithreading(allGeoModels, allGeoPointNormals);
				for (int i = 0; i < allGeoModels.Count; i++)
				{
					var tempModel = allGeoModels[i];
					geoIndexIfcIndexMap.Add(i, Convert.ToInt32(tempModel.EntityLable));
				}
			}
			return geoIndexIfcIndexMap;
		}

		private void LoadFederationGeometry(IfcStore model) 
		{
			if (model == null)
			{
				return; //nothing to show
			}
			// ensure a unique userDefinedId
			short userDefinedId = 0;
			model.UserDefinedId = userDefinedId;
			if (model.IsFederation)
			{
				foreach (var refModel in model.ReferencedModels)
				{
					refModel.Model.UserDefinedId = ++userDefinedId;
				}
			}

			XbimMatrix3D modelmatrix = XbimMatrix3D.Identity;
			if (!_currentModelPositions.Any())
			{
				var initialRegion = XbimModelRelativeTranformer.GetExpandedMostPopulated(model, ModelRegionTolerance);
				if (initialRegion != null)
				{
					_modelPositioner = new XbimModelRelativeTranformer();
					modelmatrix = _modelPositioner.SetBaseModel(model, initialRegion);
					_viewBounds = initialRegion.ToXbimRect3D().Transform(modelmatrix);
				}
			}
			else
			{
				// currently never enters here, but kept for future developments
				modelmatrix = _modelPositioner.GetRelativeMatrix(model);
				throw new NotImplementedException("LoadGeometry with existing model present.");
			}
			_currentModelPositions.Add(model, modelmatrix);

			if (model.IsFederation)
			{
				// loading all referenced models.
				foreach (var refModel in model.ReferencedModels)
				{
					LoadReferencedModel(refModel);
				}
			}
		}
		private void LoadReferencedModel(IReferencedModel refModel)
		{
			if (refModel.Model == null)
				return;

			//DefaultLayerStyler.SetFederationEnvironment(refModel);
			var mod = refModel.Model as IfcStore;
			if (mod == null)
				return;

			var initialRegion = XbimModelRelativeTranformer.GetExpandedMostPopulated(mod.ReferencingModel, ModelRegionTolerance);
			XbimMatrix3D pos = XbimMatrix3D.Identity;
			if (_modelPositioner == null)
			{
				_modelPositioner = new XbimModelRelativeTranformer();
				pos = _modelPositioner.SetBaseModel(mod.ReferencingModel, initialRegion);
				_viewBounds = initialRegion.ToXbimRect3D().Transform(pos);
			}
			else
			{
				pos = _modelPositioner.GetRelativeMatrix(mod.ReferencingModel);
				// see if we need to expand the view bounds

				var newRegion = initialRegion.ToXbimRect3D().Transform(pos);

				//Debug.WriteLine($"ths Region    :{newRegion}");
				//Debug.WriteLine($"Was ViewBounds:{_viewBounds}");
				_viewBounds = _viewBounds.Union(newRegion);
				//Debug.WriteLine($"Now ViewBounds:{_viewBounds}");
			}

			_currentModelPositions.Add(mod.ReferencingModel, pos);
			
		}
		


		public void WriteMidFile(List<GeometryMeshModel> meshModels, List<PointNormal> meshPoints, string midFilePath)
		{
			if (null == meshModels || meshModels.Count < 1
				|| null == meshPoints || meshPoints.Count < 1)
				return;
			if (null != ProgressChanged)
				ProgressChanged(this, new ProgressChangedEventArgs(5, "Convert To midFile"));
			if (!string.IsNullOrEmpty(midFilePath) && File.Exists(midFilePath))
				File.Delete(midFilePath);
			var create = new FileStream(midFilePath, FileMode.Create);
			BinaryWriter writer = new BinaryWriter(create);
			ulong ptCount = (ulong)meshPoints.Count();
			//vertices
			writer.Write(ptCount * 3);
			for (int i = 0; i < meshPoints.Count; i++)
			{
				var point = meshPoints[i];
				writer.Write(point.Point.X);
				writer.Write(point.Point.Y);
				writer.Write(point.Point.Z);
			}
			if (null != ProgressChanged)
				ProgressChanged(this, new ProgressChangedEventArgs(20, "Convert To midFile"));
			//normals
			writer.Write(ptCount * 3);
			for (int i = 0; i < meshPoints.Count; i++)
			{
				var point = meshPoints[i];
				writer.Write(point.Normal.X);
				writer.Write(point.Normal.Y);
				writer.Write(point.Normal.Z);
			}
			if (null != ProgressChanged)
				ProgressChanged(this, new ProgressChangedEventArgs(40, "Convert To midFile"));
			//global_indices, All triangle faces info
			var sumCount = (ulong)meshModels.Sum(c => c.FaceTriangles.Sum(x => x.ptIndex.Count()));
			writer.Write(sumCount);
			for (int i = 0; i < meshModels.Count; i++)
			{
				var meshModel = meshModels[i];
				foreach (var item in meshModel.FaceTriangles)
				{
					foreach (int ptIndex in item.ptIndex)
						writer.Write(ptIndex);
				}
			}
			if (null != ProgressChanged)
				ProgressChanged(this, new ProgressChangedEventArgs(60, "Convert To midFile"));
			//components' indices, all components indices
			ulong cIdCount = (ulong)meshModels.Count;
			writer.Write(cIdCount);
			foreach (var item in meshModels)
			{
				ulong vCount = (ulong)item.FaceTriangles.Sum(c => c.ptIndex.Count);
				writer.Write(vCount);
				foreach (var value in item.FaceTriangles)
				{
					foreach (int ptIndex in value.ptIndex)
						writer.Write(ptIndex);
				}
			}
			if (null != ProgressChanged)
				ProgressChanged(this, new ProgressChangedEventArgs(80, "Convert To midFile"));
			//material datas
			ulong mCount = (ulong)meshModels.Sum(c => c.FaceTriangles.Count());
			writer.Write(mCount);
			foreach (var item in meshModels)
			{
				writer.Write(item.TriangleMaterial.Color_R);
				writer.Write(item.TriangleMaterial.Color_G);
				writer.Write(item.TriangleMaterial.Color_B);
				writer.Write(item.TriangleMaterial.KS_R);
				writer.Write(item.TriangleMaterial.KS_G);
				writer.Write(item.TriangleMaterial.KS_B);
				writer.Write(item.TriangleMaterial.Alpha);
				writer.Write(item.TriangleMaterial.NS);
			}
			if (null != ProgressChanged)
				ProgressChanged(this, new ProgressChangedEventArgs(100, "Convert End"));
			writer.Close();
			if (null != ProgressChanged)
				ProgressChanged(this, new ProgressChangedEventArgs(0, ""));
		}

		//实现新的写数据方式
		public void WriteMidData(List<GeometryMeshModel> meshModels, List<PointNormal> meshPoints)
		{
			ExampleScene.ifcre_clear_model_data();
			if (null == meshModels || meshModels.Count < 1
				|| null == meshPoints || meshPoints.Count < 1)
				return;
			if (null != ProgressChanged)
				ProgressChanged(this, new ProgressChangedEventArgs(5, "Convert To midFile"));
			ulong ptCount = (ulong)meshPoints.Count();
			//vertices
			for (int i = 0; i < meshPoints.Count; i++)
			{
				var point = meshPoints[i];
				ExampleScene.ifcre_set_g_vertices(point.Point.X);
				ExampleScene.ifcre_set_g_vertices(point.Point.Y);
				ExampleScene.ifcre_set_g_vertices(point.Point.Z);
			}
			if (null != ProgressChanged)
				ProgressChanged(this, new ProgressChangedEventArgs(20, "Convert To midFile"));
			//normals
			for (int i = 0; i < meshPoints.Count; i++)
			{
				var point = meshPoints[i];

				ExampleScene.ifcre_set_g_normals(Math.Abs(point.Normal.X) < 1e-6 ? 0 : point.Normal.X);
				ExampleScene.ifcre_set_g_normals(Math.Abs(point.Normal.Y) < 1e-6 ? 0 : point.Normal.Y);
				ExampleScene.ifcre_set_g_normals(Math.Abs(point.Normal.Z) < 1e-6 ? 0 : point.Normal.Z);
			}
			if (null != ProgressChanged)
				ProgressChanged(this, new ProgressChangedEventArgs(40, "Convert To midFile"));
			//global_indices, All triangle faces info
			var sumCount = (ulong)meshModels.Sum(c => c.FaceTriangles.Sum(x => x.ptIndex.Count()));
			for (int i = 0; i < meshModels.Count; i++)
			{
				var meshModel = meshModels[i];
				foreach (var item in meshModel.FaceTriangles)
				{
					foreach (int ptIndex in item.ptIndex)
						ExampleScene.ifcre_set_g_indices(ptIndex);
					//三角形边信息
					var ptIndex1 = item.ptIndex[0];
					var ptIndex2 = item.ptIndex[1];
					var ptIndex3 = item.ptIndex[2];
					ExampleScene.ifcre_set_edge_indices(ptIndex1);
					ExampleScene.ifcre_set_edge_indices(ptIndex2);
					ExampleScene.ifcre_set_edge_indices(ptIndex2);
					ExampleScene.ifcre_set_edge_indices(ptIndex3);
					ExampleScene.ifcre_set_edge_indices(ptIndex3);
					ExampleScene.ifcre_set_edge_indices(ptIndex1);
				}
			}
			if (null != ProgressChanged)
				ProgressChanged(this, new ProgressChangedEventArgs(60, "Convert To midFile"));
			//components' indices, all components indices
			ulong cIdCount = (ulong)meshModels.Count;
			
			foreach (var item in meshModels)
			{
				ulong vCount = (ulong)item.FaceTriangles.Sum(c => c.ptIndex.Count);
				foreach (var value in item.FaceTriangles)
				{
					foreach (int ptIndex in value.ptIndex)
						ExampleScene.ifcre_set_c_indices(ptIndex);
				}
				ExampleScene.ifcre_set_c_indices(-1);
			}
			if (null != ProgressChanged)
				ProgressChanged(this, new ProgressChangedEventArgs(80, "Convert To midFile"));
			//material datas
			ulong mCount = (ulong)meshModels.Sum(c => c.FaceTriangles.Count());
			foreach (var mesh in meshModels)
			{
				ExampleScene.ifcre_set_face_mat(mesh.TriangleMaterial.Color_R);
				ExampleScene.ifcre_set_face_mat(mesh.TriangleMaterial.Color_G);
				ExampleScene.ifcre_set_face_mat(mesh.TriangleMaterial.Color_B);
				ExampleScene.ifcre_set_face_mat(mesh.TriangleMaterial.KS_R);
				ExampleScene.ifcre_set_face_mat(mesh.TriangleMaterial.KS_G);
				ExampleScene.ifcre_set_face_mat(mesh.TriangleMaterial.KS_B);
				ExampleScene.ifcre_set_face_mat(mesh.TriangleMaterial.Alpha);
				ExampleScene.ifcre_set_face_mat((float)mesh.TriangleMaterial.NS);
			}
			if (null != ProgressChanged)
				ProgressChanged(this, new ProgressChangedEventArgs(100, "Convert End"));
			if (null != ProgressChanged)
				ProgressChanged(this, new ProgressChangedEventArgs(0, ""));
			
		}


		public void WriteMidDataMultithreading(List<GeometryMeshModel> meshModels, List<PointNormal> meshPoints)
		{
			ExampleScene.ifcre_set_sleep_time(2000);
			ExampleScene.ifcre_clear_model_data();
			if (null == meshModels || meshModels.Count < 1 || null == meshPoints || meshPoints.Count < 1)
				return;
			List<Task> tasks = new List<Task>();
			tasks.Add(Task.Run(() =>
			{
				//vertices
				for (int i = 0; i < meshPoints.Count; i++)
				{
					var point = meshPoints[i];
					ExampleScene.ifcre_set_g_vertices(point.Point.X);
					ExampleScene.ifcre_set_g_vertices(point.Point.Y);
					ExampleScene.ifcre_set_g_vertices(point.Point.Z);
				}
			}));
			tasks.Add(Task.Run(() =>
			{
				//normals
				for (int i = 0; i < meshPoints.Count; i++)
				{
					var point = meshPoints[i];

					ExampleScene.ifcre_set_g_normals(Math.Abs(point.Normal.X) < 1e-6 ? 0 : point.Normal.X);
					ExampleScene.ifcre_set_g_normals(Math.Abs(point.Normal.Y) < 1e-6 ? 0 : point.Normal.Y);
					ExampleScene.ifcre_set_g_normals(Math.Abs(point.Normal.Z) < 1e-6 ? 0 : point.Normal.Z);
				}
			}));
			tasks.Add(Task.Run(() =>
			{
				//global_indices, All triangle faces info
				//var sumCount = (ulong)meshModels.Sum(c => c.FaceTriangles.Sum(x => x.ptIndex.Count()));
				for (int i = 0; i < meshModels.Count; i++)
				{
					var meshModel = meshModels[i];
					foreach (var item in meshModel.FaceTriangles)
					{
						foreach (int ptIndex in item.ptIndex)
							ExampleScene.ifcre_set_g_indices(ptIndex);
						//三角形边信息
						var ptIndex1 = item.ptIndex[0];
						var ptIndex2 = item.ptIndex[1];
						var ptIndex3 = item.ptIndex[2];
                        ExampleScene.ifcre_set_edge_indices(ptIndex1);
                        ExampleScene.ifcre_set_edge_indices(ptIndex2);
                        ExampleScene.ifcre_set_edge_indices(ptIndex2);
                        ExampleScene.ifcre_set_edge_indices(ptIndex3);
                        ExampleScene.ifcre_set_edge_indices(ptIndex3);
                        ExampleScene.ifcre_set_edge_indices(ptIndex1);
                    }
				}
			}));
			tasks.Add(Task.Run(() =>
			{
				//components' indices, all components indices
				foreach (var item in meshModels)
				{
					foreach (var value in item.FaceTriangles)
					{
						foreach (int ptIndex in value.ptIndex)
							ExampleScene.ifcre_set_c_indices(ptIndex);
					}
					ExampleScene.ifcre_set_c_indices(-1);
				}
			}));
			tasks.Add(Task.Run(() =>
			{
				//material datas
				ulong mCount = (ulong)meshModels.Sum(c => c.FaceTriangles.Count());
				foreach (var mesh in meshModels)
				{
					ExampleScene.ifcre_set_face_mat(mesh.TriangleMaterial.Color_R);
					ExampleScene.ifcre_set_face_mat(mesh.TriangleMaterial.Color_G);
					ExampleScene.ifcre_set_face_mat(mesh.TriangleMaterial.Color_B);
					ExampleScene.ifcre_set_face_mat(mesh.TriangleMaterial.KS_R);
					ExampleScene.ifcre_set_face_mat(mesh.TriangleMaterial.KS_G);
					ExampleScene.ifcre_set_face_mat(mesh.TriangleMaterial.KS_B);
					ExampleScene.ifcre_set_face_mat(mesh.TriangleMaterial.Alpha);
					ExampleScene.ifcre_set_face_mat((float)mesh.TriangleMaterial.NS);
				}
			}));
			Task.WaitAll(tasks.ToArray());
			ExampleScene.ifcre_set_sleep_time(10);
		}

	}
}
