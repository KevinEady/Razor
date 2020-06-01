using Assistant.ClearScriptBinding;
using BrightIdeasSoftware;
using Microsoft.ClearScript.V8;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
    V8ScriptEngine m_Engine;
    ObjectListView m_OLV;
    struct OLVColumns
    {
      public OLVColumn enabled, name, run;
    };

    OLVColumns m_Cols;

    List<Plugin> m_Plugins;


    public PluginManager(ObjectListView olv)
    {
      m_OLV = olv;
      m_Plugins = new List<Plugin>();
      m_Engine = new V8ScriptEngine(V8ScriptEngineFlags.EnableDynamicModuleImports);
      m_Cols = new OLVColumns();
      m_Cols.enabled = olv.GetColumn(0);
      m_Cols.name = olv.GetColumn(1);
      m_Cols.run = olv.GetColumn(2);

      Recurse(Config.GetUserDirectory("Plugins"));
      InitializeObjectListView();
    }

    private void InitializeObjectListView()
    {
      // Suppress the string contents of the first column since we are going to use
      // a decoration to draw it.

      m_Cols.enabled.AspectToStringConverter = delegate (object x)
      {
        return "";
      };
      m_OLV.SetObjects(m_Plugins);

    }

    public void olv_FormatCell(object sender, BrightIdeasSoftware.FormatCellEventArgs e)
    {
      if (e.ColumnIndex == 0)
      {
        Plugin plugin = (Plugin)e.Model;
        NamedDescriptionDecoration decoration = new NamedDescriptionDecoration();
        decoration.Title = plugin.Name;
        decoration.Description = plugin.Description;
        e.SubItem.Decoration = decoration;
      }
    }

    private void Add(Plugin p)
    {
      m_Plugins.Add(p);
    }

    private void Recurse(string path)
    {
      try
      {
        string[] pluginPaths = Directory.GetFiles(path, "plugin.json");
        if (pluginPaths.Length == 1)
        {
          Plugin p = new Plugin(pluginPaths[0]);
          p.Load();
          Add(p);
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
  }
  public class NamedDescriptionDecoration : BrightIdeasSoftware.AbstractDecoration
  {
    public ImageList ImageList;
    public string ImageName;
    public string Title;
    public string Description;

    public Font TitleFont = new Font("Segoe UI", 9, FontStyle.Bold);
    public Color TitleColor = Color.FromArgb(255, 32, 32, 32);
    public Font DescripionFont = new Font("Segoe UI", 9);
    public Color DescriptionColor = Color.FromArgb(255, 96, 96, 96);
    public Size CellPadding = new Size(2, 2);

    public override void Draw(BrightIdeasSoftware.ObjectListView olv, Graphics g, Rectangle r)
    {
      Rectangle cellBounds = this.CellBounds;
      cellBounds.Inflate(-this.CellPadding.Width, -this.CellPadding.Height);
      Rectangle textBounds = cellBounds;

      if (this.ImageList != null && !String.IsNullOrEmpty(this.ImageName))
      {
        g.DrawImage(this.ImageList.Images[this.ImageName], cellBounds.Location);
        textBounds.X += this.ImageList.ImageSize.Width;
        textBounds.Width -= this.ImageList.ImageSize.Width;
      }

      //g.DrawRectangle(Pens.Red, textBounds);

      // Draw the title
      using (StringFormat fmt = new StringFormat(StringFormatFlags.NoWrap))
      {
        fmt.Trimming = StringTrimming.EllipsisCharacter;
        fmt.Alignment = StringAlignment.Near;
        fmt.LineAlignment = StringAlignment.Near;
        using (SolidBrush b = new SolidBrush(this.TitleColor))
        {
          g.DrawString(this.Title, this.TitleFont, b, textBounds, fmt);
        }
        // Draw the description
        SizeF size = g.MeasureString(this.Title, this.TitleFont, (int)textBounds.Width, fmt);
        textBounds.Y += (int)size.Height;
        textBounds.Height -= (int)size.Height;
      }

      // Draw the description
      using (StringFormat fmt2 = new StringFormat())
      {
        fmt2.Trimming = StringTrimming.EllipsisCharacter;
        using (SolidBrush b = new SolidBrush(this.DescriptionColor))
        {
          g.DrawString(this.Description, this.DescripionFont, b, textBounds, fmt2);
        }
      }
    }
  }

  public class Plugin
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

    public string Name
    {
      get { return m_Manifest.name; }
    }

    public string Description
    {
      get { return m_Manifest.description; }
    }

    public Plugin(string manifestPath)
    {
      m_ManifestPath = manifestPath;

    }
    public void Load()
    {
      using (StreamReader r = new StreamReader(m_ManifestPath))
      {
        string json = r.ReadToEnd();
        m_Manifest = JsonConvert.DeserializeObject<Manifest>(json);
        string mainJs = Path.Combine(Path.GetDirectoryName(m_ManifestPath), m_Manifest.main);

        using (StreamReader r2 = new StreamReader(mainJs))
        {
          string js = r.ReadToEnd();
          // m_Module = Engine.Compile(js);
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
