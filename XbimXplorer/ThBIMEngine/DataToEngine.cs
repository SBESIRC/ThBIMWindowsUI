using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using THBimEngine.Domain;
using THBimEngine.Domain.Grid;
using THBimEngine.Presention;

namespace XbimXplorer.ThBIMEngine
{
    class DataToEngine
    {
		public event ProgressChangedEventHandler ProgressChanged;
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



		public void WriteMidDataMultithreading(List<GeometryMeshModel> meshModels, List<PointNormal> meshPoints)
		{
			ExampleScene.ifcre_set_sleep_time(2000);
			ExampleScene.ifcre_clear_model_data();
			if (null == meshModels || null == meshPoints)
				return;
			List<Task> tasks = new List<Task>();
			tasks.Add(Task.Run(() =>
			{
				//vertices
				for (int i = 0; i < meshPoints.Count; i++)
				{
					var point = meshPoints[i];
					ExampleScene.ifcre_set_g_vertices(-point.Point.X / 1000);
					ExampleScene.ifcre_set_g_vertices(point.Point.Y / 1000);
					ExampleScene.ifcre_set_g_vertices(-point.Point.Z / 1000);
				}
			}));
			tasks.Add(Task.Run(() =>
			{
				//normals
				for (int i = 0; i < meshPoints.Count; i++)
				{
					var point = meshPoints[i];

					ExampleScene.ifcre_set_g_normals(Math.Abs(point.Normal.X) < 1e-6 ? 0 : -point.Normal.X);
					ExampleScene.ifcre_set_g_normals(Math.Abs(point.Normal.Z) < 1e-6 ? 0 : point.Normal.Z);
					ExampleScene.ifcre_set_g_normals(Math.Abs(point.Normal.Y) < 1e-6 ? 0 : -point.Normal.Y);
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

		public void PushGridDataToEngine(List<GridLine> gridLines, List<GridCircle> gridCircles, List<GridText> gridTexts)
		{
			ExampleScene.ifcre_set_grid_data(0);
			if (null != gridLines)
			{
				foreach (var gridLine in gridLines)
				{
					ExampleScene.ifcre_set_grid_lines((Math.Abs(gridLine.stPt.X / 1000) < 1e-6 ? 0 : -gridLine.stPt.X / 1000));
					ExampleScene.ifcre_set_grid_lines((Math.Abs(gridLine.stPt.Z / 1000) < 1e-6 ? 0 : gridLine.stPt.Z / 1000));
					ExampleScene.ifcre_set_grid_lines((Math.Abs(gridLine.stPt.Y / 1000) < 1e-6 ? 0 : -gridLine.stPt.Y / 1000));

					ExampleScene.ifcre_set_grid_lines((Math.Abs(gridLine.edPt.X / 1000) < 1e-6 ? 0 : -gridLine.edPt.X / 1000));
					ExampleScene.ifcre_set_grid_lines((Math.Abs(gridLine.edPt.Z / 1000) < 1e-6 ? 0 : gridLine.edPt.Z / 1000));
					ExampleScene.ifcre_set_grid_lines((Math.Abs(gridLine.edPt.Y / 1000) < 1e-6 ? 0 : -gridLine.edPt.Y / 1000));

					ExampleScene.ifcre_set_grid_lines(gridLine.color.r);
					ExampleScene.ifcre_set_grid_lines(gridLine.color.g);
					ExampleScene.ifcre_set_grid_lines(gridLine.color.b);
					ExampleScene.ifcre_set_grid_lines(gridLine.color.a);
					ExampleScene.ifcre_set_grid_lines(gridLine.width);
					ExampleScene.ifcre_set_grid_lines(gridLine.type);
				}
			}
			if (null != gridCircles)
			{
				foreach (var gridCircle in gridCircles)
				{
					ExampleScene.ifcre_set_grid_circles((Math.Abs(gridCircle.center.X / 1000) < 1e-6 ? 0 : -gridCircle.center.X / 1000));
					ExampleScene.ifcre_set_grid_circles((Math.Abs(gridCircle.center.Z / 1000) < 1e-6 ? 0 : gridCircle.center.Z / 1000));
					ExampleScene.ifcre_set_grid_circles((Math.Abs(gridCircle.center.Y / 1000) < 1e-6 ? 0 : -gridCircle.center.Y / 1000));

					ExampleScene.ifcre_set_grid_circles((Math.Abs(gridCircle.normal.X) < 1e-6 ? 0 : -gridCircle.normal.X));
					ExampleScene.ifcre_set_grid_circles((Math.Abs(gridCircle.normal.Z) < 1e-6 ? 0 : gridCircle.normal.Z));
					ExampleScene.ifcre_set_grid_circles((Math.Abs(gridCircle.normal.Y) < 1e-6 ? 0 : -gridCircle.normal.Y));

					ExampleScene.ifcre_set_grid_circles(gridCircle.color.r);
					ExampleScene.ifcre_set_grid_circles(gridCircle.color.g);
					ExampleScene.ifcre_set_grid_circles(gridCircle.color.b);
					ExampleScene.ifcre_set_grid_circles(gridCircle.color.a);
					ExampleScene.ifcre_set_grid_circles(gridCircle.radius / 1000);
					ExampleScene.ifcre_set_grid_circles(gridCircle.width);
				}
			}
			if (null != gridTexts)
			{
				foreach (var gridText in gridTexts)
				{
					ExampleScene.ifcre_set_grid_text(gridText.content.ToCharArray());
					ExampleScene.ifcre_set_grid_text_data(-gridText.center.X / 1000);
					ExampleScene.ifcre_set_grid_text_data(gridText.center.Z / 1000);
					ExampleScene.ifcre_set_grid_text_data(-gridText.center.Y / 1000);
					ExampleScene.ifcre_set_grid_text_data(gridText.normal.X);
					ExampleScene.ifcre_set_grid_text_data(-1.0f);
					ExampleScene.ifcre_set_grid_text_data(gridText.normal.Y);
					ExampleScene.ifcre_set_grid_text_data(-gridText.direction.X);
					ExampleScene.ifcre_set_grid_text_data(gridText.direction.Y);
					ExampleScene.ifcre_set_grid_text_data(-gridText.direction.Z);


					ExampleScene.ifcre_set_grid_text_data(gridText.color.Color_R);
					ExampleScene.ifcre_set_grid_text_data(gridText.color.Color_G);
					ExampleScene.ifcre_set_grid_text_data(gridText.color.Color_B);
					ExampleScene.ifcre_set_grid_text_data(1.0f);
					ExampleScene.ifcre_set_grid_text_data(gridText.size / 20000);
				}
			}
			ExampleScene.ifcre_set_grid_data(1);
		}
	}
}
