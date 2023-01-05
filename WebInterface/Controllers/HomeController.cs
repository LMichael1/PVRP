using Microsoft.AspNetCore.Mvc;
using PVRPSolver;
using System.Diagnostics;
using WebInterface.Models;

namespace WebInterface.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Info(IFormFile file)
        {
            if (file.Length > 0)
            {
                var filePath = Path.GetTempFileName();

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var parser = new Parser(filePath);
                await parser.Parse();

                var points = parser.Points;
                var vehicles = parser.Vehicles;
                var forecastDepth = parser.ForecastDepth;

                if (points.Count == 0 || vehicles.Count == 0)
                {
                    return RedirectToAction("UploadError");
                }

                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                var solver = new Solver(vehicles, points, forecastDepth);
                solver.GetInitialSolution();
                solver.RunOptimization();

                stopWatch.Stop();

                var solution = solver.Solution;
                solution.Routes = solution.Routes.Where(route => !route.IsEmpty).ToList();

                return View(solver.Solution);
            }

            return Ok();
        }

        public IActionResult UploadError()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}