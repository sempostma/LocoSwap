using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace LocoSwap
{
    public class Route : ModelBase
    {
        protected XDocument RouteProperties;
        protected string _id;
        protected string _name;
        protected bool _isFavorite;
        protected static Dictionary<string, ZipFile> _cachedZipFiles;

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }
        public bool IsFavorite
        {
            get => _isFavorite;
            set => SetProperty(ref _isFavorite, value);
        }
        public string RouteDirectory
        {
            get
            {
                return GetRouteDirectory(Id);
            }
        }

        public Route()
        {
            Id = "";
            Name = "Name not available";
        }

        public Route(string id)
        {
            Load(id);
        }

        private ZipFile LoadAp(string apPath)
        {
            if (_cachedZipFiles.ContainsKey(apPath)) return _cachedZipFiles[apPath];
            var zipFile = ZipFile.Read(apPath);
            _cachedZipFiles[apPath] = zipFile;
            return zipFile;
        }

        public string GetFilePath(string filename)
        {
            string fullPath = Path.Combine(RouteDirectory, filename);
            if (File.Exists(fullPath) || Directory.Exists(fullPath))
            {
                return fullPath;
            }
            bool found = false;
            fullPath = Path.Combine(Utilities.GetTempDir(), filename);
            Utilities.RemoveFile(fullPath);
            string[] apFiles = Directory.GetFiles(RouteDirectory);
            foreach (string apPath in apFiles)
            {
                try
                {
                    var zipFile = LoadAp(apPath);
                    var apEntry = zipFile.Where(entry => entry.FileName == filename).FirstOrDefault();
                    if (apEntry == null) continue;
                    apEntry.Extract(Utilities.GetTempDir());
                    found = true;
                    break;
                }
                catch (Exception)
                {

                }
            }
            if (!found) throw new Exception($"{filename} not found for this route ID");
            return fullPath;
        }

        public void Load(string id)
        {
            Id = id;
            string xmlPath = GetFilePath("RouteProperties.xml");
            RouteProperties = XmlDocumentLoader.Load(xmlPath);

            XElement displayName = RouteProperties.XPathSelectElement("/cRouteProperties/DisplayName/Localisation-cUserLocalisedString");
            Name = Utilities.DetermineDisplayName(displayName);

            IsFavorite = Properties.Settings.Default.FavoriteRoutes?.IndexOf(Id) >= 0;
        }

        public static string GetRoutesDirectory()
        {
            return Path.Combine(Properties.Settings.Default.TsPath, "Content", "Routes");
        }

        public static string GetRouteDirectory(string routeId)
        {
            return Path.Combine(Properties.Settings.Default.TsPath, "Content\\Routes", routeId);
        }

        public static string[] ListAllRoutes()
        {
            List<string> ret = new List<string>();
            var routeDirectories = Directory.GetDirectories(GetRoutesDirectory());
            foreach (var directory in routeDirectories)
            {
                string id = new DirectoryInfo(directory).Name;
                string xmlPath = Path.Combine(directory, "RouteProperties.xml");
                if (File.Exists(xmlPath))
                {
                    ret.Add(id);
                    continue;
                }
                string[] apFiles = Directory.GetFiles(directory, "*.ap", SearchOption.TopDirectoryOnly);
                bool found = false;
                foreach (string apPath in apFiles)
                {
                    try
                    {
                        var zipFile = ZipFile.Read(apPath);
                        var xmlEntry = zipFile.Where(entry => entry.FileName == "RouteProperties.xml").FirstOrDefault();
                        if (xmlEntry == null) continue;
                        found = true;
                        break;
                    }
                    catch (Exception)
                    {

                    }
                }
                if (found) ret.Add(id);
            }
            return ret.ToArray();
        }
    }
}
