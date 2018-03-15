using IrisClassifier.Cntk;
using log4net;
using Newtonsoft.Json;
using System;
using System.Web.Http;

namespace IrisClassifier.WebApi
{
    [RoutePrefix("api/v1/iris")]
    public class WebApiController : ApiController
    {
        private readonly ILog log = LogManager.GetLogger("controller");

        [HttpGet]
        [Route("health_check")]
        public string Connect()
        {
            return "Up and running!";
        }

        [HttpGet]
        [Route("train_model")]
        public IHttpActionResult CreateModel()
        {
            try
            {
                log.Debug("Creating model...");
                var accuracy = CntkLib.CreateAndTrainModel();
                log.Debug("Model has been created.");
                var json = JsonConvert.SerializeObject(accuracy, new DecimalJsonConverter());
                return Ok(json);
            }
            catch(Exception ex)
            {
                log.Error(ex.ToString());
            }
            return Ok("Failed");
        }

        [HttpPost]
        [Route("evaluate")]
        public IHttpActionResult Evaluate(ClassifyRequest request)
        {
            try
            {
                log.Debug("Running existing model...");
                var result = CntkLib.Evaluate(request.Input);
                log.Debug("Model returned successfull result.");
                return Ok(result);
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
            }
            return Ok("Failed");
        }
    }
}
