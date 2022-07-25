using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using THBimEngine.Domain;
using Xbim.Common;
using Xbim.Common.Geometry;
using Xbim.Common.XbimExtensions;
using Xbim.Ifc;
using Xbim.IO.Memory;
using Xbim.Presentation.LayerStyling;

namespace XbimXplorer.ThBIMEngine
{
    class IfcStoreReadGeomtry
    {
		public event ProgressChangedEventHandler ProgressChanged;
		protected List<IPersistEntity> ifcInstances;
		protected List<XbimShapeGeometry> shapeGeometries;
		protected List<XbimShapeInstance> shapeInstances;
		protected List<IPersistEntity> federatedInstances;
		protected IDictionary<int, List<XbimShapeInstance>> shapeGeoLoopups;
		protected IfcStore ifcModel;
		private ReadTaskInfo readTaskInfo;
		readonly XbimColourMap _colourMap = new XbimColourMap();
		public IfcStoreReadGeomtry(){}
		public List<IfcMeshModel> ReadGeomtry(IfcStore model,out List<PointNormal> allPointNormals) 
		{
			ifcModel = model;
			allPointNormals = new List<PointNormal>();
			readTaskInfo = new ReadTaskInfo();
			var excludedTypes = model.DefaultExclusions(null);
			ifcInstances = new List<IPersistEntity>();
			shapeInstances = new List<XbimShapeInstance>();
			shapeGeometries = new List<XbimShapeGeometry>();
			federatedInstances = new List<IPersistEntity>();
			shapeGeoLoopups = new Dictionary<int, List<XbimShapeInstance>>();
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
					if (readTaskInfo.AllCount < 1)
					{
						isContinue = false;
						break;
					}
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
			#endregion;
			allPointNormals.AddRange(readTaskInfo.AllPointVectors);
			return readTaskInfo.AllModels;
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
				var type = this.ifcModel.Metadata.ExpressType((short)insModel.IfcTypeId);
				var typeStr = type.Name.ToLower().Replace("ifc", "");
				var material = THBimMaterial.GetTHBimEntityMaterial(typeStr,true);
				if (typeStr.Contains("open"))
					continue;
				var allPts = tr.Vertices.ToArray();
				var allFace = tr.Faces;
				if (allFace.Count < 1)
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
		private XbimPoint3D TransPoint(XbimPoint3D xbimPoint, XbimMatrix3D xbimMatrix)
		{
			return xbimMatrix.Transform(xbimPoint);
		}
		private XbimVector3D TransVector(XbimVector3D xbimVector, XbimMatrix3D xbimMatrix)
		{
			return xbimMatrix.Transform(xbimVector);
		}
		protected IEnumerable<XbimShapeInstance> GetShapeInstancesToRender(IGeometryStoreReader geomReader, HashSet<short> excludedTypes)
		{
			var shapeInstances = geomReader.ShapeInstances
				.Where(s => s.RepresentationType == XbimGeometryRepresentationType.OpeningsAndAdditionsIncluded
							&&
							!excludedTypes.Contains(s.IfcTypeId));
			return shapeInstances;
		}
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
		public THBimMaterial TriangleMaterial { get; set; }
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