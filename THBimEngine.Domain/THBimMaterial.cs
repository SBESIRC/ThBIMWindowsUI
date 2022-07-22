using System;
using System.Collections.Generic;

namespace THBimEngine.Domain
{
    public class THBimMaterial
    {
        private static string DefaultKeyName ="Default";
        public static Dictionary<string, THBimMaterial> THBimDefaultMaterial = new Dictionary<string, THBimMaterial>
        {
            { DefaultKeyName,new THBimMaterial{ Color_R = 169 / 255f,Color_G = 179 / 255f, Color_B = 218 / 255f,KS_R = 0,KS_B = 0,KS_G = 0,Alpha = 0.5f,NS = 12,} },
            { typeof(THBimWall).ToString(),new THBimMaterial{Color_R = 226 / 255f,Color_G = 212 / 255f,Color_B = 190 / 255f,KS_R = 0,KS_B = 0,KS_G = 0, Alpha = 1f,NS = 12, } },
            { typeof(THBimWindow).ToString(),new THBimMaterial{Color_R = 116 / 255f,Color_G = 195 / 255f,Color_B = 219 / 255f,KS_R = 0,KS_B = 0,KS_G = 0,Alpha = 0.5f,NS = 12,} },
            { typeof(THBimDoor).ToString(),new THBimMaterial{Color_R = 167 / 255f,Color_G = 182 / 255f,Color_B = 199 / 255f,KS_R = 0,KS_B = 0,KS_G = 0,Alpha = 1f,NS = 12 } },
            { typeof(THBimSlab).ToString(),new THBimMaterial{Color_R = 167 / 255f,Color_G = 182 / 255f,Color_B = 199 / 255f, KS_R = 0,KS_B = 0,KS_G = 0,Alpha = 1f,NS = 12,} },
            { typeof(THBimRailing).ToString(),new THBimMaterial{Color_R = 136 / 255f, Color_G = 211 / 255f, Color_B = 198 / 255f, KS_R = 0, KS_B = 0, KS_G = 0,Alpha = 0.5f, NS = 12, } },
        };
        public static THBimMaterial GetTHBimEntityMaterial(Type entityType) 
        {
            var material = THBimDefaultMaterial[DefaultKeyName];
            if (null == entityType)
                return material;
            var typeStr = entityType.ToString();
            foreach (var keyValue in THBimDefaultMaterial) 
            {
                if (keyValue.Key == typeStr)
                {
                    material = keyValue.Value;
                    break;
                }
            }
            return material;
        }

        #region 漫反射RGB
        public float Color_R { get; set; }
		public float Color_G { get; set; }
		public float Color_B { get; set; }
        #endregion

        #region 高光项RGB(暂时没有使用)
        public float KS_R { get; set; }
		public float KS_G { get; set; }
		public float KS_B { get; set; }
        #endregion
        /// <summary>
        /// 物体透明度（0 - 1）
        /// </summary>
        public float Alpha { get; set; }
        /// <summary>
        /// cos的指数（specularity）
        /// </summary>
		public int NS { get; set; }
	}
}
