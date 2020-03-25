using ExactOnline.Client.Sdk.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;

namespace ExactOnline.Client.Sdk.Helpers
{
    /// <summary>
    /// Class for stripping unnecessary Json tags from API Response
    /// </summary>
    public class ApiResponseCleaner
    {
        #region Public methods

        /// <summary>
        /// Fetch Json Object (Json within ['d'] name/value pair) from response
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public static string GetJsonObject(string response)
        {
            var oldCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            string output;
            try
            {
                var dict = JObject.Parse(response);
                var d = (JObject)dict["d"];
                output = GetJsonFromDictionary(d).ToString(Formatting.None);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = oldCulture;
            }
            return output;
        }

        public static string GetSkipToken(string response)
        {
            var oldCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            var token = string.Empty;

            try
            {
                var dict = JObject.Parse(response);
                if (dict["d"] is JObject innerPart)
                {
                    if (innerPart.TryGetValue("__next", out var value))
                    {

                        var next = value.Value<string>();

                        // Skiptoken has format "$skiptoken=xyz" in the url and we want to extract xyz.
                        var match = Regex.Match(next ?? "", @"\$skiptoken=([^&#]*)");

                        // Extract the skip token
                        token = match.Success ? match.Groups[1].Value : null;
                    }
                }
            }
            catch (Exception e)
            {
                throw new IncorrectJsonException(e.Message, e);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = oldCulture;
            }
            return token;
        }

        /// <summary>
        /// Fetch Json Array (Json within ['d']['results']) from response
        /// </summary>
        public static string GetJsonArray(string response)
        {

            var oldCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            try
            {
                JArray results;
                var dict = JObject.Parse(response);
                var innerPart = dict["d"];
                if (innerPart is JObject d)
                {
                    if (!d.ContainsKey("results"))
                    {
                        throw new ArgumentException("Json missing 'results' property");
                    }
                    results = (JArray)d["results"];
                }
                else
                {
                    results = (JArray)innerPart;
                }
                return GetJsonFromResultDictionary(results).ToString(Formatting.None);
            }
            catch (Exception e)
            {
                throw new IncorrectJsonException(e.Message, e);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = oldCulture;
            }

        }

        #endregion

        #region Private methods

        /// <summary>
        /// Converts key/value pairs to json
        /// </summary>
        private static JObject GetJsonFromDictionary(JObject dictionary)
        {
            var result = new JObject();

            foreach (var entry in dictionary)
            {
                // item is a subcollection
                if (entry.Value is JObject subcollection)
                {
                    if (subcollection.TryGetValue("results", out var token))
                    {
                        result[entry.Key] = GetJsonFromResultDictionary((JArray)token);
                    }
                }
                // item is null
                else if (entry.Value == null)
                {
                    result[entry.Key] = null;
                }
                // item is just a value
                else
                {
                    result[entry.Key] = entry.Value;
                }
            }

            return result;
        }

        private static JArray GetJsonFromResultDictionary(JArray results)
        {
            var jArray = new JArray();
            if (results != null && results.Count > 0)
            {
                foreach (var jToken in results)
                {
                    if (jToken is JObject entity)
                    {
                        jArray.Add(GetJsonFromDictionary(entity));
                    }
                }
            }

            return jArray;
        }

        #endregion

    }
}