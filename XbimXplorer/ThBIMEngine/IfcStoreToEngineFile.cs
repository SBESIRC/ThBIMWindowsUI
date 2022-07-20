using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xbim.Common;
using Xbim.Common.Federation;
using Xbim.Common.Geometry;
using Xbim.Common.XbimExtensions;
using Xbim.Ifc;
using Xbim.Ifc4.RepresentationResource;
using Xbim.IO.Memory;
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
			var allGeoModels = new List<IfcMeshModel>();
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
				WriteMidFile(allGeoModels, allGeoPointNormals, midFilePath);
				for (int i = 0; i < allGeoModels.Count; i++)
				{
					var tempModel = allGeoModels[i];
					geoIndexIfcIndexMap.Add(i, tempModel.IfcIndex);
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
		
		public void WriteMidFile(List<IfcMeshModel> meshModels, List<PointNormal> meshPoints, string midFilePath)
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
			foreach (var mesh in meshModels)
			{
				foreach (var item in mesh.FaceTriangles)
				{
					writer.Write(item.TriangleMaterial.Kd_R);
					writer.Write(item.TriangleMaterial.Kd_G);
					writer.Write(item.TriangleMaterial.Kd_B);
					writer.Write(item.TriangleMaterial.Ks_R);
					writer.Write(item.TriangleMaterial.Ks_G);
					writer.Write(item.TriangleMaterial.Ks_B);
					writer.Write(item.TriangleMaterial.K);
					writer.Write(item.TriangleMaterial.NS);
				}
			}
			if (null != ProgressChanged)
				ProgressChanged(this, new ProgressChangedEventArgs(100, "Convert End"));
			writer.Close();
			if (null != ProgressChanged)
				ProgressChanged(this, new ProgressChangedEventArgs(0, ""));
		}

	}
}
