using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Twilio.Rest.Api.V2010.Account;
using Twilio.AspNet.Common;
using Twilio.TwiML;
using Microsoft.AspNetCore.Http;
using PhotoFiddler.Helpers;

namespace PhotoFiddler.Controllers
{
    [Route("/api/photo")]
    [ApiController]
    public class PhotoController : ControllerBase
    {

        private readonly IPhotoProcessor _photoProcessor;

        public PhotoController(IPhotoProcessor photoProcessor)
        {
            _photoProcessor = photoProcessor;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Content("Hello!");
        }

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            var incomingImage = Request.Form["MediaUrl0"];
            var sid = Request.Form["MessageSid"];
            //var host = HttpContext.Request.Host.ToString();
            var host = "https://layla.ngrok.io/";

            var processedImage = await _photoProcessor.Process(incomingImage, sid, host);

            var twiml = $@"<Response>
                                <Message>
                                    <Media>{processedImage}</Media>
                                </Message>
                            </Response>";

            return new ContentResult { Content = twiml, ContentType = "application/xml" };
        }
    }
}
