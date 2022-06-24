using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xbim.Common;
using Xbim.Common.Geometry;
using Xbim.Common.Metadata;
using Xbim.Ifc;
using Xbim.Ifc4.GeometricModelResource;
using Xbim.Ifc4.RepresentationResource;
using Xbim.Ifc4.SharedBldgElements;

namespace XbimXplorer.ThBIMEngine
{
	class IfcStoreToEngineFile
	{
		public event ProgressChangedEventHandler ProgressChanged;
		protected List<IPersistEntity> ifcInstances;
		protected List<XbimShapeGeometry> shapeGeometries;
		protected List<XbimShapeInstance> shapeInstances;
		protected IfcStore ifcModel;
		private ReadTaskInfo readTaskInfo;
		readonly XbimColourMap _colourMap = new XbimColourMap();
		private List<string> testListSting;
		private List<string> testTypeStr;
		
		public IfcStoreToEngineFile()
		{
			//TestReadFile();
			testListSting = new List<string>();
			testTypeStr = new List<string>();
		}
		public void TestReadFile()
		{
			FileStream fileStream = new FileStream(".\\temp3.midfile", FileMode.Open);
			BinaryReader binaryReader2 = new BinaryReader(fileStream, Encoding.UTF8);
			int testi = 0;
			//vertices
			var ptCount = binaryReader2.ReadUInt64();
			var ptValue = new List<float>();
			for (ulong i = 0; i < ptCount; i++)
			{
				ptValue.Add(binaryReader2.ReadSingle());
			}
			//normals
			//binaryReader2.ReadUInt64();//有换行或其它字符
			var normalCount = binaryReader2.ReadUInt64();
			var normalValue = new List<float>();
			for (ulong i = 0; i < normalCount; i++)
			{
				normalValue.Add(binaryReader2.ReadSingle());
			}
			//binaryReader2.ReadUInt64();//有换行或其它字符
			//global_indices
			var indexCount = binaryReader2.ReadUInt64();
			var intdexValue = new List<int>();
			for (ulong i = 0; i < indexCount; i++)
			{
				intdexValue.Add(binaryReader2.ReadInt32());
			}
			//components' indices
			var cIndexCount = binaryReader2.ReadUInt64();
			List<List<int>> ptIndex = new List<List<int>>();
			for (ulong i = 0; i < cIndexCount; i++)
			{
				//binaryReader2.ReadDouble();
				var innerCount = binaryReader2.ReadUInt64();
				List<int> tmpvc = new List<int>();
				for (ulong j = 0; j < innerCount; j++)
				{
					tmpvc.Add(binaryReader2.ReadInt32());
				}
				ptIndex.Add(tmpvc);
			}
			//material datas
			var mCount = binaryReader2.ReadUInt64();
			var mValues = new List<List<object>>();
			for (ulong i = 0; i < mCount; i++)
			{
				var value = new List<object>();
				for (int j = 0; j < 8; j++)
				{
					if (j == 7)
					{
						value.Add(binaryReader2.ReadInt32());
					}
					else
					{
						value.Add(binaryReader2.ReadSingle());
					}
				}
				mValues.Add(value);
			}
			if (testi > 0)
			{
			}
			fileStream.Close();
		}
		public void LoadGeometry(IfcStore model,string midFilePath)
		{
			if (null == model || model.Instances == null)
				return;
			ifcModel = model;
			ifcInstances = null;
			ifcInstances = model.Instances.ToList();
			shapeGeometries = new List<XbimShapeGeometry>();
			shapeInstances = new List<XbimShapeInstance>();
			readTaskInfo = new ReadTaskInfo();
			DateTime startTime = DateTime.Now;
			
			#region 单线程
			int pIndex = 0;
			var allPointVectors = new List<PointNormal>();
			var allModels = new List<IfcMeshModel>();
			
			using (var geomStore = model.GeometryStore)
			{
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
						if (null != item.LocalShapeDisplacement) 
						{
						
						}
						var material = GetMeshModelMaterial(item.IfcShapeLabel,item.ShapeLabel);
						count += 1;
						var allPts = item.Vertices.ToArray();
						var ptIndex = allPointVectors.Count;
						var mesh = new IfcMeshModel(item.ShapeLabel);
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
								var pt1 = allPts[pt1Index];
								var pt1Normal = face.Normals.Last();
								if (pt1Index < face.Normals.Count())
									pt1Normal = face.NormalAt(pt1Index);
								pIndex += 1;
								triangle.ptIndex.Add(pIndex);
								allPointVectors.Add(GetPointNormal(pIndex, pt1, pt1Normal));
								var pt2 = allPts[pt2Index];
								var pt2Normal = face.Normals.Last();
								if (pt2Index < face.Normals.Count())
									pt2Normal = face.NormalAt(pt2Index);
								pIndex += 1;
								triangle.ptIndex.Add(pIndex);
								allPointVectors.Add(GetPointNormal(pIndex, pt2, pt2Normal));
								var pt3 = allPts[pt3Index];
								var pt3Normal = face.Normals.Last();
								if (pt3Index < face.Normals.Count())
									pt3Normal = face.NormalAt(pt3Index);
								pIndex += 1;
								triangle.ptIndex.Add(pIndex);
								allPointVectors.Add(GetPointNormal(pIndex, pt3, pt3Normal));
								mesh.FaceTriangles.Add(triangle);
							}
						}

