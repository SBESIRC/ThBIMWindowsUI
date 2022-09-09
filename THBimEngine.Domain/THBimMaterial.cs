using System;
using System.Collections.Generic;

namespace THBimEngine.Domain
{
    public class THBimMaterial
    {
        private static string DefaultKeyName = "Default";
        public static Dictionary<string, THBimMaterial> THBimDefaultMaterial = new Dictionary<string, THBimMaterial>
        {
            { DefaultKeyName,new THBimMaterial{ Color_R = 169 / 255f,Color_G = 179 / 255f, Color_B = 218 / 255f,KS_R = 0,KS_B = 0,KS_G = 0,Alpha = 1f,NS = 12,} },
            { typeof(THBimWall).Name.ToString().ToLower(),new THBimMaterial{Color_R = 255 / 255f,Color_G = 255 / 255f,Color_B = 255 / 255f,KS_R = 0,KS_B = 0,KS_G = 0, Alpha = 1f,NS = 12, } },
            { typeof(THBimWindow).Name.ToString().ToLower(),new THBimMaterial{Color_R = 214 / 255f,Color_G = 243 / 255f,Color_B = 242 / 255f,KS_R = 0.5f,KS_B = 0.5f,KS_G = 0.5f,Alpha = 0.15f,NS = 12,} },
            { typeof(THBimDoor).Name.ToString().ToLower(),new THBimMaterial{Color_R = 214 / 255f,Color_G = 243 / 255f,Color_B = 242 / 255f,KS_R = 0,KS_B = 0,KS_G = 0,Alpha = 0.15f,NS = 12 } },
            { typeof(THBimSlab).Name.ToString().ToLower(),new THBimMaterial{Color_R = 228 / 255f,Color_G = 227 / 255f,Color_B = 223 / 255f, KS_R = 0,KS_B = 0,KS_G = 0,Alpha = 1f,NS = 12,} },
            { typeof(THBimRailing).Name.ToString().ToLower(),new THBimMaterial{Color_R = 249 / 255f, Color_G = 63 / 255f, Color_B = 38 / 255f, KS_R = 0.5f, KS_B = 0.5f, KS_G = 0.5f,Alpha = 0.5f, NS = 12, } },
            { "thbeam",new THBimMaterial{ Color_R = 184 / 255f, Color_G = 172 / 255f, Color_B = 208 / 255f, KS_R = 0, KS_B = 0, KS_G = 0, Alpha = 1f, NS = 12, } },
            { "thcolumn",new THBimMaterial{ Color_R = 249 / 255f, Color_G = 94 / 255f, Color_B = 89 / 255f, KS_R = 0, KS_B = 0, KS_G = 0, Alpha = 1f, NS = 12, } },
            { "buildingelementproxy",new THBimMaterial{ Color_R = 186 / 255f, Color_G = 184 / 255f, Color_B = 203 / 255f, KS_R = 0, KS_B = 0, KS_G = 0, Alpha = 1f, NS = 12, } }
         };
        public static THBimMaterial GetTHBimEntityMaterial(Type entityType)
        {
            var material = THBimDefaultMaterial[DefaultKeyName];
            if (null == entityType)
                return material;
            var typeStr = entityType.Name.ToString();
            return GetTHBimEntityMaterial(typeStr);
        }
        public static THBimMaterial GetTHBimEntityMaterial(string type, bool isContain = false)
        {
            var material = THBimDefaultMaterial[DefaultKeyName];
            if (string.IsNullOrEmpty(type))
                return material;
            string typeStr = type.ToLower();
            if (isContain)
            {
                foreach (var keyValue in THBimDefaultMaterial)
                {
                    if (keyValue.Key.Contains(typeStr))
                    {
                        material = keyValue.Value;
                        break;
                    }
                }
            }
            else
            {
                foreach (var keyValue in THBimDefaultMaterial)
                {
                    if (keyValue.Key == typeStr)
                    {
                        material = keyValue.Value;
                        break;
                    }
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
