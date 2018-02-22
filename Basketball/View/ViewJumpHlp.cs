using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NitroBolt.Wui;
using Commune.Basis;
using Commune.Html;
using Commune.Data;
using Shop.Engine;

namespace Basketball
{
  public class ViewJumpHlp
  {
    static BasketballContext context
    {
      get { return (BasketballContext)SiteContext.Default; }
    }

    static string JumpPageUrl(string urlWithoutPageIndex, int pageIndex)
    {
      if (pageIndex == 0)
        return urlWithoutPageIndex;

      if (urlWithoutPageIndex.Contains("?"))
        return string.Format("{0}&page={1}", urlWithoutPageIndex, pageIndex);

      return string.Format("{0}?page={1}", urlWithoutPageIndex, pageIndex);

      //if (pageIndex == 0)
      //  return string.Format("/{0}", pageKind);

      //return string.Format("/{0}?page={1}", pageKind, pageIndex);
    }

    public static IHtmlControl JumpBar(string urlWithoutPageIndex, int allItemCount, int itemCountOnPage, int pageIndex)
    {
      int pageCount = BinaryHlp.RoundUp(allItemCount, itemCountOnPage);

      int startIndex = Math.Max(0, pageIndex - 9);
      int endIndex = Math.Min(startIndex + 20, pageCount);

      List<IHtmlControl> items = new List<IHtmlControl>();

      if (pageIndex > 0)
      {
        items.Add(
          new HLink(JumpPageUrl(urlWithoutPageIndex, pageIndex - 1), "назад").FontBold().MarginRight(10)
        );
      }

      for (int i = startIndex; i < endIndex; ++i)
      {
        items.Add(ViewJumpHlp.JumpElement(urlWithoutPageIndex, i, i == pageIndex, i == startIndex));
      }

      if (pageIndex < pageCount - 1)
      {
        items.Add(
          new HLink(JumpPageUrl(urlWithoutPageIndex, pageIndex + 1), "далее").FontBold().MarginLeft(10)
        );
      }

      return new HPanel(
        items.ToArray()
      ).MarginTop(25).MarginLeft(5);
    }

    static IHtmlControl JumpElement(string urlWithoutPageIndex, int pageIndex, bool isSelected, bool isFirst)
    {
      IHtmlControl jumpItem = null;
      if (!isSelected)
        jumpItem = new HLink(JumpPageUrl(urlWithoutPageIndex, pageIndex), (pageIndex + 1).ToString());
      else
        jumpItem = new HLabel(pageIndex + 1).FontBold();

      HPanel jumpElement = new HPanel(
        !isFirst ? new HLabel(" | ").MarginLeft(5).MarginRight(5) : null,
        jumpItem
      ).InlineBlock();

      //if (!isFirst)
      //  jumpElement.BorderLeft("1px solid #000");

      return jumpElement;
    }
  }
}