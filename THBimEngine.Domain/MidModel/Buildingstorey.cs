using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace THBimEngine.Domain.MidModel
{
    public class Buildingstorey// 房屋楼层
	{
		public string floor_name;     // 楼层名
		public double elevation;           // 标高
		public double top_elevation;       // 顶高
		public double bottom_elevation;    // 底高
		public int stdFlrNo, floorNo;// new feature of building storey
		public double height;      // 层高
		public List<int> element_index_s, element_index_e;// the start and the end of current building storey's elements' indices // group_id也就是一个构件一次while循环对应的iterator，每次循环增加1//多段连续的物件id（用于合模）
		public string description;
		public Dictionary<string, string> properties;  // 属性对

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

		public Buildingstorey(Xbim.Ifc4.ProductExtension.IfcBuildingStorey storey, ref int buildingIndex)
        {
			floor_name = storey.Name;
			elevation = storey.Elevation.Value;
			top_elevation = storey.Elevation.Value;// + storey.LevelHeight;
			bottom_elevation = storey.Elevation.Value;
			stdFlrNo = buildingIndex;///
			floorNo = buildingIndex;///
			height = 0;
			description = storey.Description;

			buildingIndex++;
		}

		public Buildingstorey(Xbim.Ifc2x3.ProductExtension.IfcBuildingStorey storey, ref int buildingIndex)
		{
			floor_name = storey.Name;
			if(!(storey.Elevation is null))
            {
				elevation = storey.Elevation.Value;
				top_elevation = storey.Elevation.Value;// + storey.LevelHeight;
				bottom_elevation = storey.Elevation.Value;
			}
	
			stdFlrNo = buildingIndex;///
			floorNo = buildingIndex;///
			height = 0;
			description = storey.Description;

			buildingIndex++;
		}

		public void WriteToFile(BinaryWriter writer)
        {
			writer.Write(floor_name.Length);
			writer.Write(floor_name);
			writer.Write(elevation);
			writer.Write(top_elevation);
			writer.Write(bottom_elevation);
			writer.Write(stdFlrNo);
			writer.Write(floorNo);
			writer.Write(height);
			writer.Write(element_index_s.Count);
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
				writer.Write(key.Length);
				writer.Write(key);
				writer.Write(value.Length);
				writer.Write(value);
			}
			writer.Write(description.Count());
			writer.Write(description);
		}
	}
}
