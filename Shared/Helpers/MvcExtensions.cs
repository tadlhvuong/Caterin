using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

namespace Shared.Helpers
{
    public static class MvcExtensions
    {
        public static bool IsActive(this IHtmlHelper html, string? controller = null, string? action = null, string? area = null)
        {
            var route = html.ViewContext.RouteData.Values;
            var currentAction = route["action"]?.ToString();
            var currentController = route["controller"]?.ToString();
            var currentArea = route["area"]?.ToString();

            if (string.IsNullOrEmpty(controller))
                controller = currentController;

            if (string.IsNullOrEmpty(action))
                action = currentAction;

            if (string.IsNullOrEmpty(area))
                area = currentArea;
            return controller == currentController && action == currentAction && area == currentArea;
            //return controller == currentController && action == currentAction;
        }
        public static bool IsActiveTab(this IHtmlHelper html, string tab)
        {
            var currentTab = html.ViewContext.HttpContext.Request.Query["tab"].ToString();

            if (string.IsNullOrEmpty(currentTab))
                currentTab = "personal";

            return string.Equals(currentTab, tab, StringComparison.OrdinalIgnoreCase);
        }
        //public static string IsActive(this IHtmlHelper html,
        //string? controller = null,
        //string? action = null,
        //string? area = null)
        //{
        //    var route = html.ViewContext.RouteData.Values;

        //    var currentController = route["controller"]?.ToString();
        //    var currentAction = route["action"]?.ToString();
        //    var currentArea = route["area"]?.ToString();

        //    bool active =
        //        (controller == null || controller == currentController) &&
        //        (action == null || action == currentAction) &&
        //        (area == null || area == currentArea);

        //    return active ? "active" : "";
        //}

        public static IHtmlContent GetHtml(this TagBuilder tagBuilder)
        {
            using (var writer = new StringWriter())
            {
                tagBuilder.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
                return new HtmlString(writer.ToString());
            }
        }

        public static IHtmlContent EnumStatusFor<TModel, TValue>(this IHtmlHelper<TModel> html, Expression<Func<TModel, TValue>> expression)
        {
            Type enumType = typeof(TValue);
            if (!enumType.IsEnum)
                return html.DisplayFor(expression);
            TValue enumValue = (expression.Compile())(html.ViewData.Model);
            string enumText = EnumHelper<TValue>.GetDisplayValue(enumValue);
            var fieldInfo = enumType.GetField(enumValue.ToString());
            if (fieldInfo == null)
                return html.DisplayFor(expression);

            var attributes = fieldInfo.GetCustomAttributes(typeof(StatusCssAttribute), false) as StatusCssAttribute[];
            if (attributes == null || attributes.Length == 0)
                return html.EnumDisplayFor(expression);
            return new HtmlString(string.Format("<span class=\"badge badge-{0}\">{1}</span>", attributes[0].Name, enumText));
        }

        public static IHtmlContent EnumDisplayFor<TModel, TValue>(this IHtmlHelper<TModel> html, Expression<Func<TModel, TValue>> expression)
        {
            Type enumType = typeof(TValue);
            if (!enumType.IsEnum)
                return html.DisplayFor(expression);

            TValue enumValue = (expression.Compile())(html.ViewData.Model);

            var fieldInfo = enumType.GetField(enumValue.ToString());
            if (fieldInfo == null)
                return html.DisplayFor(expression);

            var attributes = fieldInfo.GetCustomAttributes(typeof(DisplayAttribute), false) as DisplayAttribute[];
            if (attributes == null || attributes.Length == 0)
                return html.DisplayFor(expression);

            return new HtmlString(attributes[0].Name);
        }
        public static IHtmlContent DetailsLink(this IHtmlHelper html, string actionName, object routeValues, bool openModal = false)
        {
            var urlHelperFactory = html.ViewContext.HttpContext.RequestServices.GetRequiredService<IUrlHelperFactory>();
            var urlHelper = urlHelperFactory.GetUrlHelper(html.ViewContext);
            string actionLink = urlHelper.Action(actionName, routeValues);
            TagBuilder aTag = new TagBuilder("a");
            aTag.InnerHtml.SetHtmlContent("<i class='fas fa-folder'></i> Chi tiết");
            aTag.MergeAttribute("href", actionLink);
            aTag.MergeAttribute("title", "Details");
            if (openModal)
                aTag.MergeAttribute("data-modal", "");
            aTag.AddCssClass("btn btn-primary btn-sm");
            return aTag.GetHtml();
        }

        public static IHtmlContent UpdateLink(this IHtmlHelper html, string actionName, object routeValues, bool openModal = false)
        {
            var urlHelperFactory = html.ViewContext.HttpContext.RequestServices.GetRequiredService<IUrlHelperFactory>();
            var urlHelper = urlHelperFactory.GetUrlHelper(html.ViewContext);
            string actionLink = urlHelper.Action(actionName, routeValues);
            TagBuilder aTag = new TagBuilder("a");
            aTag.InnerHtml.SetHtmlContent("<i class='fa fa-pencil-alt'></i> Sửa");
            //aTag.InnerHtml.SetHtmlContent("<i class='fa fa-pencil'></i>");
            aTag.MergeAttribute("href", actionLink);
            aTag.MergeAttribute("title", "Update");
            if (openModal)
                aTag.MergeAttribute("data-modal", "");
            aTag.AddCssClass("btn btn-info btn-sm ");
            return aTag.GetHtml();
        }

        public static IHtmlContent DeleteLink(this IHtmlHelper html, string actionName, object routeValues, bool openModal = false)
        {
            var urlHelperFactory = html.ViewContext.HttpContext.RequestServices.GetRequiredService<IUrlHelperFactory>();
            var urlHelper = urlHelperFactory.GetUrlHelper(html.ViewContext);
            string actionLink = urlHelper.Action(actionName, routeValues);
            TagBuilder aTag = new TagBuilder("a");
            aTag.InnerHtml.SetHtmlContent("<i class='fas fa-trash'></i> Xóa");
            aTag.MergeAttribute("href", actionLink);
            aTag.MergeAttribute("title", "Delete");
            if (openModal)
            {
                aTag.MergeAttribute("data-modal", "");
            }
            aTag.AddCssClass("btn btn-danger btn-sm");
            return aTag.GetHtml();
        }
        public static IHtmlContent SummerNote(this IHtmlHelper htmlHelper, string name, object config = null)
        {
            return htmlHelper.Editor(name, "SummerNote", new { Config = config });
        }

        public static IHtmlContent SummerNoteFor<TModel, TProperty>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, object config = null)
        {
            return htmlHelper.EditorFor(expression, "SummerNote", new { Config = config });
        }

        public static IHtmlContent DatePicker(this IHtmlHelper htmlHelper, string name, object config = null)
        {
            return htmlHelper.Editor(name, "DatePicker", new { Config = config });
        }

        public static IHtmlContent DatePickerFor<TModel, TProperty>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, object config = null)
        {
            var fieldValue = htmlHelper.DisplayFor(expression);
            return htmlHelper.EditorFor(expression, "DatePicker", new { Config = config, Value = fieldValue });
        }

        public static IHtmlContent DateTimePicker(this IHtmlHelper htmlHelper, string name, object config = null)
        {
            return htmlHelper.Editor(name, "DateTimePicker", new { Config = config });
        }

        public static IHtmlContent DateTimePickerFor<TModel, TProperty>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, object config = null)
        {
            var fieldValue = htmlHelper.DisplayFor(expression);
            return htmlHelper.EditorFor(expression, "DateTimePicker", new { Config = config, Value = fieldValue });
        }
    }
}
