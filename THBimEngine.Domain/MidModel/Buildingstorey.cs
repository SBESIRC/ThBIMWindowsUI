using System.Collections.Generic;
using System.IO;

namespace THBimEngine.Domain.MidModel
{
    public class Buildingstorey// 房屋楼层
	{
		public string floor_name;     // 楼层名
		public double elevation;           // 标高
		public double top_elevation;       // 顶高
		public double bottom_elevation;    // 底高
		public int stdFlrNo, floorNo;// 标准层号，层号
		public double height;      // 层高
		public List<int> element_index_s = new List<int>();// the start and the end of current building storey's elements' indices
		public List<int> element_index_e = new List<int>(); // group_id也就是一个构件一次while循环对应的iterator，每次循环增加1//多段连续的物件id（用于合模）
		public string description = "";
		public Dictionary<string, string> properties = new Dictionary<string, string>();  // 属性对

		public Buildingstorey(THBimStorey storey,ref int buildingIndex)
        {
			floor_name = storey.Name;
			elevation = storey.Elevation;
			top_elevation = storey.Elevation + storey.LevelHeight;
			bottom_elevation = storey.Elevation;
			stdFlrNo = buildingIndex;///
			floorNo = buildingIndex;///
			height = storey.LevelHeight;
			description = storey.Describe;

			buildingIndex++;
		}

		public Buildingstorey(Xbim.Ifc4.ProductExtension.IfcBuildingStorey storey, FloorPara floorPara)
        {
			floor_name = storey.Name;
			elevation = storey.Elevation.Value;
			height = floorPara.Height;
			top_elevation = elevation + height;
			bottom_elevation = elevation;
			stdFlrNo = floorPara.StdNum;///
			floorNo = floorPara.Num;///
			if (!(storey.Description is null))
				description = storey.Description;
		}

		public Buildingstorey(Xbim.Ifc2x3.ProductExtension.IfcBuildingStorey storey, FloorPara floorPara)
		{
			floor_name = storey.Name;
			elevation = storey.Elevation.Value;
			height = floorPara.Height;
			top_elevation = elevation + height;
			bottom_elevation = elevation;
			stdFlrNo = floorPara.StdNum;///
			floorNo = floorPara.Num;///
			if(!(storey.Description is null))
				description = storey.Description;
		}

		public void WriteToFile(BinaryWriter writer)
        {
			floor_name.WriteStr(writer);
			writer.Write(elevation);
			writer.Write(top_elevation);
			writer.Write(bottom_elevation);
			writer.Write(stdFlrNo);
			writer.Write(floorNo);
			writer.Write(height);
			writer.Write((ulong)element_index_s.Count);
			for(int i =0; i < element_index_s.Count;i++)
            {
                writer.Write(element_index_s[i]);
				writer.Write(element_index_e[i]);
			}
			writer.Write(properties.Count);
			foreach(var property in properties)
            {
				var key = property.Key;
				var value = property.Value;
				key.WriteStr(writer);
				value.WriteStr(writer);
			}
			description.WriteStr(writer);
		}
	}
}
