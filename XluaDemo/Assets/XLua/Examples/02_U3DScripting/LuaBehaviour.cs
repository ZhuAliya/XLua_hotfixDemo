/*
 * Tencent is pleased to support the open source community by making xLua available.
 * Copyright (C) 2016 THL A29 Limited, a Tencent company. All rights reserved.
 * Licensed under the MIT License (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
 * http://opensource.org/licenses/MIT
 * Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.
*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using XLua;
using System;
using UnityEngine.UI;

namespace XLuaTest
{
    [System.Serializable]
    public class Injection  //可以做反射
    {
        public string name;
        public GameObject value;
    }

    [LuaCallCSharp]
    public class LuaBehaviour : MonoBehaviour
    {
        public TextAsset luaScript;     //这个是Lua代码的文本对象
        public Injection[] injections;

        //luaEnv可以理解为Lua虚拟机，运行时候的上下文
        internal static LuaEnv luaEnv = new LuaEnv(); //all lua behaviour shared one luaenv only!
        internal static float lastGCTime = 0;
        internal const float GCInterval = 1;//1 second 

        private Action luaStart;  //Behaviour生命周期的Lua接口；
        private Action luaUpdate;
        private Action luaOnDestroy;

        private LuaTable scriptEnv;     //脚本运行的上下文环境；

        void Awake()
        {
            scriptEnv = luaEnv.NewTable();  // 1.当调用Awake()的时候,就会new一个脚本运行的上下文环境的这样一个table

            // 为每个脚本设置一个独立的环境，可一定程度上防止脚本间全局变量、函数冲突
            //给这个table new一个原形表
            LuaTable meta = luaEnv.NewTable(); // 2.然后再给这个table new一个原形表
            meta.Set("__index", luaEnv.Global);
            scriptEnv.SetMetaTable(meta);
            meta.Dispose();
            //end


            scriptEnv.Set("self", this); //lua脚本使用self-->当前的组件实例this；当我们在脚本里面使用self时，就会是scriptEnv这个上下文

            foreach (var injection in injections)  //反射，对应上面injection方法里面的name和value
            {
                scriptEnv.Set(injection.name, injection.value);
            }

            //DoString 就是加载我们的lua代码，执行； //scriptEnv运行时候的环境表就是刚才设置的 scriptEnv.Set("self", this)这个环境表
            luaEnv.DoString(luaScript.text, "LuaBehaviour", scriptEnv); //luaEnv lua虚拟机，luaScript.text就是手动绑定的luaScript文件 能够获得这个文本里面所有的文本内容
            //当我们执行这个lua代码以后，这个环境表里面就是lua里面的环境表，环境表scriptEnv和代码(luaScript.text)装载在一起执行

            Action luaAwake = scriptEnv.Get<Action>("awake");//装载进去环境表(scriptEnv)就会有lua代码(luaScript.text)里面的函数,所有就可以scriptEnv.Get<Action>("awake")
            scriptEnv.Get("start", out luaStart);
            scriptEnv.Get("update", out luaUpdate);
            scriptEnv.Get("ondestroy", out luaOnDestroy);

            if (luaAwake != null)
            {
                luaAwake();
            }
        }

        // Use this for initialization
        void Start()
        {
            if (luaStart != null)
            {
                luaStart();
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (luaUpdate != null)
            {
                luaUpdate();
            }
            if (Time.time - LuaBehaviour.lastGCTime > GCInterval)
            {
                luaEnv.Tick();
                LuaBehaviour.lastGCTime = Time.time;
            }
    
        }

        void OnDestroy()
        {
            if (luaOnDestroy != null)
            {
                luaOnDestroy();
            }
            luaOnDestroy = null;
            luaUpdate = null;
            luaStart = null;
            scriptEnv.Dispose();
            injections = null;
        }
    }
}
