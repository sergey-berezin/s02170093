namespace API.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using DigitRecognitionLibrary;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    [ApiController]
    [Route("[controller]")]
    public class RecController : ControllerBase
    {
        [HttpPost]
        public List<TransferFile> Post([FromBody] string dir)
        {
            List<TransferFile> pred = new List<TransferFile>();
            Recognition R = new Recognition();
            R.Run(dir);
            using var LibContext = new LibraryContext();
            foreach (var path in Directory.GetFiles(dir).Where(s => s.EndsWith(".png") || s.EndsWith(".jpg") || s.EndsWith(".bmp") || s.EndsWith(".gif")))
            {
                var byteImg = from item in LibContext.ImageObjs
                              where item.Path == path
                              select item.ImageDetails.Image;
                var tempImg = Convert.ToBase64String(byteImg.First());
                Tuple<int, float> res = LibContext.FindResults(path);
                pred.Add(new TransferFile() { Path = path, Label = res.Item1.ToString(), Confidence = res.Item2.ToString(), Image = tempImg });
            }

            return pred;
        }

        [HttpGet]
        public IEnumerable<string> Get()
        {
            using var LibContext = new LibraryContext();
            var q = from item in LibContext.LabelObjs
                    orderby item.Label
                    select "Label " + item.Label.ToString() + " is " + item.StatCount.ToString() + " time(s) in database.";
            return q.ToArray();
        }

        [HttpDelete]
        public void Delete()
        {
            using var LibContext = new LibraryContext();
            LibContext.ClearDb();
        }
    }

    public class TransferFile
    {
        public string Path { get; set; }
        public string Label { get; set; }
        public string Confidence { get; set; }
        public string Image { get; set; }
    }
}
