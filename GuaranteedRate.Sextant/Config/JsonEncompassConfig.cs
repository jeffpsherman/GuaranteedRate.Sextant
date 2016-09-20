﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using EllieMae.Encompass.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GuaranteedRate.Sextant.Config
{
    /// <summary>
    /// provides a simple config fild parser for JSON files.  Defaults to ASCII encoding
    /// </summary>
    public class JsonEncompassConfig : IEncompassConfig
    {
        private JObject _jsonObject = null;
        private Encoding _encoding = Encoding.ASCII;
        private string _configPath = "Sextant.json";

        /// <summary>
        /// Returns all keys in the json config.  
        /// </summary>
        /// <returns>String collection of keys</returns>
        public ICollection<string> GetKeys()
        {
            return _jsonObject.Descendants().Select(aa => aa.Path).Distinct().ToList();
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public JsonEncompassConfig()
        {
        }

        /// <summary>
        /// Initializes the config from a fixed string.  Useful for testing.
        /// </summary>
        /// <param name="configText">The json text.</param>
        /// <returns>true for success, throws exception otherwise</returns>
        [Obsolete("Use LoadFromString instead.")]
        public JsonEncompassConfig(string configText)
        {
            LoadFromString(configText);
        }

        /// <summary>
        /// Returns the value of the given key
        /// </summary>
        /// <param name="key">Full path to the key(e.g. "MyKey" or "Keys[0].MySubKey")</param>
        /// <param name="defaultVal">Default value if the key is null</param>
        /// <returns></returns>
        public bool GetValue(string key, bool defaultVal)
        {
            return Boolean.Parse(GetValue(key, defaultVal.ToString()));
        }

        /// <summary>
        /// Returns the value of the given key
        /// </summary>
        /// <param name="key">Full path to the key(e.g. "MyKey" or "Keys[0].MySubKey")</param>
        /// <param name="defaultVal">Default value if the key is null</param>
        /// <returns></returns>
        public string GetValue(string key, string defaultVal = null)
        {
            var val = _jsonObject.SelectToken(key);

            if (val == null)
            {
                return defaultVal;
            }
            return val.ToString();
        }


        /// <summary>
        /// Initializes the config from a default "sextant.json" file.
        /// </summary>
        /// <param name="session">Current Encompass session</param>
        /// <returns>true for success, throws exception otherwise</returns>
        public bool Init(Session session)
        {
            return Init(session, _configPath);
        }


        /// <summary>
        /// Reloads the config from a default "sextant.json" file.
        /// </summary>
        /// <param name="session">Current Encompass session</param>
        /// <returns>true for success, throws exception otherwise</returns>
        public bool Reload(Session session)
        {
            return Reload(session, _configPath);
        }


        /// <summary>
        /// Initializes the config from this file.
        /// </summary>
        /// <param name="session">Current Encompass session</param>
        /// <param name="configPath">Name of the config file (e.g. "myconfig.json")</param>
        public bool Init(Session session, string configPath)
        {
            return Init(session, configPath, Encoding.ASCII);
        }

        /// <summary>
        /// Initializes the config from this file.
        /// </summary>
        /// <param name="session">Current Encompass session</param>
        /// <param name="configPath">Name of the config file (e.g. "myconfig.json")</param>
        /// <param name="encoding">Encoding of the file.</param>
        /// <returns>true for success, throws exception otherwise</returns>
        public bool Init(Session session, string configPath, Encoding encoding)
        {
            try
            {
                _encoding = encoding;
                _configPath = configPath;
                var configText = session.DataExchange.GetCustomDataObject(_configPath);
                _jsonObject = JObject.Parse(configText.ToString(_encoding));
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Could not load config file {0}", configPath), ex);
            }
        }

        /// <summary>
        /// Reloads the config file
        /// </summary>
        /// <param name="session">Current Encompass session</param>
        /// <param name="configPath">Name of the config file e.g. "foo.json"</param>
        /// <returns>true for success, throws exception otherwise</returns>
        public bool Reload(Session session, string configPath)
        {
            return Init(session, configPath);
        }


        /// <summary>
        /// Returns a sub-section of the config.
        /// </summary>
        /// <param name="key">Json key of the section you want.</param>
        /// <returns>A JsonEncompassConfig created from the subsection.</returns>
        public IEncompassConfig GetConfigGroup(string key)
        {
            var val = _jsonObject.SelectToken(key);

            if (val == null)
            {
                return null;
            }

            return new JsonEncompassConfig(val.ToString());
        }

        /// <summary>
        /// Loads the config from the give string.  Useful for testing.
        /// </summary>
        /// <param name="configAsString">The json string</param>
        /// <returns></returns>
        public bool LoadFromString(string configAsString)
        {
            try
            {
                _jsonObject = JObject.Parse(configAsString);
                return true;

            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}