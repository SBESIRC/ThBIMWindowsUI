using System.IO;

namespace THBimEngine.Domain.MidModel
{
    public class Component// 包含类型名、类型id、颜色、是否水平信息
	{
		public string name;// 类型名
		public int type_id;        // 类型id
		public Color color;       // 颜色rgba
		public bool hori;          // 是否水平

		public Component(string type,  int componentIndex)
        {
			name = type;
			type_id = componentIndex;
			color = new Color();
		}

		public void WriteToFile(BinaryWriter writer)
        {
			writer.Write(name.Length);
			writer.Write(name);
			writer.Write(type_id);
			color.Write(writer);
			writer.Write(hori);
		}
	}
}