						allModels.Add(mesh);
					}
				}
			}
			if (null != ProgressChanged)
				ProgressChanged(this, new ProgressChangedEventArgs(100, "Read Shape End"));
			WriteMidFile(allModels, allPointVectors, midFilePath);
			#endregion
			/*
            #region 多线程
           
			using (var geomStore = model.GeometryStore)
			{
				using (var geomReader = geomStore.BeginRead())
				{
					var geoCount = geomReader.ShapeGeometries.Count();
					shapeGeometries.AddRange(geomReader.ShapeGeometries);
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
            #endregion*/
			DateTime endTime = DateTime.Now;
			var total = endTime - startTime;
		}

        private async Task MoreTaskReadAsync()
		{
			List<Task> tasks = new List<Task>();

			int size = 200;
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
					ReadGeometries(targetShapes,tempi,start,end);

				});
				tasks.Add(t);
			}
			await Task.WhenAll(tasks);
		}
		private void ReadGeometries(List<XbimShapeGeometry> targetShapes,int taskNum,int start,int end) 
		{
			int thisCount = end - start;
			int pIndex = 0;
			var thisPointVectors = new List<PointNormal>();
			var thisModels = new List<IfcMeshModel>();
			foreach (var item in targetShapes)
			{
				var material = GetMeshModelMaterial(item.IfcShapeLabel,item.ShapeLabel);
				var allPts = item.Vertices.ToArray();
				var mesh = new IfcMeshModel(item.ShapeLabel);
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
						var pt1 = allPts[pt1Index];
						var pt1Normal = face.Normals.Last();
						if (pt1Index < face.Normals.Count())
							pt1Normal = face.NormalAt(pt1Index);
						pIndex += 1;
						triangle.ptIndex.Add(pIndex);
						thisPointVectors.Add(GetPointNormal(pIndex, pt1, pt1Normal));
						var pt2 = allPts[pt2Index];
						var pt2Normal = face.Normals.Last();
						if (pt2Index < face.Normals.Count())
							pt2Normal = face.NormalAt(pt2Index);
						pIndex += 1;
						triangle.ptIndex.Add(pIndex);
						thisPointVectors.Add(GetPointNormal(pIndex, pt2, pt2Normal));
						var pt3 = allPts[pt3Index];
						var pt3Normal = face.Normals.Last();
						if (pt3Index < face.Normals.Count())
							pt3Normal = face.NormalAt(pt3Index);
						pIndex += 1;
						triangle.ptIndex.Add(pIndex);
						thisPointVectors.Add(GetPointNormal(pIndex, pt3, pt3Normal));
						mesh.FaceTriangles.Add(triangle);
					}
				}
				thisModels.Add(mesh);
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
		private PointNormal GetPointNormal(int pIndex, XbimPoint3D point , XbimVector3D normal) 
		{
			return new PointNormal
			{
				PointIndex = pIndex,
				Point = new PointVector() { X = (float)point.X, Y = (float)point.Z, Z = (float)point.Y },
				Normal = new PointVector() { X = (float)normal.X, Y = (float)normal.Z, Z = (float)normal.Y },
			};
		}
		private void WriteMidFile(List<IfcMeshModel> meshModels,List<PointNormal> meshPoints,string midFilePath) 
		{
			if (null == meshModels || meshModels.Count < 1
				|| null == meshPoints || meshPoints.Count<1)
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
			//global_indices
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
			//components' indices
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

		private IfcMaterial GetMeshModelMaterial(int ifcLable,int shapeLable) 
		{
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
			var insModel = shapeInstances.Find(c => c.ShapeGeometryLabel == shapeLable);
			var type = this.ifcModel.Metadata.ExpressType((short)insModel.IfcTypeId);
			var typeStr = type.ExpressName.ToLower();
			var v = _colourMap[type.Name];
			if (typeStr.Contains("window")) 
			{
			
			}
			defalutMaterial = new IfcMaterial
			{
				Kd_R = v.Red,
				Kd_G = v.Green,
				Kd_B = v.Blue,
				Ks_R = v.ReflectionFactor,
				Ks_G = v.SpecularFactor,
				Ks_B = v.DiffuseFactor,
				K = v.Alpha,
				NS = (int)v.DiffuseTransmissionFactor,
			};
			return defalutMaterial;
			/*
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
			return defalutMaterial;*/
		}
		private string GetTypeName(IPersistEntity entity) 
		{
			if (entity is Xbim.Ifc2x3.RepresentationResource.IfcRepresentation ifcRep)
			{
				if (ifcRep.OfProductRepresentation.Count() > 0)
				{
					return GetTypeName(ifcRep.OfProductRepresentation.Last());
				}
			}
			else if (entity is IfcRepresentation ifcRep4)
			{
				if (ifcRep4.OfProductRepresentation.Count() > 0)
				{
					return GetTypeName(ifcRep4.OfProductRepresentation.Last());
				}
			}
			else if (entity is Xbim.Ifc2x3.RepresentationResource.IfcProductDefinitionShape shape)
			{
				if (shape.ShapeOfProduct.Count() > 0)
					return GetTypeName(shape.ShapeOfProduct.Last());
			}
			else if (entity is IfcProductDefinitionShape shape4)
			{
				if (shape4.ShapeOfProduct.Count() > 0)
					return GetTypeName(shape4.ShapeOfProduct.Last());
			}
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
			public int CIndex { get; }
			public List<FaceTriangle> FaceTriangles { get;}
			public IfcMeshModel(int index) 
			{
				CIndex = index;
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
