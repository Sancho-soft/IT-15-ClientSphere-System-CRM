using System.Diagnostics;
using ClientSphere.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClientSphere.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult SecurityPolicy()
        {
            return View();
        }

        [Route("api/geographic/{*path}")]
        public async Task<IActionResult> GeographicProxy(string path)
        {
            try
            {
                var query = Request.QueryString.ToString();
                // Ensure target URL matches gitlab.io API layout
                var targetUrl = $"https://psgc.gitlab.io/api/{path}{query}";
                
                using var httpClient = new System.Net.Http.HttpClient();
                var response = await httpClient.GetAsync(targetUrl);
                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode);
                }
                
                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
