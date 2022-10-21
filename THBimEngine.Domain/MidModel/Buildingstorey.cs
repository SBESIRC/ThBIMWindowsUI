﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xbim.Ifc4.Interfaces;

namespace THBimEngine.Domain.MidModel
{
    public class Buildingstorey
	{
		public string floor_name;
		public double elevation;
		public double top_elevation;
		public double bottom_elevation;
		public int stdFlrNo, floorNo;
		public double height;
		public List<int> element_index_s = new List<int>();
		public List<int> element_index_e = new List<int>();
		public string description = "";
		public Dictionary<string, string> properties = new Dictionary<string, string>();

		public Buildingstorey(THBimStorey storey,int floorNum, int stdFloorIndex)
        {
			floor_name = storey.Name;
			elevation = storey.Elevation;
			top_elevation = storey.Elevation + storey.LevelHeight;
			bottom_elevation = storey.Elevation;
			stdFlrNo = stdFloorIndex;
			floorNo = floorNum;
			height = storey.LevelHeight;
			description = storey.Describe;
		}

		public Buildingstorey(IIfcBuildingStorey storey, FloorPara floorPara)
		{
			floor_name = storey.Name;
			if(storey.Elevation == null)
			{
                elevation = 0;
            }
            else
			{
                elevation = storey.Elevation.Value;
            }
            height = floorPara.Height;
			top_elevation = elevation + height;
			bottom_elevation = elevation;
			stdFlrNo = floorPara.StdNum;///
			floorNo = floorPara.Num;///
			if (!(storey.Description is null))
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

		public void WriteToTxt(StreamWriter sw)
		{
            sw.WriteLine($"storey name:\t {floor_name}");
            sw.WriteLine($"elevation:\t {elevation}");
            sw.WriteLine($"top_elevation:\t {top_elevation}");
            sw.WriteLine($"bottom_elevation:\t {bottom_elevation}");
            sw.WriteLine($"description:\t {description}");
            sw.WriteLine($"properties:");
            sw.WriteLine($"\tFloorNo:\t {floorNo}");
            sw.WriteLine($"\tHeight:\t {height}");
            sw.WriteLine($"\tStdFlrNo:\t {stdFlrNo}");
            sw.WriteLine($"------------------------------------");
        }
	}
}
