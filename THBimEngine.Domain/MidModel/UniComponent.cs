using System.Collections.Generic;
using System.IO;
using System.Text;

namespace THBimEngine.Domain.MidModel
{
    public class UniComponent: Component// 各个独一无二的物件
	{
		public int unique_id;      // 独一无二的id
		public string guid;

		public int tri_ind_s;      // 当前物件的三角面起始索引
		public int tri_ind_e;      // 当前物件的三角面结束索引
		public int edge_ind_s;         // 当前物件的边起始索引
		public int edge_ind_e;         // 当前物件的边结束索引
		public string floor_name; // 楼层名
		public int floor_num;      // 楼层序号
		public double[] rgb;//use for SkecthUp-out's ifc, to determine the priority

		public double depth= 5300.00000000;       // depth = z_r - z_l 深度值，由bbx决定
		public double depth_t= 5300.00000000;     // z_len？？？？？？？？？？？？？？？？？？？？？？？？？
		public double x_len=0.0;
		public double y_len=0.0;
		public double x_l, x_r;    // bbx信息
		public double y_l, y_r;    // bbx信息	
		public double z_l, z_r;    // bbx信息
		public double bg= -1.7976931348623157e+308;          // bbx信息
		public double[] direction=new double[3] {0.0,0.0,0.0};//if the component is IfcDoor, then the direction maps into 2D
		public string material="";       // 材质名称
		public string openmethod="";     // 开启方式
		public string description="";    // 描述信息
		public Dictionary<string, string> properties=new Dictionary<string, string>();  // 属性信息string to string
		public double _height=5300;
		public double _width = 5300;
		public int OpenDirIndex;
		public string comp_name="ifc";  // 物件名


		public UniComponent(THBimElementRelation bimRelation,THBimMaterial bimMaterial, ref int uniComponentIndex, Buildingstorey buildingStorey) : base("",0)
		{
			unique_id = uniComponentIndex;
			uniComponentIndex++;
			guid = bimRelation.RelationElementUid;

			floor_name = buildingStorey.floor_name;
			floor_num = buildingStorey.floorNo;
			rgb = new double[3] { bimMaterial.KS_R, bimMaterial.KS_G, bimMaterial.KS_B };
		}

		public UniComponent(string uid, THBimMaterial bimMaterial, ref int uniComponentIndex, Buildingstorey buildingStorey,Component component) : base("", 0)
		{
			unique_id = uniComponentIndex;
			uniComponentIndex++;
			guid = uid;

			floor_name = buildingStorey.floor_name;
			floor_num = buildingStorey.floorNo;
			rgb = new double[3] { bimMaterial.Color_R, bimMaterial.Color_G, bimMaterial.Color_B };

			name = component.name;
			type_id = component.type_id;
		}

		public new void WriteToFile(BinaryWriter writer)
        {
			writer.Write((ulong)name.Length);
			writer.Write(name.ToCharArray());
			writer.Write(type_id);
			color.Write(writer);
			writer.Write(hori);
			writer.Write(unique_id);
			writer.Write(depth);
			writer.Write(depth_t);
			writer.Write(x_len);
			writer.Write(y_len);
			writer.Write((ulong)floor_name.Length);
			writer.Write(floor_name.ToCharArray());
			writer.Write((ulong)material.Length);
			writer.Write(material.ToCharArray());
			writer.Write((ulong)openmethod.Length);
			writer.Write(openmethod.ToCharArray());
			writer.Write((ulong)description.Length);
			writer.Write(description.ToCharArray());
			writer.Write((System.UInt64)guid.Length);
			writer.Write(guid.ToCharArray());
			writer.Write(tri_ind_s);
			writer.Write(tri_ind_e);
			writer.Write(x_l);
			writer.Write(x_r);
			writer.Write(y_l);
			writer.Write(y_r);
			writer.Write(z_l);
			writer.Write(z_r);
			writer.Write(bg);
			writer.Write(floor_num);
			writer.Write(direction[0]);
			writer.Write(direction[1]);
			writer.Write(direction[2]);

			writer.Write(rgb[0]);
			writer.Write(rgb[1]);
			writer.Write(rgb[2]);

			writer.Write(_height);
			writer.Write(_width);
			writer.Write(properties.Count);

			foreach (var property in properties)
            {
				var key = property.Key;
				var value = property.Value;
				writer.Write((System.UInt64)key.Length);
				writer.Write(key.ToCharArray());
				writer.Write((System.UInt64)value.Length);
				writer.Write(value.ToCharArray());
			}
			writer.Write(OpenDirIndex);
			writer.Write((System.UInt64)comp_name.Length);
			writer.Write(comp_name.ToCharArray());
			writer.Write(edge_ind_s);
			writer.Write(edge_ind_e);
		}
	}
}
