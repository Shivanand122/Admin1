//using Admin.Data;
//using Admin.Models;
using AdminDAL.Entities2;
using AdminDAL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdminDAL.Context;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Configuration;
using Newtonsoft.Json;
using Microsoft.FeatureManagement;

namespace Admin.Controllers
{
   // [Authorize]
    public class AdminMController : Controller
    {
        private string apiUrl;
        private readonly IFeatureManager _featureManager;
        private readonly IFeatureRepository _featureRepository;
        private readonly AdminCont _db;
        private List<PendingChangeModel> pendingChanges = new List<PendingChangeModel>();




        public AdminMController(IFeatureRepository featureRepository, AdminCont db, IConfiguration configuration, IFeatureManager featureManager)
        {
            apiUrl = configuration.GetValue<string>("ApiUrl");
            _featureRepository = featureRepository;
            _db = db;
            _featureManager = featureManager;
        }

        [HttpGet]
        public IActionResult IsAdminSideEnabled()
        {
            // Replace this with your actual logic to check the feature flag status
            var isAdminSideEnabled = _featureManager.IsEnabledAsync("AdminSide").Result;

            return Json(isAdminSideEnabled);
        }


        //private readonly AdminContext _db;


        public class PendingChangeModel
        {
            public int FeatureID { get; set; }
            public string Action { get; set; }
            public string Comment { get; set; }
        }

        [AllowAnonymous]
        public IActionResult Log()
        {
            return View();
        }

        /*  [HttpPost]
          public IActionResult Login()
          {
            *//*  // Check the credentials (you should use a more secure authentication mechanism)
              if (model.Username == "yourUsername" && model.Password == "yourPassword")
              {
                  // For simplicity, use session to store the authentication status
                  HttpContext.Session.SetString("IsAuthenticated", "true");

                  return RedirectToAction("Index");
              }

              // Invalid credentials, show error
              ViewBag.Error = "Invalid username or password";*//*
              return View();
          }
  */
        
        public async Task<IActionResult> Index()
        {

            List<Feature> chartdata = await GetChartData();

            string serializedata = Newtonsoft.Json.JsonConvert.SerializeObject(chartdata);

            ViewBag.ChartData = serializedata;

            IEnumerable<Feature> features = _featureRepository.GetAllFeatures();
            // .Where(feature => feature.UserName == "User1")

            // Pass the features to the view
            return View(features);

            //if (User.HasClaim(c => c.Type == "Has2FA" && c.Value == "true"))
            //{
                
            //}
            //else
            //{
            //    // Redirect to the 2-factor authentication page
            //    return RedirectToAction("Log");
            //}
            
        }

        [AllowAnonymous]
        public IActionResult Authentication()
        {
            return View();
        }




        [HttpPost]
        public IActionResult UpdateStatus(int featureId, int newStatus)
        {
            try
            {
                // Retrieve the feature from the database
                Feature feature = _db.Features.FirstOrDefault(f => f.FeatureId == featureId);

                if (feature == null)
                {
                    // Handle the case where the feature is not found
                    return NotFound();
                }

                // Update the status
                feature.ApprovalStatus = (byte)newStatus;

                if ((byte)newStatus == 0)
                {
                    TempData["success"] = "Approved successfully";
                }
                else
                {
                    TempData["error"] = "Rejected successfully";
                }

                // Save changes to the database
                _db.SaveChanges();

                // Signal that the change has been successfully made
                return Ok(new { FeatureId = featureId, NewStatus = newStatus });
            }
            catch (Exception ex)
            {
                // Handle exceptions, log errors, etc.
                return BadRequest($"Error updating status: {ex.Message}");
            }
        }

        private static List<PendingChangeModel> _pendingChanges = new List<PendingChangeModel>();

        [HttpPost]
        public IActionResult UpdateComment(int featureId, string comment)
        {
            Console.WriteLine(comment);
            try
            {
                Feature feature = _db.Features.FirstOrDefault(f => f.FeatureId == featureId);

                if (feature == null)
                {
                    return NotFound();
                }

                feature.AdminComments += $" {comment}"; // Assuming AdminComments is the field for storing comments

                _pendingChanges.Add(new PendingChangeModel
                {
                    FeatureID = featureId,
                    Action = "Comment",
                    Comment = comment // Include the comment in the pending changes
                });

                //_db.SaveChanges();

                TempData["error"] = "Comment updated successfully";

                return Ok("Comment updated successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating comment: {ex.Message}");
                return BadRequest($"Error updating comment: {ex.Message}");
            }
        }






