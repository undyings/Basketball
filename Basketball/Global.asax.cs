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
using NitroBolt.Wui;
using System.Data;
using System.Data.SQLite;
using System.ComponentModel.Design;
using System.Web.UI.WebControls;

namespace Basketball
{
  public class WebApiApplication : System.Web.HttpApplication
  {
    const string connectionStringFormat = "Data Source={0};Pooling=true;FailIfMissing=false;UseUTF16Encoding=True;datetimekind=Utc";
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
        DialogueHlp.CheckAndCreateDialogueTables(forumConnection);

        MetaHlp.ReserveDiapasonForMetaProperty(fabricConnection);

        FabricHlp.CheckAndCreateMenu(fabricConnection, "main");

//        DataTable table = userConnection.GetTable("", "Select * From light_object");
//        foreach (DataRow row in table.Rows)
//        {
//          DateTime createTime = (DateTime)row[3];
//          DateTime? modifyTime = null;
//          if (row[4] != DBNull.Value)
//            modifyTime = (DateTime)row[4];

//          userConnection.GetScalar("",
//  "Update light_object Set act_from=@createTime, act_till=@modifyTime Where obj_id=@id",
//  new DbParameter("createTime", createTime),
//  new DbParameter("modifyTime", modifyTime),
//  new DbParameter("id", row[0])
//);
//        }

				//        DataTable table = fabricConnection.GetTable("", "Select * From light_object where type_id = 5000 and obj_id > 130170 order by obj_id asc");
				//        foreach (DataRow row in table.Rows)
				//        {
				//          DateTime createTime = (DateTime)row[3];
				//          DateTime? modifyTime = null;
				//          if (row[4] != DBNull.Value)
				//            modifyTime = (DateTime)row[4];

				//          fabricConnection.GetScalar("",
				//  "Update light_object Set act_from=@createTime, act_till=@modifyTime Where obj_id=@id",
				//  new DbParameter("createTime", createTime),
				//  new DbParameter("modifyTime", modifyTime),
				//  new DbParameter("id", row[0])
				//);
				//}


				//        DataTable table = forumConnection.GetTable("", "Select * From message Where id > 42683 order by id asc");
				//        foreach (DataRow row in table.Rows)
				//        {
				//          DateTime createTime = (DateTime)row[5];
				//          DateTime modifyTime = (DateTime)row[6];
				//					forumConnection.GetScalar("",
				//	"Update message Set create_time=@createTime, modify_time=@modifyTime Where id=@id",
				//	new DbParameter("createTime", new DateTime(createTime.ToUniversalTime().Ticks, DateTimeKind.Unspecified)),
				//	new DbParameter("modifyTime", new DateTime(modifyTime.ToUniversalTime().Ticks, DateTimeKind.Unspecified)),
				//	new DbParameter("id", row[0])
				//);

				//				}


				EditorSelector sectionEditorSelector = new EditorSelector(
          new SectionTunes("news", "Новости"),
          new SectionTunes("articles", "Статьи"),
          new SectionTunes("forum", "Форум"),
					new SectionTunes("rules", "Правила").Link(),
          new SectionTunes("forumSection", "Раздел форума")
        );

        EditorSelector unitEditorSelector = new EditorSelector(
          new UnitTunes("reclame", "Рекламный блок").Tile().ImageAlt().Link().Annotation()
        );

        Shop.Engine.Site.Novosti = "news";
        Shop.Engine.Site.DirectPageLinks = true;
				//Shop.Engine.Site.AddFolderForNews = true;

				try
				{

					string fabricScriptPath = Path.Combine(appPath, "FabricScript.sql");
					if (File.Exists(fabricScriptPath))
					{
						string script = File.ReadAllText(fabricScriptPath);
						Logger.AddMessage("Выполняем стартовый скрипт для fabric.db3: {0}", script);
						fabricConnection.GetScalar("", script);
					}

					string userScriptPath = Path.Combine(appPath, "UserScript.sql");
					if (File.Exists(userScriptPath))
					{
						string script = File.ReadAllText(userScriptPath);
						Logger.AddMessage("Выполняем стартовый скрипт для user.db3: {0}", script);
						userConnection.GetScalar("", script);
					}
				}
				catch (Exception ex)
				{
					Logger.WriteException(ex, "Ошибка при выполнении стартового скрипта");
				}

        SiteContext.Default = new BasketballContext(
          appPath, sectionEditorSelector, unitEditorSelector,
          userConnection, fabricConnection, messageConnection, forumConnection
        );

        SiteContext.Default.Pull.StartTask(Labels.Service, 
          MemoryChecker((BasketballContext)SiteContext.Default)
        );

        SiteContext.Default.Pull.StartTask(Labels.Service,
          SiteTasks.CleaningSessions(SiteContext.Default, 
            TimeSpan.FromHours(1), TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(1)
          )
        );
      }
      catch (Exception ex)
      {
        Logger.WriteException(ex, "Ошибка создания подключения к базе данных:");
      }
    }

    static IEnumerable<Step> MemoryChecker(BasketballContext context)
    {
      while (!context.Pull.IsFinishing)
      {
        Process process = Process.GetCurrentProcess();
        Logger.AddMessage("Process: {0}", ProcessInfoToString(process, context));

        yield return new WaitStep(TimeSpan.FromMinutes(5));
      }
    }

    static string ProcessInfoToString(Process process, BasketballContext context)
    {
      //PerformanceCounter counter = new PerformanceCounter("ASP.NET Applications", "Sessions Active", "__Total__");
      //PerformanceCounter counter = new PerformanceCounter("ASP.NET", "Application Restarts");

      return string.Format(
				"id = {0} cpu = {1}, session = {2}, work = {3}mb, virtual = {4}mb, requests = {5}, topics = {6}, forum = {7}",
        process.Id, process.UserProcessorTime, HWebApiSynchronizeHandler.Frames.Count,
        process.WorkingSet64 / 1000000, process.VirtualMemorySize64 / 1000000,
				requestCount,	context.NewsStorages.TopicCount, context.Forum.TopicsStorages.TopicCount
			);
    }

		//volatile static int sessionCount = 0;

		//protected void Session_Start(object sender, EventArgs e)
		//{
		//  Logger.AddMessage("SessionStart");
		//  sessionCount++;
		//}

		//protected void Session_End(object sender, EventArgs e)
		//{
		//  sessionCount--;
		//}

		static volatile int requestCount = 0;

    protected void Application_BeginRequest(object sender, EventArgs e)
    {
			requestCount++;

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

    protected void Application_End(object sender, EventArgs e)
    {
      SiteContext.Default.Pull.Finish();

      Process process = Process.GetCurrentProcess();

      Logger.AddMessage("Process: {0}",
        ProcessInfoToString(process, (BasketballContext)SiteContext.Default)
      );
    }
  }
}