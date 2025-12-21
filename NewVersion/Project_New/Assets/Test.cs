using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
// 库（变量、函数....）

// 类（）
// 鸟的基础功能（Bird.cs）
// 0. 属性（重量、撞击系数）
// 1. 选中、拖拽、释放
// 2. 速度计算
// 3. .....

// 红色
// 黄色（加速）
// 黑色（按键爆炸）
//     Bomb.cs

// 面向对象（JAVA,PYTHON,C#） 面向过程（C，Pascal...）
// C# 面向对象

public class Test : MonoBehaviour
{
    // 函数method/方法/功能 function

    public UnityEvent onHPressed;

    // Start is called before the first frame update
    void Start()
    {
        // 1. 在update前1帧执行
        // 2. 执行1次
        // Debug.Log("Hello!");
    }

    // Update is called once per frame
    void Update()
    {
        // 每帧执行
        // 瓦洛兰特：
        // A 30帧 1/30(s)
        // B 240帧  
        //Debug.Log("Hello!");

        if (Input.GetKeyDown(KeyCode.H))
        {
            onHPressed?.Invoke();
        }

        // 什麽時候觸發？(鍵盤檢測輸入/UI按鈕檢測按下)
        // 觸發什麽功能？(UnityEvent/Method/....)
    }

    public void SayHi()
    {
        Debug.Log("Hi!");
    }


}