        [HttpPost]
        public async Task<IActionResult> UpdateAllChangesAsync()
        {
            try
            {
                foreach (var change in _pendingChanges.ToList())
                {
                    var feature = _db.Features.FirstOrDefault(f => f.FeatureId == change.FeatureID);

                    if (feature != null)
                    {
                        switch (change.Action.ToLower())
                        {
                            case "accept":


                                // Assuming your Azure Function URL and payload format
                                var functionUrl = "https://meshapp1.azurewebsites.net/api/FunctionApp1?code=lFcSH3N5USMOqD73vw9RHU4QviZFcRDnllLDtAmHkTonAzFu3CXKfA==";


                                using (var client = new HttpClient())
                                {
                                    var response = await client.GetAsync(functionUrl);

                                    if (response.IsSuccessStatusCode)
                                    {
                                        // Azure Function triggered successfully
                                        // Handle any further logic after Azure Function invocation
                                        feature.ApprovalStatus = 0;
                                    }
                                    else
                                    {
                                        Console.WriteLine("Failed to trigger Azure Function");
                                        // Handle the failure scenario accordingly
                                    }
                                }
                                break;

                            case "reject":
                                feature.ApprovalStatus = 1;
                                break;
                            case "pending":
                                feature.ApprovalStatus = 2;

                                break;
                            case "comment":
                                 feature.AdminComments = change.Comment;
                                break;
                                // Handle other cases if needed
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Feature with ID {change.FeatureID} not found.");
                    }

                    _pendingChanges.Remove(change);
                }

                // Save changes to the database
                _db.SaveChanges();
                TempData["error"] = "Comment updated successfully";
                return Ok("All changes applied successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating all changes: {ex.Message}");
                return BadRequest($"Error updating all changes: {ex.Message}");
            }
        }







        [HttpGet]
        public IActionResult FeatureDetails(int featureId)
        {
            Feature feature = _db.Features.Include(f => f.EntityNameNavigation).FirstOrDefault(f => f.FeatureId == featureId);

            if (feature == null)
            {
                return NotFound();
            }

            return PartialView("_FeatureDetailsPartial", feature); // Pass a single Feature object
        }





        public void ClearPendingChanges()
        {
            _pendingChanges.Clear();
        }






        [HttpPost]
        public IActionResult SearchFeatures(string searchTerm)
        {
            try
            {
                // Perform a search in your database based on the searchTerm
                // Assuming 'FeatureName' is the property you want to search against

                IEnumerable<Feature> searchedFeatures = _db.Features
                    .Where(f => f.FeatureName.Contains(searchTerm))
                    .ToList();

                if (searchedFeatures.Any())
                {
                    // Assuming 'Index' view is designed to display the searched features
                    return View("Index", searchedFeatures);
                }
                else
                {
                    TempData["error"] = "No features found matching the search term";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching features: {ex.Message}");
                return BadRequest($"Error searching features: {ex.Message}");
            }
        }







        [HttpPost]
        public IActionResult AcceptFeature(int featureId)
        {
            try
            {
                // Check if there's an existing pending change for the same feature
                var existingChange = _pendingChanges.FirstOrDefault(p => p.FeatureID == featureId);

                if (existingChange != null)
                {
                    // Update the existing pending change action to 'Accept'
                    existingChange.Action = "Accept";
                }
                else
                {
                    // Add a new pending change for acceptance
                    _pendingChanges.Add(new PendingChangeModel
                    {
                        FeatureID = featureId,
                        Action = "Accept"
                    });
                }

                return Ok("Feature marked for acceptance");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error marking feature for acceptance: {ex.Message}");
                return BadRequest($"Error marking feature for acceptance: {ex.Message}");
            }
        }

        [HttpPost]
        public IActionResult RejectFeature(int featureId)
        {
            try
            {
                // Check if there's an existing pending change for the same feature
                var existingChange = _pendingChanges.FirstOrDefault(p => p.FeatureID == featureId);



                if (existingChange != null)
                {
                   
                    existingChange.Action = "Reject";
                   
                }
                else
                {
                    // Add a new pending change for rejection
                    _pendingChanges.Add(new PendingChangeModel
                    {
                        FeatureID = featureId,
                        Action = "Reject"
                    });
                }


                return Ok("Feature marked for rejection");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error marking feature for rejection: {ex.Message}");
                return BadRequest($"Error marking feature for rejection: {ex.Message}");
            }
        }





        [HttpGet]
        public IActionResult CommentBox(int featureId)
        {
            // Assuming _db is your database context and Feature is your model
            var feature = _db.Features.FirstOrDefault(f => f.FeatureId == featureId);

            if (feature == null)
            {
                return NotFound();
            }

            return PartialView("_CommentBoxPatial", feature);
            
        }




        [HttpGet]
        public async Task<List<Feature>> GetChartData()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(apiUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        string jsoncontent = await response.Content.ReadAsStringAsync();
                        var features = JsonConvert.DeserializeObject<List<Feature>>(jsoncontent);

                        var featuresWithNoNullComments = features.Select(feature =>
                        {
                            feature.AdminComments ??= "No Comment";
                            return feature;
                        });

                        return featuresWithNoNullComments.ToList();
                    }
                    else
                    {
                        response.EnsureSuccessStatusCode();
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HttpRequestException: {ex.Message}");
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"JsonException: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }

            return new List<Feature>();
        }



        [HttpGet]
        public async Task<IActionResult> BarChart()
        {
            try
            {
                List<Feature> chartdata = await GetChartData();

                string serializedata = Newtonsoft.Json.JsonConvert.SerializeObject(chartdata);

                ViewBag.ChartData = serializedata;

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"BarChart Exception: {ex.Message}");

                return View("Error");
            }
        }

    }
}
    
 