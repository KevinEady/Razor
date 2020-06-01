using Microsoft.ClearScript.V8;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace Assistant.ClearScriptUtils
{
  public class Plugin
  {
    object m_settings;
    public Plugin(object o)
    {
      m_settings = o;
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
