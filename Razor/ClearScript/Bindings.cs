using Assistant.ClearScriptBinding;
using BrightIdeasSoftware;
using Microsoft.ClearScript;
using Microsoft.ClearScript.JavaScript;
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
using Ultima;

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
    object m_InternalUtils;
    JSUtils m_jsutils;
    static PluginManager inst;

    struct OLVColumns
    {
      public OLVColumn enabled, name, run;
    };

    OLVColumns m_Cols;

    List<Plugin> m_Plugins;

    public class JSUtils
    {

      dynamic m_InternalUtils;
      public JSUtils(V8ScriptEngine engine)
      {
        string js = @"import * as myExports from 'util-internal'; myExports;";
        m_InternalUtils = engine.Evaluate(new DocumentInfo { Category = ModuleCategory.Standard }, js);
      }

      public object makePromise(object what)
      {
        return m_InternalUtils.makePromise(what);
      }

      public object isPluginRunning(dynamic pluginInst)
      {
        try
        {
          Plugin p = inst.m_Plugins.Find((Plugin other) =>
          {
            return other.Instance.name.Equals(pluginInst.name);
          });

          return p.Running;
        }
        catch { return null; }
      }
    }


    public static PluginManager instance(ObjectListView olv = null)
    {

      return PluginManager.inst ?? (PluginManager.inst =
        new ClearScriptEngine.PluginManager(olv));
    }
    private PluginManager(ObjectListView olv)
    {
      m_OLV = olv;
      m_Plugins = new List<Plugin>();
      m_Engine = new V8ScriptEngine(V8ScriptEngineFlags.EnableDynamicModuleImports | V8ScriptEngineFlags.EnableDebugging, 9422);
      m_Cols = new OLVColumns();
      m_Cols.enabled = olv.GetColumn(0);
      m_Cols.name = olv.GetColumn(1);
      m_Cols.run = olv.GetColumn(2);

      m_Engine.DocumentSettings.AccessFlags = DocumentAccessFlags.EnableFileLoading;
      m_Engine.DocumentSettings.AddSystemDocument("cuo", ModuleCategory.Standard, @"
export class Plugin {
  constructor() {

    Object.defineProperty(this, 'running', {
        get() {
            return PluginManagerUtils.isPluginRunning(this)
        }
    })

Object.defineProperty(this, 'name', {
        get() {
            return 'hello-world';
        },
        set() {}
    })
  }
  install() {}
  uninstall() {}
}
");
      m_Engine.DocumentSettings.AddSystemDocument("util", ModuleCategory.Standard, @"
export function sleep(duration) { 
  return new Promise(resolve => Timer.DelayedCallback(TimeSpan.FromMilliseconds(duration), new TimerCallback(resolve)).Start());
}
export async function move(direction, { paces } = { paces: 1 } ) {
  try {
    for (var i = 0; i < paces; i++) 
    {
      player.move(direction);
      await sleep(400);
    }
  } catch (e) { }
}

export function overhead(args, opts = null) {
  player.overhead(args, opts);
}

globalThis.sleep2 = sleep;
");
      m_Engine.DocumentSettings.AddSystemDocument("util-internal", ModuleCategory.Standard, @"
export function makePromise(x) { 
  return Promise.resolve(x);
}
");

      m_jsutils = new JSUtils(m_Engine);

      m_Engine.AddHostObject("PluginManagerUtils", m_jsutils);
      m_Engine.AddHostType("TimerCallback", typeof(TimerCallback));
      m_Engine.AddHostType("Timer", typeof(Timer));
      m_Engine.AddHostType("TimeSpan", typeof(TimeSpan));

      // m_Engine.AddHostObject("host", new ExtendedHostFunctions());


      m_Engine.AddHostObject("player", new ClearScriptBinding.Player(m_Engine));

      Recurse(Config.GetUserDirectory("Plugins"));

      InitializeObjectListView();
    }
    private delegate int OnCastSpell(string what);

    public static bool Debug = true;

    public JSUtils utils
    {
      get { return m_jsutils; }
    }
    internal static void Log(string str, params object[] args)
    {
      if (Debug)
      {
        try
        {
          using (StreamWriter w = new StreamWriter("Plugin.log", true))
          {
            w.Write(Engine.MistedDateTime.ToString("HH:mm:ss.fff"));
            w.Write(":: ");
            w.WriteLine(str, args);
            w.Flush();
          }
        }
        catch
        {
        }
      }
    }

    private void InitializeObjectListView()
    {
      // Suppress the string contents of the first column since we are going to use
      // a decoration to draw it.

      m_Cols.enabled.AspectToStringConverter = delegate (object x)
      {
        return "";
      };


      m_OLV.ButtonClick += objectListView1_ButtonClick;
      m_OLV.ItemChecked += objectListView1_ItemChecked;

      m_OLV.FormatCell += new System.EventHandler<BrightIdeasSoftware.FormatCellEventArgs>(this.olv_FormatCell);
      m_Cols.run.Renderer = new MyButtonRenderer();
      m_OLV.SetObjects(m_Plugins);

      //m_Cols.run.IsButton = true;
      //m_OLV.Enabled = false;

    }
    private void objectListView1_ButtonClick(object sender, BrightIdeasSoftware.CellClickEventArgs e)
    {
      if (!e.Item.Checked)
      {
        e.Item.Checked = true;
      }

      Plugin p = (Plugin)e.Model;
      p.Execute(m_Engine);
    }

    private void objectListView1_ItemChecked(object sender, ItemCheckedEventArgs e)
    {
      bool selected = e.Item.Checked;
      Plugin p = m_Plugins[e.Item.Index];
      if (selected)
      {
        p.Install(m_Engine);
      }
      else
      {
        p.Uninstall();
      }
      m_OLV.RefreshObject(p);
    }

    public class MyButtonRenderer : BrightIdeasSoftware.ColumnButtonRenderer
    {

      protected override void DrawImageAndText(Graphics g, Rectangle r)
      {


        //var row = (RowObject as Plugin);

        //g.Clear(Color.Green);
        //g.DrawString(row.Name, new Font("Arial", 12), Brushes.White, r.Left, r.Top);

        base.DrawImageAndText(g, r);
        //base.DrawImageAndText(g, r);
      }
    }

    public void olv_FormatCell(object sender, BrightIdeasSoftware.FormatCellEventArgs e)
    {
      Plugin plugin = (Plugin)e.Model;
      if (e.ColumnIndex == 1)
      {
        NamedDescriptionDecoration decoration = new NamedDescriptionDecoration();
        decoration.Title = plugin.Name;
        decoration.Description = plugin.Description;
        e.SubItem.Decoration = decoration;
      }
      //else if (e.ColumnIndex == 2)
      //{
      //  e.SubItem.Text = plugin.Enabled ? "Run" : "Enable && Run";
      //}
    }

    private void Add(Plugin p)
    {
      m_Plugins.Add(p);
    }

    private void Recurse(string path)
    {
      string[] pluginPaths = Directory.GetFiles(path, "plugin.json");
      if (pluginPaths.Length == 1)
      {
        try
        {
          Plugin p = new Plugin(pluginPaths[0]);
          p.Load();
          Add(p);
        }
        catch (Exception e)
        {
          Log("Could not load plugin {0}: {1}", pluginPaths[0], e.ToString());
          // Assistant.Client.Instance.
        }
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

    internal void RefreshObject(Plugin plugin)
    {
      m_OLV.RefreshObject(plugin);
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

    string m_ManifestPath;
    Manifest m_Manifest;
    dynamic m_PluginInst;
    bool m_Enabled;
    bool m_Running;

    public string Name
    {
      get { return m_Manifest.name; }
    }

    public bool Enabled
    {
      get { return m_Enabled; }
      set { m_Enabled = Enabled; }
    }

    public string Description
    {
      get { return m_Manifest.description; }
    }

    public string Run
    {
      get { return m_Running ? "Stop" : m_Enabled ? "Run" : "Install && Run"; }
    }

    internal dynamic Instance { get { return m_PluginInst; } }

    public bool Running { get { return m_Running; } }

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

        if (m_Manifest.main.StartsWith(".."))
        {
          throw new Exception("Plugin main must not start with ..");
        }


        //var def = m_Exports["default"];
        //PluginManager.Log("The square is {0}", new def);

      }
    }
    public bool Install(V8ScriptEngine engine)
    {
      if (m_PluginInst == null)
      {
        string mainJs = Path.Combine(Path.GetDirectoryName(m_ManifestPath), m_Manifest.main);
        string js = @"import plugin from '" + new Uri(mainJs).AbsoluteUri + "'; new plugin;";

        // m_Module = engine.Compile(new DocumentInfo { Category = ModuleCategory.Standard }, js);
        try
        {
          m_PluginInst = engine.Evaluate(new DocumentInfo { Category = ModuleCategory.Standard }, js);
          m_PluginInst.install();
          m_Enabled = true;
        }
        catch (Exception ex)
        {
          PluginManager.Log("Error installing plugin: {0}", ex.ToString());
        }
      }

      return m_PluginInst != null;
    }

    public void Stop()
    {
      m_Running = false;
      PluginManager.instance().RefreshObject(this);
    }
    public void Uninstall()
    {
      if (m_PluginInst != null)
      {
        m_PluginInst.uninstall();
        m_PluginInst = null;
        m_Enabled = false;
        PluginManager.instance().RefreshObject(this);
        DocumentLoader.Default.DiscardCachedDocuments();


      }
    }

    internal void Execute(V8ScriptEngine engine)
    {
      try
      {
        if (m_Running)
        {
          Stop();
        }
        else if (m_PluginInst != null || Install(engine))
        {
          m_Running = true;
          var result = m_PluginInst.exec();
          var pm = PluginManager.instance();
          var promised = pm.utils.makePromise(result);
          Action<object> onComplete = value => { 
            Stop();
            pm.RefreshObject(this);
          };
          promised.then(onComplete, onComplete); // writes "Resolved: 123"
          pm.RefreshObject(this);
        }
      }
      catch (Exception ex)
      {
        PluginManager.Log("Error executing plugin: {0}", ex.ToString());
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

    public void overhead(string format, dynamic opts = null)
    {
      {
        int hue = Config.GetInt("SysColor");
        try
        {
          hue = opts.hue;
        }
        catch { /* ignore */ };

        World.Player?.OverheadMessage(hue, format);
      }
    }

    public void move(string direction, int times = 1)
    {
      times = Math.Max(Math.Min(times, 1), 10);
      try
      {
        Direction dir = (Direction)Enum.Parse(typeof(Direction), direction, true);
        for (int i = 0; i < times; i++)
        {
          Client.Instance.RequestMove(dir);

        }
      }
      catch { /* ignore */ }
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
