using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace THBimEngine.Domain
{
    public static class EnumUtil
    {
        #region 枚举的相关信息
        
        public static T GetEnumItemByDescription<T>(string desName) where T : Enum
        {
            Type enumType = typeof(T);
            string[] allEnums = null;
            try
            {
                allEnums = Enum.GetNames(enumType);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            if (null == allEnums || allEnums.Length < 1)
                return default(T);
            foreach (var item in allEnums)
            {
                var enumItem = enumType.GetField(item);
                object[] objs = enumItem.GetCustomAttributes(typeof(DescriptionAttribute), false);
                string des = "";
                if (objs.Length == 0)
                    des = item;
                else
                    des = ((DescriptionAttribute)objs[0]).Description;
                if (string.IsNullOrEmpty(des))
                    continue;
                if (desName == des)
                    return (T)enumItem.GetValue(item);
            }
            return default(T);
        }
        /// <summary>
        /// 获取枚举值的描述信息DescriptionAttribute中内容
        /// [DescriptionAttribute("")]
        /// </summary>
        /// <param name="enumValue"></param>
        /// <returns></returns>
        public static string GetEnumDescription(Enum enumValue)
        {
            string value = enumValue.ToString();
            FieldInfo field = enumValue.GetType().GetField(value);
            if (field == null)
                return "";
            object[] objs = field.GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (objs.Length == 0)
                return value;
            DescriptionAttribute description = (DescriptionAttribute)objs[0];
            return description.Description;
        }
        public static List<string> GetEnumDescriptions<T>() where T:Enum
        {
            var allDes = new List<string>();
            var enumType = typeof(T);
            string[] allEnums = Enum.GetNames(enumType);
            if (null == allEnums || allEnums.Length < 1)
                return allDes;
            foreach (var item in allEnums)
            {
                var enumItem = enumType.GetField(item);
                object[] objs = enumItem.GetCustomAttributes(typeof(DescriptionAttribute), false);
                string des = "";
                if (objs.Length == 0)
                    des = item;
                else
                    des = ((DescriptionAttribute)objs[0]).Description;
                allDes.Add(des);
            }
            return allDes;
        }
        public static Dictionary<int,string> GetEnumValueDicDescriptions<T>() where T : Enum
        {
            var allDes = new Dictionary<int,string>();
            var enumType = typeof(T);
            string[] allEnums = Enum.GetNames(enumType);
            if (null == allEnums || allEnums.Length < 1)
                return allDes;
            foreach (var item in allEnums)
            {
                var enumItem = enumType.GetField(item);
                int value = (int)enumItem.GetValue(item);
                if (allDes.ContainsKey(value))
                    continue;
                object[] objs = enumItem.GetCustomAttributes(typeof(DescriptionAttribute), false);
                string des = "";
                if (objs.Length == 0)
                    des = item;
                else
                    des = ((DescriptionAttribute)objs[0]).Description;
                allDes.Add(value,des);
            }
            return allDes;
        }
        public static Dictionary<T, string> GetEnumDicDescriptions<T>() where T : Enum
        {
            var allDes = new Dictionary<T, string>();
            var enumType = typeof(T);
            string[] allEnums = Enum.GetNames(enumType);
            if (null == allEnums || allEnums.Length < 1)
                return allDes;
            foreach (var item in allEnums)
            {
                var enumItem = enumType.GetField(item);
                var enumIt = (T)enumItem.GetValue(item);
                if (allDes.ContainsKey(enumIt))
                    continue;
                object[] objs = enumItem.GetCustomAttributes(typeof(DescriptionAttribute), false);
                string des = "";
                if (objs.Length == 0)
                    des = item;
                else
                    des = ((DescriptionAttribute)objs[0]).Description;
                allDes.Add(enumIt, des);
            }
            return allDes;
        }
        #endregion
    }
}
