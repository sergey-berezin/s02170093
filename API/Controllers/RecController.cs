namespace API.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using DigitRecognitionLibrary;
    using Microsoft.AspNetCore.Mvc;
    using Contracts;

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
                pred.Add(new TransferFile() { Path = path, Label = res.Item1, Confidence = res.Item2, Image = tempImg });
            }

            return pred;
        }

        [HttpGet]
        public IEnumerable<string> Get()
        {
            using var LibContext = new LibraryContext();
            var q = from item in LibContext.LabelObjs
                    orderby item.Label
                    select item.Label.ToString() + " " + item.StatCount.ToString();

            return q.ToArray();
        }

        [HttpGet("{label}")]
        public string GetImage(int label)
        {
            List<string> images = new List<string>();
            using var LibContext = new LibraryContext();
            var byteImg = from item in LibContext.ImageObjs
                    where item.LabelObject.Label == label
                    select item.ImageDetails.Image;

            string res = "";
            foreach (var item in byteImg)
            {
                res += Convert.ToBase64String(item);
                res += ',';
            }

            res = res.Remove(res.Length - 1);
            return res;
        }

        [HttpDelete]
        public void Delete()
        {
            using var LibContext = new LibraryContext();
            LibContext.ClearDb();
        }
    }
}
