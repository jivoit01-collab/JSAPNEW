using JSAPNEW.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using JSAPNEW.Models;
using ServiceStack;

namespace JSAPNEW.Controllers
{
    public class BPmasterwebController : Controller
    {
        private readonly IBPmasterService _BPService;
        public BPmasterwebController(IBPmasterService  BPService)
        {
            _BPService = BPService;
        }
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("userId");
            var selectedCompanyId = HttpContext.Session.GetInt32("selectedCompanyId");
            if (userId == null || selectedCompanyId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.CompanyId = selectedCompanyId;
            int company = selectedCompanyId.Value;
           
            ViewBag.SPname= await _BPService.GetSLPnameAsync(company);
            ViewBag.MSMEData = await _BPService.GetMSMEtypeAsync(company);
            ViewBag.Country = await _BPService.GetCountryAsync(company);
            return View();
        }

        public async Task<IActionResult> Index1()
        {
            var userId = HttpContext.Session.GetInt32("userId");
            var selectedCompanyId = HttpContext.Session.GetInt32("selectedCompanyId");
            if (userId == null || selectedCompanyId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.CompanyId = selectedCompanyId;
            int company = selectedCompanyId.Value;

            ViewBag.SPname = await _BPService.GetSLPnameAsync(company);
            ViewBag.MSMEData = await _BPService.GetMSMEtypeAsync(company);
            ViewBag.Country = await _BPService.GetCountryAsync(company);
            return View();
        }
    }

}
