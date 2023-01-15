using System.Text;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
#region << 脚 本 注 释 >>
//作  用:    ListHelper
//作  者:    曾思信
//创建时间:  2022/
#endregion


namespace TMgr
{
    public static class ListHelper
    {
        public static string ToStringList(this ICollection col)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in col)
            {
                sb.Append(item.ToString() + " ");
            }
            return sb.ToString();
        }
    }
}
