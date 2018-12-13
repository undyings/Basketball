using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Commune.Data;
using Commune.Html;
using Shop.Engine;

namespace Basketball
{
  public static class Decor
  {
    public const int AvatarSize = 50;
    public const int ArticleThumbWidth = 100;
    public const int ArticleThumbHeight = 60;

    public const string timeFormat = "dd.MM.yyyy HH:mm";

    public const string pageBackground = "#f5f5f5";
    public const string textColor = "#000";
    public const string panelBackground = "#fff";

    public const string minorColor = "#626262";
    public const string linkColor = "#0063ce";

    public const string disabledColor = "#9c9c9c";
    public const string redColor = "red"; // "#f16e61";

    public const string menuBackground = "#105a94";
    public const string menuSelectedBackground = pageBackground;
    public const string menuColor = "#fff";
    public const string menuSelectedColor = "#000";
    public const string subtitleColor = "#b4321e";

    public const string evenBackground = "#f2f2f2";

    public const string buttonBorder = "1px solid #a5a5a5";
    public const string bottomBorder = "3px solid #f2f2f2";
    public const string columnBorder = "2px solid #fff";

    public static HButton Button(string caption)
    {
      return new HButton(caption, new HHover().Border("1px solid #7b7b7b"))
        .Padding(3, 8, 2, 8).Border(buttonBorder)
        .Background("#f1f1f1")
        .LinearGradient("to top right", "#dddddd", "#f1f1f1"); ;
    }

    public static HButton ButtonMidi(string caption)
    {
      return new HButton(caption).FontBold().FontSize(12).Padding(2, 7).MarginRight(5)
        .Color("#666666").Border("1px solid #e6e6e6").Background(Decor.pageBackground);
    }

    public static HButton ButtonMini(string caption)
    {
      return new HButton(caption).Padding(0, 3, 1, 3).MarginRight(5)
        .FontSize("85%").Color("#666666").Border("1px solid #e6e6e6").Background(Decor.pageBackground);
    }

    public static IHtmlControl Title(string title)
    {
      return new HPanel(new HH1(title).FontBold(false).FontSize("180%")).MarginTop(15).MarginBottom(15);
    }

    public static IHtmlControl Subtitle(string subtitle)
    {
      return new HLabel(subtitle).Color(Decor.subtitleColor).FontSize("140%").Margin(18, 10, 0, 0);
    }

    public static HPanel PropertyEdit(string dataName, string caption)
    {
      return PropertyEdit(dataName, caption, "");
    }

    public static HPanel PropertyEdit(string dataName, string caption, string value)
    {
      return new HPanel(
        new HLabel(caption).FontBold(),
        new HTextEdit(dataName, value).Width("100%")
      ).MarginBottom(5);
    }

    public static HPanel AuthEdit(string dataName, string caption)
    {
      return AuthEdit(new HTextEdit(dataName), caption);
    }

    public static HPanel AuthEdit(IHtmlControl editControl, string caption)
    {
      return new HPanel(
        new HLabel(caption).Block(),
        editControl.Width("100%")
      ).WidthLimit("", "380px").MarginBottom(15);
    }
  }
}