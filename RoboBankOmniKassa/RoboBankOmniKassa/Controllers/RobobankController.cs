using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Specialized;
using System.Net;
using RoboBankOmniKassa.Models;
namespace RoboBankOmniKassa.Controllers
{
    public class RobobankController : Controller
    {
        // Test  
        private const String PostUrl = @"https://payment-webinit.simu.omnikassa.rabobank.nl/paymentServlet";
        // Production  
        //private const String PostUrl = https://payment-webinit.omnikassa.rabobank.nl/paymentServlet  
        private const String SecurityKey = @"002020000000001_KEY1";
        private const String MerchantId = @"002020000000001";
        private const String SecurityKeyVersion = "1";
        private const String CurrencyCode = "978"; // Euro  
        private const String LanguageCode = "EN"; // Euro    
        // GET: Robobank
        public ActionResult Index()
        {
            return View();
        }

        // GET: Robobank
        public ActionResult Pay()
        {
            RobobankViewModel VM = new RobobankViewModel();
            VM.Amount = 50;
            VM.OrderID = DateTime.Now.Ticks.ToString();

            return View(VM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Pay(RobobankViewModel model)
        {
            if (ModelState.IsValid)
            {
                string data = String.Format("merchantId={0}", MerchantId)
              + String.Format("|orderId={0}", model.OrderID)
              + String.Format("|amount={0}", model.Amount)
              + String.Format("|customerLanguage={0}", LanguageCode)
              + String.Format("|keyVersion={0}", SecurityKeyVersion)
              + String.Format("|currencyCode={0}", CurrencyCode)
              // + String.Format("|PaymentMeanBrandList={0}", "IDEAL")
              + String.Format("|normalReturnUrl={0}", "http://www." + Request.Url.Host + "/Robobank/RedirectResponse")
              + String.Format("|automaticResponseUrl={0}", "http://www." + Request.Url.Host + "/Robobank/RedirectResponse")
              + String.Format("|transactionReference={0}", model.OrderID + "x" + DateTime.Now.ToString("hhmmss"));

                // Seal-veld berekenen
                SHA256 sha256 = SHA256.Create();
                byte[] hashValue = sha256.ComputeHash(new UTF8Encoding().GetBytes(data + SecurityKey));

                // POST data samenstellen
                NameValueCollection postData = new NameValueCollection();
                postData.Add("Data", data);
                postData.Add("Seal", ByteArrayToHexString(hashValue));
                postData.Add("InterfaceVersion", "HP_1.0");

                // Posten van data 
                byte[] response;
                using (WebClient client = new WebClient())
                    response = client.UploadValues(PostUrl, postData);

                TempData["Response"] = Encoding.UTF8.GetString(response);
                return RedirectToAction("Confirm", "Robobank");

            }

            return View(model);
        }
        // Converteer een String naar Hexadecimale waarde
        public string ByteArrayToHexString(byte[] bytes)
        {
            StringBuilder result = new StringBuilder(bytes.Length * 2);
            const string hexAlphabet = "0123456789ABCDEF";

            foreach (byte b in bytes)
            {
                result.Append(hexAlphabet[b >> 4]);
                result.Append(hexAlphabet[b & 0xF]);
            }

            return result.ToString();
        }
        List<KeyValuePair<String, String>> ParseData(String data)
        {
            return (from part in data.Split('|')
                    let key = part.Split('=')[0]
                    let value = part.Split('=')[1]
                    select new KeyValuePair<String, String>(key, value)).ToList();
        }

        static String GetTransactionStatus(String transactionCode)
        {
            switch (transactionCode)
            {
                case "00":
                    return "SUCCESS";
                case "60":
                    return "PENDING";
                case "97":
                    return "EXPIRED";
                case "17":
                    return "CANCELLED";
            }
            return "FAILED";
        }

        public ActionResult Confirm(string response)
        {
            ViewBag._Response = TempData["Response"];
            return View();
        }


        public ActionResult Report()
        {

            return View();
        }

        public ActionResult RedirectResponse()
        {
            ViewBag._Url = "URL = http://" + Request.Url.Host + "/Robobank/RedirectResponse";
            return View();
        }
        [HttpPost]
        public ActionResult RedirectResponse(string Data, string Seal)
        {
            String data = Data;
            String seal = Seal;

            // Verifieer de Seal
            SHA256 sha256 = SHA256.Create();
            byte[] hashValue = sha256.ComputeHash(new UTF8Encoding().GetBytes(data + SecurityKey));

            if (seal.ToLower() == ByteArrayToHexString(hashValue).ToLower()) // Seal is goed
            {
                // Lees de gewenste waarden uit de server response
                List<KeyValuePair<String, String>> dataItems = ParseData(data);
                String transactionCode = dataItems.Where(i => i.Key == "transactionReference").First().Value;
                var responseCode = dataItems.Where(i => i.Key == "responseCode").First().Value;
                String transactionStatus = GetTransactionStatus(responseCode);

                ViewBag._transactionCode = transactionCode;
                ViewBag._transactionStatus = transactionStatus;
            }
            else // Hash BAD
            {
                ViewBag._HashBad = "Invalid response from Rabo OmniKassa server";
            }
            return View();
        }


    }
}