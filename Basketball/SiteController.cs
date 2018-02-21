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
using System.Web.Http;
using System.Net;
using System.Net.Http;
using Shop.Engine;
using System.Collections.Specialized;

namespace Basketball
{
  public class SiteController : ApiController
  {
    [HttpGet, HttpPost]
    [Route("")]
    public HttpResponseMessage Main()
    {
      return HWebApiSynchronizeHandler.Process<object>(this.Request, MainView.HViewCreator("", null));
    }

    [HttpGet, HttpPost]
    [Route("editor")]
    public HttpResponseMessage Editor()
    {
      return HWebApiSynchronizeHandler.Process<object>(this.Request, ContentEdit.HView);
    }

    [HttpGet, HttpPost]
    [Route("seo")]
    public HttpResponseMessage Seo()
    {
      return HWebApiSynchronizeHandler.Process<object>(this.Request, SeoEdit.HView);
    }

    [HttpGet, HttpPost]
    [Route("{kind}")]
    public HttpResponseMessage Route(string kind)
    {
      return HWebApiSynchronizeHandler.Process<object>(this.Request, MainView.HViewCreator(kind, null));
    }

    [HttpGet, HttpPost]
    [Route("{kind}/{id}")]
    public HttpResponseMessage Route(string kind, int id)
    {
      return HWebApiSynchronizeHandler.Process<object>(this.Request, MainView.HViewCreator(kind, id));
    }

    [HttpGet, HttpPost]
    [Route("filesupload")]
    public HttpResponseMessage FilesUpload()
    {
      return HttpLoader.FilesUploader();
    }

    [HttpGet, HttpPost]
    [Route("tileupload")]
    public HttpResponseMessage TileUpload()
    {
      return HttpLoader.TileUploader(Decor.ArticleThumbWidth);
    }

    [HttpGet, HttpPost]
    [Route("avatarupload")]
    public HttpResponseMessage AvatarUpload()
    {
      try
      {
        return HttpLoader.AvatarUploader("users", "avatar", Decor.AvatarSize);
      }
      catch (Exception ex)
      {
        Logger.WriteException(ex);
        throw;
      }
    }
  }
}