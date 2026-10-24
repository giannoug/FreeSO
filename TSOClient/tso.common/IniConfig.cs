using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace FSO.Common
{
    public abstract class IniConfig
    {
        public abstract string HeadingComment { get; }
        public string ActivePath { get; private set; }

        public abstract Dictionary<string, string> DefaultValues
        {
            get; set;
        }

        private void SetValue(string key, string value)
        {
            var prop = this.GetType().GetProperty(key);
            if (prop != null)
            {
                try
                {
                    if (prop.PropertyType != typeof(string))
                        prop.SetValue(this, Convert.ChangeType(value, prop.PropertyType, CultureInfo.InvariantCulture));
                    else prop.SetValue(this, value);
                }
                catch (Exception) { }
            }
        }

        public IniConfig(string path)
        {
            ActivePath = path;
            Load();
        }

        public void Load()
        {
            //assume default values for all unset properties
            foreach (var pair in DefaultValues)
            {
                SetValue(pair.Key, pair.Value);
            }

            if (!File.Exists(ActivePath))
            {
                Save();
            } else
            {
                var lines = File.ReadAllLines(ActivePath);
                foreach (var line in lines)
                {
                    var clean = line.Trim();
                    if (clean.Length == 0 || clean[0] == '#' || clean[0] == '[') continue;
                    var split = clean.IndexOf('=');
                    if (split == -1) continue; //?
                    var prop = clean.Substring(0, split).Trim();
                    var value = clean.Substring(split+1).Trim();

                    SetValue(prop, value);
                }
            }
        }

        protected virtual object GetSaveValue(string name, object value) => value;

        public void Save() => TrySave(out _);

        public bool TrySave(out string error)
        {
            string temporary = null;
            try
            {
                var destination = Path.GetFullPath(ActivePath);
                temporary = Path.Combine(Path.GetDirectoryName(destination), Path.GetRandomFileName());
                using (var stream = new StreamWriter(File.Open(temporary, FileMode.CreateNew, FileAccess.Write)))
                {
                    stream.WriteLine($"# {HeadingComment}");
                    foreach (var prop in GetType().GetProperties())
                    {
                        if (prop.Name == "Default" || prop.Name == "DefaultValues") continue;
                        stream.WriteLine(prop.Name + "=" + Convert.ToString(GetSaveValue(prop.Name, prop.GetValue(this)), CultureInfo.InvariantCulture));
                    }
                }
                File.Move(temporary, destination, true);
                error = null;
                return true;
            }
            catch (Exception e)
            {
                error = e.GetBaseException().Message;
                return false;
            }
            finally
            {
                if (temporary != null)
                {
                    try { File.Delete(temporary); }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
            }
        }
    }
}
