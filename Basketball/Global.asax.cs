using Commune.Basis;
using Commune.Data;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Security;
using System.Web.Http;
using System.Web.SessionState;
using Commune.Html;
using Shop.Engine;
using Commune.Task;

namespace Basketball
{
  public class WebApiApplication : System.Web.HttpApplication
  {
    const string connectionStringFormat = "Data Source={0};Pooling=true;FailIfMissing=false;UseUTF16Encoding=True;";
    protected void Application_Start(object sender, EventArgs e)
    {
      string appPath = HttpContext.Current.Server.MapPath("");
      string logFolder = ApplicationHlp.CheckAndCreateFolderPath(appPath, "Logs");

      try
      {
        Logger.EnableLogging(Path.Combine(logFolder, "site.log"), 2);

        GlobalConfiguration.Configure(WebApiConfig.Register);

        string databaseFolder = ApplicationHlp.CheckAndCreateFolderPath(appPath, "Data");

        IDataLayer userConnection = new SQLiteDataLayer(string.Format(
          connectionStringFormat, Path.Combine(databaseFolder, "user.db3")));

        IDataLayer fabricConnection = new SQLiteDataLayer(string.Format(
          connectionStringFormat, Path.Combine(databaseFolder, "fabric.db3")));

        IDataLayer messageConnection = new SQLiteDataLayer(string.Format(
          connectionStringFormat, Path.Combine(databaseFolder, "message.db3")));

        IDataLayer forumConnection = new SQLiteDataLayer(string.Format(
          connectionStringFormat, Path.Combine(databaseFolder, "forum.db3")));

        Logger.AddMessage("Подключения к базам данных успешно созданы");

        SQLiteDatabaseHlp.CheckAndCreateDataBoxTables(userConnection);
        SQLiteDatabaseHlp.CheckAndCreateDataBoxTables(fabricConnection);
        MessageHlp.CheckAndCreateMessageTables(messageConnection);
        MessageHlp.CheckAndCreateMessageTables(forumConnection);

        MetaHlp.ReserveDiapasonForMetaProperty(fabricConnection);

        FabricHlp.CheckAndCreateMenu(fabricConnection, "main");

        EditorSelector sectionEditorSelector = new EditorSelector(
          new SectionTunes("news", "Новости"),
          new SectionTunes("articles", "Статьи"),
          new SectionTunes("forum", "Форум"),
          new SectionTunes("forumSection", "Раздел форума")
        );

        EditorSelector unitEditorSelector = new EditorSelector(
          new UnitTunes("reclame", "Рекламный блок").Thumb().ImageAlt().Link()
        );

        Shop.Engine.Site.Novosti = "news";
        Shop.Engine.Site.DirectPageLinks = true;
        //Shop.Engine.Site.AddFolderForNews = true;

        string scriptPath = Path.Combine(appPath, "ExecuteScript.sql");
        if (File.Exists(scriptPath))
        {
          string script = File.ReadAllText(scriptPath);
          Logger.AddMessage("Выполняем стартовый скрипт: {0}", script);
          fabricConnection.GetScalar("", script);
        }

        SiteContext.Default = new BasketballContext(
          appPath, sectionEditorSelector, unitEditorSelector,
          userConnection, fabricConnection, messageConnection, forumConnection
        );

        SiteContext.Default.Pull.StartTask(Labels.Service, 
          MemoryChecker(SiteContext.Default)
        );
      }
      catch (Exception ex)
      {
        Logger.WriteException(ex, "Ошибка создания подключения к базе данных:");
      }
    }

    static IEnumerable<Step> MemoryChecker(IContext context)
    {
      while (!context.Pull.IsFinishing)
      {
        Process process = Process.GetCurrentProcess();
        Logger.AddMessage("Process: {0}, topics = {1}",
          ProcessInfoToString(process), 
          ((BasketballContext)context).NewsStorages.TopicCount
        );

        yield return new WaitStep(TimeSpan.FromMinutes(60));
      }
    }

    static string ProcessInfoToString(Process process)
    {
      return string.Format("id = {0} cpu = {1}, page = {2}mb peak = {3}mb, virtual = {4}mb peak = {5}mb",
        process.Id, process.UserProcessorTime,
        process.PagedMemorySize64 / 1000000, process.PeakPagedMemorySize64 / 1000000,
        process.VirtualMemorySize64 / 1000000, process.PeakVirtualMemorySize64 / 1000000
      );
    }

    protected void Session_Start(object sender, EventArgs e)
    {

    }

    protected void Application_BeginRequest(object sender, EventArgs e)
    {
      string path = (this.Context.Request.Path ?? "").ToLower();

      LightObject redirect = SiteContext.Default.Store.Redirects.Find(path);
      if (redirect != null)
      {
        Context.Response.Status = "301 Moved Permanently";
        Context.Response.StatusCode = 301;
        Context.Response.AddHeader("Location", redirect.Get(RedirectType.To));
        return;
      }
    }

    protected void Application_AuthenticateRequest(object sender, EventArgs e)
    {
      HttpContext.Current.SetUserFromCookie();
    }

    protected void Application_Error(object sender, EventArgs e)
    {

    }

    protected void Session_End(object sender, EventArgs e)
    {

    }

    protected void Application_End(object sender, EventArgs e)
    {
      SiteContext.Default.Pull.Finish();

      Process process = Process.GetCurrentProcess();

      Logger.AddMessage("Process: {0}, topics = {1}",
        ProcessInfoToString(process),
        ((BasketballContext)SiteContext.Default).NewsStorages.TopicCount
      );
    }
  }
}