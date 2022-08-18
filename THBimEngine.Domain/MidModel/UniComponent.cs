using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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



		public double depth;       // depth = z_r - z_l 深度值，由bbx决定
		public double depth_t;     // z_len？？？？？？？？？？？？？？？？？？？？？？？？？
		public double x_len;
		public double y_len;
		public double x_l, x_r;    // bbx信息
		public double y_l, y_r;    // bbx信息	
		public double z_l, z_r;    // bbx信息
		public double bg;          // bbx信息
		public double[] direction;//if the component is IfcDoor, then the direction maps into 2D
		public string material;       // 材质名称
		public string openmethod;     // 开启方式
		public string description;    // 描述信息
		public Dictionary<string, string> properties;  // 属性信息string to string
		public double _height;
		public double _width;
		public int OpenDirIndex;
		public string comp_name;  // 物件名


		public UniComponent(THBimElementRelation bimRelation,THBimMaterial bimMaterial, ref int uniComponentIndex, Buildingstorey buildingStorey) : base("",0)
		{
			unique_id = uniComponentIndex;
			uniComponentIndex++;
			guid = bimRelation.RelationElementUid;

			floor_name = buildingStorey.floor_name;
			floor_num = buildingStorey.floorNo;
			rgb = new double[3] { bimMaterial.KS_R, bimMaterial.KS_G, bimMaterial.KS_B };
		}

		public UniComponent(string uid, THBimMaterial bimMaterial, ref int uniComponentIndex, Buildingstorey buildingStorey) : base("", 0)
		{
			unique_id = uniComponentIndex;
			uniComponentIndex++;
			guid = uid;

			floor_name = buildingStorey.floor_name;
			floor_num = buildingStorey.floorNo;
			rgb = new double[3] { bimMaterial.KS_R, bimMaterial.KS_G, bimMaterial.KS_B };
		}
	}
}
