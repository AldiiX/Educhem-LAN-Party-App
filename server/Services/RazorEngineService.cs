using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace server.Services;

public static class RazorEngineService {
	public static async Task<string> RenderViewToStringAsync<TModel>(IServiceProvider requestServices, string viewName, TModel model) {
		var viewEngine = requestServices.GetRequiredService<IRazorViewEngine>();
		var viewEngineResult = viewEngine.GetView(null, viewName, false);
		if (viewEngineResult.View == null) {
			throw new Exception("Could not find the View file. Searched locations:\r\n" + string.Join("\r\n", viewEngineResult.SearchedLocations));
		}

		var httpContextAccessor = requestServices.GetRequiredService<IHttpContextAccessor>();
		var httpContext = httpContextAccessor.HttpContext ?? new DefaultHttpContext { RequestServices = requestServices };
		var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
		var tempDataProvider = requestServices.GetRequiredService<ITempDataProvider>();

		using var outputStringWriter = new StringWriter();
		var viewContext = new ViewContext(
			actionContext,
			viewEngineResult.View,
			new ViewDataDictionary<TModel>(new EmptyModelMetadataProvider(), new ModelStateDictionary()) { Model = model },
			new TempDataDictionary(actionContext.HttpContext, tempDataProvider),
			outputStringWriter,
			new HtmlHelperOptions()
		);

		await viewEngineResult.View.RenderAsync(viewContext);

		return outputStringWriter.ToString();
	}
}
