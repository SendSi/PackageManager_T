using TMgr;
using UnityEngine;
#region << 脚 本 注 释 >>
//作  用:    TestT
//作  者:    曾思信
//创建时间:  2022/
#endregion



public class TestT : MonoBehaviour
{
    public int[] tList;
    private void Start()
    {
        Debug.Log(tList.ToString()) ;
        Debug.Log(tList.ToStringList());
    }
}

