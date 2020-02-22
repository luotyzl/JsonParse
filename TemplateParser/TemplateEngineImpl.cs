using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace TemplateParser {
    public class TemplateEngineImpl : ITemplateEngine {

        /// <summary>
        /// Applies the specified datasource to a string template, and returns a result string
        /// with substituted values.
        /// </summary>
        public string Apply(string template, object dataSource)
        {
            var jsonObject = JObject.FromObject(dataSource);
            var pattern = @"\[(.+?)\]";
            var rgx = new Regex(pattern,RegexOptions.Multiline);
            var match = rgx.Matches(template);
            var idx = match.Count;
            var result = template;
            for (int i = 0; i < idx; i++)
            {
                var token = match[i].Value;
                if (token.Contains("[with "))
                {
                    var subResult = result.Substring(result.IndexOf(token, StringComparison.Ordinal) + token.Length);
                    var replacedResult = Apply(subResult, jsonObject[token.Replace("[with ", "").Replace("]","")]);
                    result = result.Replace(subResult,replacedResult);
                    result = result.Replace(token, "");
                    return result;
                }

                if (token.Contains("[/with]"))
                {
                    result = result.Replace("[/with]", "");
                    return result;
                }

                if (token.Contains(" \""))
                {
                    var subPattern = " \"(.+?)\"";
                    var format = new Regex(subPattern, RegexOptions.Multiline).Match(token).ToString().Trim();
                    var property = token.Replace(format, "");
                    var formatValue = jsonObject.SelectToken(property.Replace("[","").Replace("]",""));
                    if (formatValue?.Type == JTokenType.Date)
                    {
                        result = result.Replace(token,
                            formatValue.ToObject<DateTime>().ToString(format.Replace("\"",""), CultureInfo.InvariantCulture));
                        return result;
                    }
                    result = result.Replace(token, formatValue?.ToString() ?? "");
                    return result;
                }
                var propertyName = token;
                var value = jsonObject.SelectToken(propertyName.Replace("[","").Replace("]",""))?.ToString() ?? "";
                result = result.Replace(propertyName, value);
            }
            return result;
        }
    }
}
