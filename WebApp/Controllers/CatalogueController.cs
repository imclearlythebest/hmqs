using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;
public class CatalogueController() : Controller
{
	[HttpGet]
	public IActionResult Edit(string fileName)
	{
		if (string.IsNullOrWhiteSpace(fileName))
		{
			return RedirectToAction("Index", "Home");
		}

		ViewData["HidePlayer"] = true;
		ViewData["HideSidebar"] = true;
		ViewData["FileName"] = fileName;

		return View();
	}
}