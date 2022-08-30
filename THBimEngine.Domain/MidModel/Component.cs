using System.IO;

namespace THBimEngine.Domain.MidModel
{
    public class Component// 包含类型名、类型id、颜色、是否水平信息
	{
		public string name;  // 类型名
		public int type_id;  // 类型id
		public Color color;  // 颜色rgba
		public bool hori;    // 是否水平

		public Component(string type,  int componentIndex)
        {
			name = type;
			type_id = componentIndex;
			color = new Color((float)0.7, (float)0.2, (float)0.2, (float)1);
			if(type.Contains("Beam")||type.Contains("Slab"))
            {
				hori = true;
            }
			else
            {
				hori = false;
			}
		}

		public void WriteToFile(BinaryWriter writer)
        {
			writer.Write((ulong)name.Length);
			writer.Write(name.ToCharArray());
			writer.Write(type_id);
			color.Write(writer);
			writer.Write(hori);
		}
	}
}
