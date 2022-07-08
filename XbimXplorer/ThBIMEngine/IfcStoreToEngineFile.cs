using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xbim.Common;
using Xbim.Common.Geometry;
using Xbim.Common.XbimExtensions;
using Xbim.Ifc;
using Xbim.Ifc4.RepresentationResource;
using Xbim.IO.Memory;
using Xbim.Presentation.LayerStyling;

namespace XbimXplorer.ThBIMEngine
{
	class IfcStoreToEngineFile
	{
		public event ProgressChangedEventHandler ProgressChanged;
		protected List<IPersistEntity> ifcInstances;
		protected List<XbimShapeGeometry> shapeGeometries;
		protected List<XbimShapeInstance> shapeInstances;
		protected IDictionary<int, List<XbimShapeInstance>> shapeGeoLoopups;
		protected IfcStore ifcModel;
		private ReadTaskInfo readTaskInfo;
		readonly XbimColourMap _colourMap = new XbimColourMap();
		private Dictionary<int, int> geoIndexIfcIndexMap;
		public IfcStoreToEngineFile()
		{
		}
		public Dictionary<int, int> LoadGeometry(IfcStore model, string midFilePath)
		{
			var excludedTypes = model.DefaultExclusions(null);
			geoIndexIfcIndexMap = new Dictionary<int, int>();
			if (null == model || model.Instances == null)
				return geoIndexIfcIndexMap;
			ifcModel = model;
			ifcInstances = null;
			ifcInstances = model.Instances.ToList();
			shapeGeometries = new List<XbimShapeGeometry>();
			shapeInstances = new List<XbimShapeInstance>();
			shapeGeoLoopups = new Dictionary<int, List<XbimShapeInstance>>();
			readTaskInfo = new ReadTaskInfo();
            #region 单线程
            /*
			int pIndex = 0;
			var allPointVectors = new List<PointNormal>();
			var allModels = new List<IfcMeshModel>();
			
			using (var geomStore = model.GeometryStore)
			{
				var shapeGeoLoopups = ((Xbim.Common.Model.InMemoryGeometryStore)geomStore).GeometryShapeLookup;
				using (var geomReader = geomStore.BeginRead())
				{
					var geoCount = geomReader.ShapeGeometries.Count();
					shapeGeometries.AddRange(geomReader.ShapeGeometries);
					shapeInstances.AddRange(geomReader.ShapeInstances);

					int count = 1;
					foreach (var item in geomReader.ShapeGeometries)
					{
						if (null != ProgressChanged)
						{
							var currentProgress = count * 100 / geoCount;
							ProgressChanged(this, new ProgressChangedEventArgs(currentProgress, "Read ShapeGeometries"));
						}
						var insModel = shapeInstances.Find(c => c.ShapeGeometryLabel == item.ShapeLabel);
						var material = GetMeshModelMaterial(insModel,item.IfcShapeLabel,item.ShapeLabel);
						var allValues = shapeGeoLoopups[item.ShapeLabel];
						var allPts = item.Vertices.ToArray();
						var ptIndex = allPointVectors.Count;
						int tempCount = 1;
						foreach (var copyModel in allValues) 
						{
							if (item.FaceCount < 1)
								continue;
							var transform = copyModel.Transformation;
							count += 1;
							var mesh = new IfcMeshModel(item.ShapeLabel + tempCount,item.IfcShapeLabel);
							foreach (var face in item.Faces.OfType<WexBimMeshFace>().ToList())
							{
								var ptIndexs = face.Indices.ToArray();
								for (int i = 0; i < face.TriangleCount; i++)
								{
									var triangle = new FaceTriangle();
									triangle.TriangleMaterial = material;
									var pt1Index = ptIndexs[i * 3];
									var pt2Index = ptIndexs[i * 3 + 1];
									var pt3Index = ptIndexs[i * 3 + 2];
									var pt1 = TransPoint(allPts[pt1Index], transform);
									var pt1Normal = face.Normals.Last();
									if (pt1Index < face.Normals.Count())
										pt1Normal = face.NormalAt(pt1Index);
									pt1Normal = TransVector(pt1Normal, transform);
									pIndex += 1;
									triangle.ptIndex.Add(pIndex);
									allPointVectors.Add(GetPointNormal(pIndex, pt1, pt1Normal));
									var pt2 = TransPoint(allPts[pt2Index], transform);
									var pt2Normal = face.Normals.Last();
									if (pt2Index < face.Normals.Count())
										pt2Normal = face.NormalAt(pt2Index);
									pt2Normal = TransVector(pt2Normal, transform);
									pIndex += 1;
									triangle.ptIndex.Add(pIndex);
									allPointVectors.Add(GetPointNormal(pIndex, pt2, pt2Normal));
									var pt3 = TransPoint(allPts[pt3Index], transform);
									var pt3Normal = face.Normals.Last();
									if (pt3Index < face.Normals.Count())
										pt3Normal = face.NormalAt(pt3Index);
									pt3Normal = TransVector(pt3Normal, transform);
									pIndex += 1;
									triangle.ptIndex.Add(pIndex);
									allPointVectors.Add(GetPointNormal(pIndex, pt3, pt3Normal));
									mesh.FaceTriangles.Add(triangle);
								}
							}
							allModels.Add(mesh);
							tempCount += 1;
						}
					}
				}
			}
			if (null != ProgressChanged)
				ProgressChanged(this, new ProgressChangedEventArgs(100, "Read Shape End"));
			WriteMidFile(allModels, allPointVectors, midFilePath);*/
            #endregion

            #region 多线程

            using (var geomStore = model.GeometryStore)
            {
                if (geomStore is InMemoryGeometryStore meyGeoStore)
                {
                    shapeGeoLoopups = ((InMemoryGeometryStore)geomStore).GeometryShapeLookup;
                }
                using (var geomReader = geomStore.BeginRead())
                {
                    var tempIns = GetShapeInstancesToRender(geomReader, excludedTypes);
                    var geoCount = geomReader.ShapeGeometries.Count();
                    shapeGeometries.AddRange(geomReader.ShapeGeometries);
                    shapeInstances.AddRange(tempIns);
                }
            }
            var task = MoreTaskReadAsync();
            bool isContinue = true;
            while (isContinue)
            {
                Thread.Sleep(2000);
                lock (readTaskInfo)
                {
                    if (readTaskInfo.TaskCount < 1)
                    {
                        isContinue = false;
                    }
                    if (null != ProgressChanged)
                    {
                        var currentProgress = readTaskInfo.ReadCount * 100 / readTaskInfo.AllCount;
                        ProgressChanged(this, new ProgressChangedEventArgs(currentProgress, "Reading ShapeGeometries"));
                    }
                }
            }
            if (null != ProgressChanged)
                ProgressChanged(this, new ProgressChangedEventArgs(100, "Reading Shape End"));
            WriteMidFile(readTaskInfo.AllModels, readTaskInfo.AllPointVectors, midFilePath);
            #endregion
            for (int i = 0; i < readTaskInfo.AllModels.Count; i++)
			{
				var tempModel = readTaskInfo.AllModels[i];
				geoIndexIfcIndexMap.Add(i, tempModel.IfcIndex);
			}
			return geoIndexIfcIndexMap;
		}
		private XbimPoint3D TransPoint(XbimPoint3D xbimPoint, XbimMatrix3D xbimMatrix)
		{
			return xbimMatrix.Transform(xbimPoint);
		}
		protected IEnumerable<XbimShapeInstance> GetShapeInstancesToRender(IGeometryStoreReader geomReader, HashSet<short> excludedTypes)
		{
			var shapeInstances = geomReader.ShapeInstances
				.Where(s => s.RepresentationType == XbimGeometryRepresentationType.OpeningsAndAdditionsIncluded
							&&
							!excludedTypes.Contains(s.IfcTypeId));
			return shapeInstances;
		}
		private XbimVector3D TransVector(XbimVector3D xbimVector, XbimMatrix3D xbimMatrix)
		{
			return xbimMatrix.Transform(xbimVector);
		}
		private async Task MoreTaskReadAsync()
		{
			List<Task> tasks = new List<Task>();

			int size = 100;
			int count = shapeGeometries.Count;
			int taskCount = (int)Math.Ceiling((double)count / size);
			readTaskInfo.AllCount = count;
			readTaskInfo.TaskCount = taskCount;
			readTaskInfo.AllTaskCount = taskCount;

			for (int j = 0; j < taskCount; j++)
			{
				var tempi = j;
				var t = Task.Run(() =>
				{
					var targetShapes = new List<XbimShapeGeometry>();

					int start = tempi * size,
						end = (tempi + 1) * size,
						thisSize = size;
					lock (shapeGeometries)
					{
						if (end > shapeGeometries.Count)
						{
							thisSize = shapeGeometries.Count - start;
							end = shapeGeometries.Count;
						}
					}
					targetShapes.AddRange(shapeGeometries.GetRange(start, thisSize));
					ReadGeometries(targetShapes, tempi, start, end);

				});
				tasks.Add(t);
			}
			await Task.WhenAll(tasks);
		}
        private void ReadGeometries(List<XbimShapeGeometry> targetShapes, int taskNum, int start, int end)
        {
            int thisCount = end - start;
            int pIndex = 0;
            var thisPointVectors = new List<PointNormal>();
            var thisModels = new List<IfcMeshModel>();
            var intGeoCount = 0;
			foreach (var item in targetShapes)
			{
				var insModel = shapeInstances.Find(c => c.ShapeGeometryLabel == item.ShapeLabel);
				if (insModel == null)
				{
					continue;
				}
				var iGeo = item as IXbimShapeGeometryData;
				var ms = new MemoryStream(iGeo.ShapeData);
				var br = new BinaryReader(ms);
				var tr = br.ReadShapeTriangulation();
				if (tr.Faces.Count < 1)
					continue;
				var material = GetMeshModelMaterial(insModel, item.IfcShapeLabel, item.ShapeLabel, out string typeStr);
				if (typeStr.Contains("open"))
					continue;
				var allPts = tr.Vertices.ToArray();
				var allFace = tr.Faces;
                if (allFace.Count < 1)
                    continue;
                if (typeStr.Contains("open"))
                    continue;
                if (shapeGeoLoopups.ContainsKey(item.ShapeLabel))
                {
                    var allValues = shapeGeoLoopups[item.ShapeLabel];
                    int tempCount = 1;
                    foreach (var copyModel in allValues)
                    {
                        var transform = copyModel.Transformation;
                        var mesh = new IfcMeshModel(intGeoCount + tempCount, copyModel.IfcProductLabel);
                        foreach (var face in allFace.ToList())
                        {
                            var ptIndexs = face.Indices.ToArray();
                            for (int i = 0; i < face.TriangleCount; i++)
                            {
                                var triangle = new FaceTriangle();
                                triangle.TriangleMaterial = material;
                                var pt1Index = ptIndexs[i * 3];
                                var pt2Index = ptIndexs[i * 3 + 1];
                                var pt3Index = ptIndexs[i * 3 + 2];
                                var pt1 = TransPoint(allPts[pt1Index], transform);
                                var pt1Normal = face.Normals.Last().Normal;
								if (pt1Index < face.Normals.Count())
									pt1Normal = face.Normals[pt1Index].Normal;
                                pIndex += 1;
                                pt1Normal = TransVector(pt1Normal, transform);
                                triangle.ptIndex.Add(pIndex);
                                thisPointVectors.Add(GetPointNormal(pIndex, pt1, pt1Normal));
                                var pt2 = TransPoint(allPts[pt2Index], transform);
                                var pt2Normal = face.Normals.Last().Normal;
								if (pt2Index < face.Normals.Count())
                                    pt2Normal = face.Normals[pt2Index].Normal;
								pIndex += 1;
                                pt2Normal = TransVector(pt2Normal, transform);
                                triangle.ptIndex.Add(pIndex);
                                thisPointVectors.Add(GetPointNormal(pIndex, pt2, pt2Normal));
                                var pt3 = TransPoint(allPts[pt3Index], transform);
                                var pt3Normal = face.Normals.Last().Normal;
								if (pt3Index < face.Normals.Count())
                                    pt3Normal = face.Normals[pt3Index].Normal;
								pIndex += 1;
                                pt3Normal = TransVector(pt3Normal, transform);
                                triangle.ptIndex.Add(pIndex);
                                thisPointVectors.Add(GetPointNormal(pIndex, pt3, pt3Normal));
                                mesh.FaceTriangles.Add(triangle);
                            }
                        }
                        thisModels.Add(mesh);
                    }
                }
                else
                {
                    var transform = insModel.Transformation;
                    var mesh = new IfcMeshModel(intGeoCount, insModel.IfcProductLabel);
                    foreach (var face in allFace.ToList())
                    {
                        var ptIndexs = face.Indices.ToArray();
                        for (int i = 0; i < face.TriangleCount; i++)
                        {
                            var triangle = new FaceTriangle();
                            triangle.TriangleMaterial = material;
                            var pt1Index = ptIndexs[i * 3];
                            var pt2Index = ptIndexs[i * 3 + 1];
                            var pt3Index = ptIndexs[i * 3 + 2];
                            var pt1 = TransPoint(allPts[pt1Index], transform);
							var pt1Normal = face.Normals.Last().Normal;
							if (pt1Index < face.Normals.Count())
								pt1Normal = face.Normals[pt1Index].Normal;
							pIndex += 1;
                            pt1Normal = TransVector(pt1Normal, transform);
                            triangle.ptIndex.Add(pIndex);
                            thisPointVectors.Add(GetPointNormal(pIndex, pt1, pt1Normal));
                            var pt2 = TransPoint(allPts[pt2Index], transform);
							var pt2Normal = face.Normals.Last().Normal;
							if (pt2Index < face.Normals.Count())
								pt2Normal = face.Normals[pt2Index].Normal;
							pIndex += 1;
                            pt2Normal = TransVector(pt2Normal, transform);
                            triangle.ptIndex.Add(pIndex);
                            thisPointVectors.Add(GetPointNormal(pIndex, pt2, pt2Normal));
                            var pt3 = TransPoint(allPts[pt3Index], transform);
							var pt3Normal = face.Normals.Last().Normal;
							if (pt3Index < face.Normals.Count())
								pt3Normal = face.Normals[pt3Index].Normal;
							pIndex += 1;
                            pt3Normal = TransVector(pt3Normal, transform);
                            triangle.ptIndex.Add(pIndex);
                            thisPointVectors.Add(GetPointNormal(pIndex, pt3, pt3Normal));
                            mesh.FaceTriangles.Add(triangle);
                        }
                    }
                    thisModels.Add(mesh);
                }

                intGeoCount += 1;
            }
            lock (readTaskInfo)
            {
                readTaskInfo.ReadCount += thisCount;
                var ptOffSet = readTaskInfo.AllPointVectors.Count;
                foreach (var item in thisPointVectors)
                {
                    item.PointIndex += ptOffSet;
                }
                foreach (var item in thisModels)
                {
                    item.CIndex += ptOffSet;
                    foreach (var tr in item.FaceTriangles)
                    {
                        for (int i = 0; i < tr.ptIndex.Count; i++)
                            tr.ptIndex[i] += ptOffSet;
                    }
                }
                readTaskInfo.AllPointVectors.AddRange(thisPointVectors);
                readTaskInfo.AllModels.AddRange(thisModels);
                readTaskInfo.TaskCount -= 1;
            }
        }
        private PointNormal GetPointNormal(int pIndex, XbimPoint3D point, XbimVector3D normal)
		{
			return new PointNormal
			{
				PointIndex = pIndex,
				Point = new PointVector() { X = -(float)point.X, Y = (float)point.Z, Z = (float)point.Y },
				Normal = new PointVector() { X = -(float)normal.X, Y = (float)normal.Z, Z = (float)normal.Y },
			};
		}
		private void WriteMidFile(List<IfcMeshModel> meshModels, List<PointNormal> meshPoints, string midFilePath)
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

