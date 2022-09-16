using System.Collections.Generic;
using System.IO;

namespace THBimEngine.Domain.MidModel
{
    public class UniComponent : Component
    {
        public int unique_id;
        public string guid;
        public int tri_ind_s;
        public int tri_ind_e;
        public int edge_ind_s;
        public int edge_ind_e;
        public string floor_name;
        public int floor_num;
        public double[] rgb;
        public double depth = 0;
        public double depth_t = 0;
        public double x_len = 0.0;
        public double y_len = 0.0;
        public double x_l, x_r;
        public double y_l, y_r;
        public double z_l, z_r;
        public double bg = 0;

        public double[] direction = new double[3] { 0.0, 0.0, 0.0 };
        public string material = "";
        public string openmethod = "";
        public string description = "";
        public Dictionary<string, string> properties = new Dictionary<string, string>();
        public double _height = 5300;
        public double _width = 5300;
        public int OpenDirIndex;

        public string comp_name = "ifc";


        public UniComponent(THBimEntity entity, THBimMaterial bimMaterial, ref int uniComponentIndex, Buildingstorey buildingStorey, Component component,string materialType="") : base(component.name, component.type_id)
        {
            unique_id = uniComponentIndex++;
            guid = entity.Uid;

            floor_name = buildingStorey.floor_name;
            floor_num = buildingStorey.floorNo;
            comp_name = component.name;
            rgb = new double[3] { bimMaterial.KS_R, bimMaterial.KS_G, bimMaterial.KS_B };
            properties.Add("type", name);
            material = entity.Material;
            if (material == "加气混凝土") material = "TH-加气混凝土";

            if (name.Contains("Door") )
            {
                _height = (entity.GeometryParam as GeometryStretch).ZAxisLength;
                _width = (entity.GeometryParam as GeometryStretch).XAxisLength;
                properties.Add("二维门窗块名", (entity as THBimDoor).OperationType.ToString()) ;
                properties.Add("开启方向", (entity as THBimDoor).Swing.ToString());
                properties.Add("thickness", (entity.GeometryParam as GeometryStretch).YAxisLength.ToString());
                //properties.Add("wallThick", ( entity.ParentUid .GeometryParam as GeometryStretch).YAxisLength.ToString());
                properties.Add("旋转角度", new Xbim.Common.Geometry.XbimVector3D(1, 0, 0).GetAngle2((entity.GeometryParam as GeometryStretch).XAxis, new Xbim.Common.Geometry.XbimVector3D(0,0,1)).ToString() );
            }
            if (name.Contains("Window"))
            {
                _height = (entity.GeometryParam as GeometryStretch).ZAxisLength;
                _width = (entity.GeometryParam as GeometryStretch).XAxisLength;
                properties.Add("二维门窗块名", (entity as THBimWindow).WindowType.ToString());
                properties.Add("旋转角度", new Xbim.Common.Geometry.XbimVector3D(1, 0, 0).GetAngle2((entity.GeometryParam as GeometryStretch).XAxis, new Xbim.Common.Geometry.XbimVector3D(0, 0, 1)).ToString());
                properties.Add("thickness", (entity.GeometryParam as GeometryStretch).YAxisLength.ToString());
            }
        }

        public UniComponent(string uid, THBimMaterial bimMaterial, ref int uniComponentIndex, Buildingstorey buildingStorey, Component component,string materialType) : base(component.name, component.type_id)
        {
            unique_id = uniComponentIndex++;
            guid = uid;
            floor_name = buildingStorey.floor_name;
            floor_num = buildingStorey.floorNo;
            rgb = new double[3] { bimMaterial.Color_R, bimMaterial.Color_G, bimMaterial.Color_B };
            material = materialType;
            comp_name = component.name;

            properties.Add("type", name);
        }


        public new void WriteToFile(BinaryWriter writer)
        {
            name.WriteStr(writer);
            writer.Write(type_id);
            color.Write(writer);
            writer.Write(hori);
            writer.Write(unique_id);
            writer.Write(depth);
            writer.Write(depth_t);
            writer.Write(x_len);
            writer.Write(y_len);
            floor_name.WriteStr(writer);
            
            material.WriteStr(writer);
            openmethod.WriteStr(writer);
            description.WriteStr(writer);
            guid.WriteStr(writer);

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
                key.WriteStr(writer);
                value.WriteStr(writer);
            }
            if (OpenDirIndex == 2)
                ;
            writer.Write(OpenDirIndex);
            comp_name.WriteStr(writer);
            writer.Write(edge_ind_s);
            writer.Write(edge_ind_e);
        }
    }
}
