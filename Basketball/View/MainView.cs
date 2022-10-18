using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using Commune.Basis;
using Commune.Html;
using System.Drawing;
using NitroBolt.Wui;
using Commune.Data;
using System.IO;
using System.Net;
using System.Net.Http;
using Shop.Engine;

namespace Basketball
{
  public class MainView
  {
    static SiteStore store
    {
      get
      {
        return (SiteStore)SiteContext.Default.Store;
      }
    }

    public static Func<object, JsonData[], HttpRequestMessage, HtmlResult<HElement>> HViewCreator(
      string kind, int? id)
    {
      return delegate (object _state, JsonData[] jsons, HttpRequestMessage context)
      {
        SiteState state = _state as SiteState;
        if (state == null)
        {
          state = new SiteState();
        }

        LinkInfo link = null;
        if (kind == "news" || kind == "user" || (kind == "novosti" && id != null) || 
          kind == "article" || kind == "topic" || kind == "tags" || kind == "dialog" || kind == "confirmation")
        {
          link = new LinkInfo("", kind, id);
        }
        else
        {
          if (StringHlp.IsEmpty(kind))
            link = new LinkInfo("", "", null);
          else if (id == null)
          {
            link = store.Links.FindLink(kind, "");
            if (link == null)
              link = store.Links.FindLink("page", kind);
          }
        }

        if (link == null)
        {
          return new HtmlResult
          {
            RawResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.NotFound }
          };
        }

        if (jsons.Length > 0)
          state.AccessTime = DateTime.UtcNow;

				//hack Флажок, по которому добавляется очистка localStorage
				string localStorageKey = "";

        foreach (JsonData json in jsons)
        {
          try
          {
            if (state.IsRattling(json))
              continue;

						//Logger.AddMessage(json.ToString());

            try
            {
              string command = json.JPath("data", "command")?.ToString();
							if (command != null)
							{
								localStorageKey = Command.GetLocalStorageKey(command);

								if (StringHlp.IsEmpty(state.BlockHint))
								{
									object id1 = json.JPath("data", "id1");
									string hint = Command.GetHint(command, id1);
									if (!StringHlp.IsEmpty(hint))
									{
										state.BlockHint = hint;
										Logger.AddMessage("RestoreCommand: {0}", hint);
									}
								}
							}

       //       if (command != null && StringHlp.IsEmpty(state.BlockHint))
       //       {
       //         if (command.StartsWith("save_"))
       //         {
							//		object id1 = json.JPath("data", "id1");

							//		string hint = command.Substring(5);
							//		if (id != null)
							//			hint = string.Format("{0}_{1}", hint, id1);
							//		state.BlockHint = hint;

							//		Logger.AddMessage("Restore: {0}", hint);
							//	}
							//	else if (command == "tag_add" && kind == "")
							//	{
							//		state.BlockHint = "news_add";

							//		Logger.AddMessage("Restore: news_add");
							//	}
							//}
            }
            catch (Exception ex)
            {
              Logger.WriteException(ex);

							localStorageKey = "";
            }

            state.Operation.Reset();

            HElement cachePage = Page(HttpContext.Current, state, link.Kind, link.Id, "");

            hevent eventh = cachePage.FindEvent(json, true);
            if (eventh != null)
            {
              eventh.Execute(json);
            }
          }
          catch (Exception ex)
          {
            Logger.WriteException(ex);
            state.Operation.Message = string.Format("Непредвиденная ошибка: {0}", ex.Message);

						localStorageKey = "";
          }
        }

        HElement page = Page(HttpContext.Current, state, link.Kind, link.Id, localStorageKey);
        if (page == null)
        {
          return new HtmlResult
          {
            RawResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.NotFound }
          };
        }
        return HtmlHlp.FirstHtmlResult(page, state, TimeSpan.FromHours(1));
      };
    }

    static readonly HBuilder h = null;

    static HElement Page(HttpContext httpContext, SiteState state, 
			string kind, int? id, string localStorageKey)
    {
      UserHlp.DirectAuthorization(httpContext, SiteContext.Default.SiteSettings);

      LightObject currentUser = UserHlp.GetCurrentUser(httpContext, SiteContext.Default.UserStorage);

      if (currentUser != null && (BasketballHlp.IsBanned(currentUser) || currentUser.Get(UserType.NotConfirmed)))
      {
        currentUser = null;
        httpContext.Logout();
      }

      state.EditMode = httpContext.IsInRole("edit");
      state.SeoMode = httpContext.IsInRole("seo");
      state.UserMode = currentUser != null;

			int[] foundTagIds = state.Option.Get(OptionType.FoundTagIds);
			if (foundTagIds != null && foundTagIds.Length > 0)
			{
				kind = "search";
				id = null;
			}

      IHtmlControl adminSectionPanel = null;
      if (kind == "page")
      {
        LightSection section = store.Sections.FindSection(id);
        adminSectionPanel = DecorEdit.AdminSectionPanel(
          state.EditMode, state.SeoMode, kind, section, false
        );
      }

      IHtmlControl dialogBox = null;
      if (!StringHlp.IsEmpty(state.Operation.Message))
        dialogBox = DecorEdit.GetDialogBox(state);

      bool isForum = kind == "topic";
      if (kind == "page")
      {
        LightSection pageSection = store.Sections.FindSection(id);
        string designKind = pageSection.Get(SectionType.DesignKind);
        if (pageSection != null && (designKind == "forum" || designKind == "forumSection"))
          isForum = true;
      }


			string title = "";
			string description = "";
			SchemaOrg schema = null;
			bool wideContent;

			IHtmlControl centerView = ViewHlp.GetCenter(httpContext,
        state, currentUser, kind, id, out title, out description, out schema, out wideContent
      );

      if (centerView == null && StringHlp.IsEmpty(state.RedirectUrl))
        return null;

      BasketballContext context = (BasketballContext)SiteContext.Default;
      try
      {
        if (currentUser != null && kind == "dialog" && id != null)
        {
          if (context.UnreadDialogLink.FindRow(DialogReadType.UnreadByUserId, currentUser.Id) != null)
          {
            DialogueHlp.MarkReadCorrespondence(context.ForumConnection, currentUser.Id, id.Value);
            context.UpdateUnreadDialogs();
          }
        }
      }
      catch (Exception ex)
      {
        Logger.WriteException(ex);
      }

      HEventPanel mainPanel = new HEventPanel(
        new HPanel(
          new HAnchor("top"),
          DecorEdit.AdminMainPanel(SiteContext.Default.SiteSettings, httpContext),
          ViewHeaderHlp.GetHeader(httpContext, state, currentUser, kind, id, isForum),
          adminSectionPanel,
					!StringHlp.IsEmpty(localStorageKey) ? 
						BasketballHlp.RemoveLocalStorageScriptControl(localStorageKey) : null,
          new HPanel(
            new HPanel(
              centerView,
              new HPanel(
                ViewRightColumnHlp.GetRightColumnView(state, isForum).InlineBlock().MarginRight(15)
                  .MediaLaptop(new HStyle().Block().MarginRight(0))
                  .MediaTablet(new HStyle().InlineBlock().MarginRight(15))
                  .MediaSmartfon(new HStyle().Width("100%").MarginRight(0)),
                ViewRightColumnHlp.GetReclameColumnView(state).InlineBlock().VAlign(true)
              ).Hide(wideContent).PositionAbsolute().Top(13).Right(15)
                .MediaTablet(new HStyle().Position("static").MarginTop(15))
            //.MediaSmartfon(new HStyle().Width("100%"))
            ).PositionRelative().Align(true)
            .Padding(15).PaddingRight(485)
            .WidthLimit("", kind == "" ? "727px" : "892px")
            .MediaLaptop(new HStyle().PaddingRight(250))
            .MediaTablet(new HStyle().PaddingRight(15))
            .MediaSmartfon(new HStyle().PaddingLeft(5).PaddingRight(5))
          ).MarginLeft(12).MarginRight(12).PaddingBottom(160).Background(Decor.panelBackground)
            .BoxSizing().HeightLimit("900px", "")
            .MediaTablet(new HStyle().MarginLeft(0).MarginRight(0))
        ),
        ViewHlp.GetFooterView(kind == ""),
        dialogBox
      //popupPanel
      ).Width("100%").BoxSizing().Align(null).Background(Decor.pageBackground)
        .Padding(1)
        .FontFamily("Tahoma").FontSize(12); //.Color(Decor.textColor);

      if (!StringHlp.IsEmpty(state.PopupHint) || dialogBox != null)
      {
        mainPanel.OnClick(";");
        mainPanel.Event("popup_reset", "", delegate
        {
          state.PopupHint = "";
          state.Operation.Reset();
        });
      }

      StringBuilder css = new StringBuilder();

      std.AddStyleForFileUploaderButtons(css);

      HElement mainElement = mainPanel.ToHtml("main", css);

      SiteSettings settings = SiteContext.Default.SiteSettings;

      //string blockHint = state.BlockHint;
      //bool withCkeditor = false;
      //if (!StringHlp.IsEmpty(blockHint))
      //{
      //  withCkeditor = blockHint == "news_add" || blockHint.StartsWith("article_edit_") ||
      //    blockHint.StartsWith("news_edit_");
      //}
      //bool withFileuploader = kind == "user" || withCkeditor;

      return h.Html
      (
        h.Head(
          h.Element("title", title),
          h.MetaDescription(description),
          h.LinkCss(UrlHlp.FileUrl("/css/static.css")),
          h.LinkShortcutIcon("/images/favicon.ico"),
          h.Meta("viewport", "width=device-width"),
          //h.LinkCss("/css/font-awesome.css"),
          //h.LinkCss("/css/fileuploader.css"),
          h.LinkScript("/scripts/fileuploader.js"),
          h.LinkScript("/ckeditor/ckeditor.js?v=4113"),
          HtmlHlp.CKEditorUpdateAll(),
          h.Raw(store.SeoWidgets.WidgetsCode),
          HtmlHlp.SchemaOrg(schema),
          h.OpenGraph("type", "website"),
          h.OpenGraph("title", title),
          h.OpenGraph("url", description),
          h.OpenGraph("site_name", settings.Organization),
          h.OpenGraph("image", settings.FullUrl("/images/logo_mini.jpg"))
        ),
        h.Body(
          h.Css(h.Raw(css.ToString())),
          h.Div(
            HtmlHlp.RedirectScript(state.RedirectUrl)
          ),
          //h.Div(
          //  withFileuploader ? h.LinkScript("/scripts/fileuploader.js") : null,
          //  withCkeditor ? h.LinkScript("/ckeditor/ckeditor.js") : null,
          //  withCkeditor ? HtmlHlp.CKEditorUpdateAll() : null
          //),
          mainElement
          //withEditor ? h.Script(h.type("text/javascript"), "console.log('withScript');") : null,
          //!withFileuploader && withEditor ? h.LinkScript("/scripts/fileuploader.js") : null,
          //withEditor ? h.LinkScript("/ckeditor/ckeditor.js") : null,
          //withEditor ? HtmlHlp.CKEditorUpdateAll() : null
        //HtmlHlp.SchemaOrg(schema),
        )
      );
    }
  }
}