		private IfcMaterial GetMeshModelMaterial(XbimShapeInstance insModel, int ifcLable, int shapeLable, out string typeStr)
		{
			typeStr = "";
			var defalutMaterial = new IfcMaterial
			{
				Kd_R = 169 / 255f,
				Kd_G = 179 / 255f,
				Kd_B = 218 / 255f,
				Ks_R = 0,
				Ks_B = 0,
				Ks_G = 0,
				K = 0.5f,
				NS = 12,
			};
			//var ifcModel = ifcInstances[ifcLable];
			//var insModel = shapeInstances.Find(c => c.ShapeGeometryLabel == shapeLable);
			var type = this.ifcModel.Metadata.ExpressType((short)insModel.IfcTypeId);
			typeStr = type.ExpressName.ToLower();
			var v = _colourMap[type.Name];
			if (typeStr.Contains("window"))
			{

			}
			else if (typeStr.Contains("open"))
			{

			}
			/*
			defalutMaterial = new IfcMaterial
			{
				Kd_R = v.Red,
				Kd_G = v.Green,
				Kd_B = v.Blue,
				Ks_R = v.DiffuseFactor,
				Ks_G = v.SpecularFactor,
				Ks_B = v.DiffuseTransmissionFactor,
				K = v.Alpha,
				NS = 12,
			};
			return defalutMaterial;*/

			//testListSting.Add(ifcModel.GetType().ToString().ToLower());
			//testTypeStr.Add(typeStr);
			if (typeStr.Contains("wall"))
			{
				defalutMaterial = new IfcMaterial
				{
					Kd_R = 226 / 255f,
					Kd_G = 212 / 255f,
					Kd_B = 190 / 255f,
					Ks_R = 0,
					Ks_B = 0,
					Ks_G = 0,
					K = 1f,
					NS = 12,
				};
			}
			else if (typeStr.Contains("beam"))
			{
				defalutMaterial = new IfcMaterial
				{
					Kd_R = 194 / 255f,
					Kd_G = 178 / 255f,
					Kd_B = 152 / 255f,
					Ks_R = 0,
					Ks_B = 0,
					Ks_G = 0,
					K = 1f,
					NS = 12,
				};
			}
			else if (typeStr.Contains("door"))
			{
				defalutMaterial = new IfcMaterial
				{
					Kd_R = 167 / 255f,
					Kd_G = 182 / 255f,
					Kd_B = 199 / 255f,
					Ks_R = 0,
					Ks_B = 0,
					Ks_G = 0,
					K = 1f,
					NS = 12,
				};
			}
			else if (typeStr.Contains("slab"))
			{
				defalutMaterial = new IfcMaterial
				{
					Kd_R = 167 / 255f,
					Kd_G = 182 / 255f,
					Kd_B = 199 / 255f,
					Ks_R = 0,
					Ks_B = 0,
					Ks_G = 0,
					K = 1f,
					NS = 12,
				};
			}
			else if (typeStr.Contains("window"))
			{
				defalutMaterial = new IfcMaterial
				{
					Kd_R = 116 / 255f,
					Kd_G = 195 / 255f,
					Kd_B = 219 / 255f,
					Ks_R = 0,
					Ks_B = 0,
					Ks_G = 0,
					K = 0.5f,
					NS = 12,
				};
			}
			else if (typeStr.Contains("column"))
			{
				defalutMaterial = new IfcMaterial
				{
					Kd_R = 171 / 255f,
					Kd_G = 157 / 255f,
					Kd_B = 135 / 255f,
					Ks_R = 0,
					Ks_B = 0,
					Ks_G = 0,
					K = 1f,
					NS = 12,
				};
			}
			else if (typeStr.Contains("railing"))
			{
				defalutMaterial = new IfcMaterial { Kd_R = 136 / 255f, Kd_G = 211 / 255f, Kd_B = 198 / 255f, Ks_R = 0, Ks_B = 0, Ks_G = 0, K = 0.5f, NS = 12, };
			}
			else if (typeStr.Contains("open"))
			{

			}
			else if (typeStr.Contains("ifcmaterial"))
			{

			}
			else
			{

			}
			return defalutMaterial;
		}
		private string GetTypeName(IPersistEntity entity)
		{
			//if (entity is Xbim.Ifc2x3.RepresentationResource.IfcRepresentation ifcRep)
			//{
			//	if (ifcRep.OfProductRepresentation.Count() > 0)
			//	{
			//		return GetTypeName(ifcRep.OfProductRepresentation.Last());
			//	}
			//}
			//else if (entity is IfcRepresentation ifcRep4)
			//{
			//	if (ifcRep4.OfProductRepresentation.Count() > 0)
			//	{
			//		return GetTypeName(ifcRep4.OfProductRepresentation.Last());
			//	}
			//}
			//else if (entity is Xbim.Ifc2x3.RepresentationResource.IfcProductDefinitionShape shape)
			//{
			//	if (shape.ShapeOfProduct.Count() > 0)
			//		return GetTypeName(shape.ShapeOfProduct.Last());
			//}
			//else if (entity is IfcProductDefinitionShape shape4)
			//{
			//	if (shape4.ShapeOfProduct.Count() > 0)
			//		return GetTypeName(shape4.ShapeOfProduct.Last());
			//}
			return entity.GetType().ToString();
		}
		class PointNormal
		{
			public int PointIndex { get; set; }
			public PointVector Point { get; set; }
			public PointVector Normal { get; set; }

		}
		class FaceTriangle
		{
			public List<int> ptIndex { get; }
			public IfcMaterial TriangleMaterial { get; set; }
			public FaceTriangle()
			{
				ptIndex = new List<int>();
			}
		}
		class IfcMeshModel
		{
			public int CIndex { get; set; }
			public int IfcIndex { get; }
			public List<FaceTriangle> FaceTriangles { get; }
			public IfcMeshModel(int index, int ifcIndex)
			{
				CIndex = index;
				IfcIndex = ifcIndex;
				FaceTriangles = new List<FaceTriangle>();
			}
		}
		class PointVector
		{
			public float X { get; set; }
			public float Y { get; set; }
			public float Z { get; set; }
		}
		class IfcMaterial
		{
			public float Kd_R { get; set; }
			public float Kd_G { get; set; }
			public float Kd_B { get; set; }
			public float Ks_R { get; set; }
			public float Ks_G { get; set; }
			public float Ks_B { get; set; }
			public float K { get; set; }
			public int NS { get; set; }
		}

		class ReadTaskInfo
		{
			public int AllCount { get; set; }
			public int ReadCount { get; set; }
			public int TaskCount { get; set; }
			public int AllTaskCount { get; set; }
			public List<PointNormal> AllPointVectors { get; } = new List<PointNormal>();
			public List<IfcMeshModel> AllModels { get; } = new List<IfcMeshModel>();
		}
	}
}
