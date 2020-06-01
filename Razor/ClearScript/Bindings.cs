using Assistant.ClearScriptBinding;
using BrightIdeasSoftware;
using Microsoft.ClearScript.V8;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//namespace Assistant.ClearScriptUtils
//{
//  public class Utils
//  {
//    private readonly object _createArray;
//    public Utils(V8ScriptEngine engine)
//    {
//      _createArray = engine.Evaluate(@"
//            (function (what) {
//                return Array.from(what);
//            }).valueOf()
//        ");
//    }

//    private object CreateJsDate(DateTime dt)
//    {
//      return ((dynamic)_createJsDate)(
//          dt.Year, dt.Month - 1, dt.Day,
//          dt.Hour, dt.Minute, dt.Second, dt.Millisecond
//      );
//    }
//    private static DateTime GetDateTime(string name)
//    {

//    }
//    public object GetDate(string name)
//    {
//      return CreateJsDate(GetDateTime(name));
//    }
//  }
//}

namespace Assistant.ClearScriptEngine
{

  public class PluginManager
  {
    static V8ScriptEngine Engine;

    public static void InitializeManager(ObjectListView olv)
    {
      Engine = new V8ScriptEngine(V8ScriptEngineFlags.EnableDynamicModuleImports);
      Recurse(Config.GetUserDirectory("Plugins"));
    }

    private static void Recurse(string path)
    {
      try
      {
        string[] pluginPaths = Directory.GetFiles(path, "plugin.json");
        if (pluginPaths.Length == 1)
        {
          Plugin p = new Plugin(pluginPaths[0]);
        }
      }
      catch
      {
       // Assistant.Client.Instance.
      }

      try
      {
        string[] dirs = Directory.GetDirectories(path);
        for (int i = 0; i < dirs.Length; i++)
        {
          if (dirs[i] != "" && dirs[i] != "." && dirs[i] != "..")
          {
            Recurse(dirs[i]);
          }
        }
      }
      catch
      {
      }
    }

    class Plugin
    {

      private class Manifest
      {
        public string name { get; set; }
        public string version { get; set; }
        public string description { get; set; }
        public string main { get; set; }
      }

      object m_Settings;
      string m_ManifestPath;
      Manifest m_Manifest;
      V8Script m_Module;
      bool m_Enabled;

      public Plugin(string manifestPath)
      {
        m_ManifestPath = manifestPath;
        using (StreamReader r = new StreamReader(manifestPath))
        {
          string json = r.ReadToEnd();
          m_Manifest = JsonConvert.DeserializeObject<Manifest>(json);
          string mainJs = Path.Combine(Path.GetDirectoryName(manifestPath), m_Manifest.main);

          using (StreamReader r2 = new StreamReader(mainJs))
          {
            string js = r.ReadToEnd();
            m_Module = Engine.Compile(js);
          }
        }
      }
    }
  }
}

namespace Assistant.ClearScriptBinding
{
  public class Player
  {
    private V8ScriptEngine m_engine;

    public Player(V8ScriptEngine engine)
    {
      this.m_engine = engine;
    }

    public string foo() { return "bar"; }

    public string name
    {
      get { return World.Player?.Name; }
      set { }
    }

    public Item backpack
    {
      get { return World.Player == null ? null : new Item(m_engine, World.Player.Backpack); }
      set { }
    }

  }


  public class Item
  {
    private V8ScriptEngine m_engine;
    private Assistant.Item m_Item;

    public Item(V8ScriptEngine engine, Assistant.Item item)
    {
      this.m_Item = item;
      this.m_engine = engine;
    }

    public object contents
    {
      get
      {
        //Debugger.Break();
        //return 3;
        dynamic arrayFrom = m_engine.Evaluate("Array.from");
        return arrayFrom(m_Item.Contains.Select(x => new Item(m_engine, x)));
      }
      set { }
    }

    public string name
    {
      get { return m_Item.DisplayName; }
      set { }
    }

    public string contentsStr
    {
      get
      {
        //Debugger.Break();
        //return 3;
        return string.Join(", ", m_Item.Contains.Select(x => x.DisplayName));
      }
      set { }
    }

  }
}
