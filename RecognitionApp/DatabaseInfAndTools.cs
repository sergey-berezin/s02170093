namespace RecognitionApp
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.EntityFrameworkCore;

    public class ImageObj
    {
        public int Id { get; set; }
        public string Path { get; set; }
        public Blob ImageDetails { get; set; }
        public float Confidence { get; set; }

        public LabelObj LabelObject { get; set; }
    }

    public class Blob
    {
        public int Id { get; set; }
        public byte[] Image { get; set; }
    }

    public class LabelObj
    {
        public int Id { get; set; }
        public int Label { get; set; }
        public int StatCount { get; set; }

        public ICollection<ImageObj> ImageObjs { get; set; }
    }

    public class LibraryContext : DbContext
    {
        public DbSet<LabelObj> LabelObjs { get; set; }
        public DbSet<ImageObj> ImageObjs { get; set; }
        public DbSet<Blob> ImageDetails { get; set; }

        string dir = @"library.db";
        protected override void OnConfiguring(DbContextOptionsBuilder o) => o.UseSqlite("Data Source = " + dir);

        public void ClearDb()
        {
            foreach (var item in ImageObjs)
            {
                ImageObjs.Remove(item);
            }

            foreach (var item in ImageDetails)
            {
                ImageDetails.Remove(item);
            }

            foreach (var item in LabelObjs)
            {
                LabelObjs.Remove(item);
            }

            SaveChanges();
        }

        public Tuple<int, float, int> FindResults(string path)
        {
            Tuple<int, float, int> tmpInf = null;
            byte[] img = File.ReadAllBytes(path);
            ImageObj el;
            try
            {
                el = ImageObjs.First(i => i.Path == path);
            }
            catch (InvalidOperationException)
            {
                el = null;
            }

            if (el != null)
            {
                Entry(el).Reference(i => i.LabelObject).Load();
                Entry(el).Reference(i => i.ImageDetails).Load();
                if (img.Length == el.ImageDetails.Image.Length)
                {
                    bool check = true;
                    for (int i = 0; i < img.Length; i++)
                    {
                        if (img[i] != el.ImageDetails.Image[i])
                        {
                            check = false;
                            break;
                        }
                    }

                    if (check)
                    {
                        tmpInf = new Tuple<int, float, int>(el.LabelObject.Label, el.Confidence, el.LabelObject.StatCount);
                    }
                }
            }

            return tmpInf;
        }

        public void AddResults(string path, int l, float c)
        {
            ImageObj imageObj = new ImageObj();
            imageObj.Path = path;
            byte[] img = File.ReadAllBytes(path);
            imageObj.ImageDetails = new Blob() { Image = img };
            imageObj.Confidence = c;

            LabelObj el;
            try
            {
                el = LabelObjs.First(i => i.Label == l);
                Entry(el).Collection(i => i.ImageObjs).Load();
            }
            catch (InvalidOperationException)
            {
                el = null;
            }

            if (el == null)
            {
                imageObj.LabelObject = new LabelObj() { Label = l, StatCount = 1 };
                imageObj.LabelObject.ImageObjs = new List<ImageObj>();
                imageObj.LabelObject.ImageObjs.Add(imageObj);

                ImageObjs.Add(imageObj);
                LabelObjs.Add(imageObj.LabelObject);
                ImageDetails.Add(imageObj.ImageDetails);
            }
            else
            {
                imageObj.LabelObject = el;
                imageObj.LabelObject.StatCount++;
                imageObj.LabelObject.ImageObjs.Add(imageObj);

                ImageObjs.Add(imageObj);
                ImageDetails.Add(imageObj.ImageDetails);
            }

            SaveChanges();
        }
    }
}
