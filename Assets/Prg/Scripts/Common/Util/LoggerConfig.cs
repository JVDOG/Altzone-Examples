using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Prg.Scripts.Common.Util
{
    /// <summary>
    /// Debug logger config.
    /// </summary>
    public class LoggerConfig : ScriptableObject
    {
        /// <summary>
        /// Class <c>RegExpFilter</c> contains regexp pattern and flag to include or exclude class name filter patterns from logging.
        /// </summary>
        private class RegExpFilter
        {
            public bool IsLogged;
            public Regex Regex;
        }

        [Header("Settings")] public bool _isLogToFile;
        public string _colorForClassName;

        [Header("Class Filter"), TextArea(5, 20)] public string _loggerRules;

        private static string _prefixTag;
        private static string _suffixTag;

        public static void CreateLoggerConfig(LoggerConfig config)
        {
            string FilterClassNameLogMessage(string message)
            {
                return message.Replace(_prefixTag, "[").Replace(_suffixTag, "]");
            }

            if (config._isLogToFile)
            {
                CreateLogWriter();
            }
            // Log color
            var trimmed = string.IsNullOrEmpty(config._colorForClassName) ? string.Empty : config._colorForClassName.Trim();
            if (trimmed.Length > 0)
            {
                // This is a bit complicated because:
                // - Debug knows what to log and adds color to logged content that goes to UnityEngine console logger
                // - LogWriter receives it and needs to remove those parts that are only for console logging, like colors.
                _prefixTag = $"[<color={config._colorForClassName}>";
                _suffixTag = "</color>]";
                Debug.SetTagsForClassName(_prefixTag, _suffixTag);
                LogWriter.AddLogLineContentFilter(FilterClassNameLogMessage);
            }
            var filterList = config.BuildFilter();
            if (filterList.Count == 0)
            {
                return;
            }

            // Install log filter as last thing here.
            bool LogLineAllowedFilter(MethodBase method)
            {
                // For anonymous types we try its parent type.
                var isAnonymous = method.ReflectedType?.Name.StartsWith("<");
                var type = isAnonymous.HasValue && isAnonymous.Value
                    ? method.ReflectedType?.DeclaringType
                    : method.ReflectedType;
                var className = type?.FullName;
                if (className == null)
                {
                    return false;
                }
                var match = filterList.FirstOrDefault(x => x.Regex.IsMatch(className));
                return match?.IsLogged ?? false;
            }

            Debug.AddLogLineAllowedFilter(LogLineAllowedFilter);
#if FORCE_LOG || UNITY_EDITOR
#else
            UnityEngine.Debug.LogWarning($"<b>NOTE!</b> Application logging is totally disabled on platform: {Application.platform}");
#endif
        }

        [Conditional("UNITY_EDITOR"), Conditional("FORCE_LOG")]
        private static void CreateLogWriter()
        {
            string FilterPhotonLogMessage(string message)
            {
                // This is mainly to remove "formatting" form Photon ToString and ToStringFull messages and make then one liners!
                if (!string.IsNullOrEmpty(message))
                {
                    if (message.Contains("\n") || message.Contains("\r") || message.Contains("\t"))
                    {
                        message = message.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");
                    }
                }
                return message;
            }

            UnityExtensions.CreateGameObjectAndComponent<LogWriter>(nameof(LogWriter), true);
            LogWriter.AddLogLineContentFilter(FilterPhotonLogMessage);
        }

        private List<RegExpFilter> BuildFilter()
        {
            // Note that line parsing relies on TextArea JSON serialization which I have not tested very well!
            // - lines can start and end with "'" if content has something that needs to be "protected" during JSON parsing
            // - JSON multiline separator is LF "\n"
            var list = new List<RegExpFilter>();
            var lines = _loggerRules ?? string.Empty;
            if (lines.StartsWith("'") && lines.EndsWith("'"))
            {
                lines = lines.Substring(1, lines.Length - 2);
            }
            foreach (var token in lines.Split('\n'))
            {
                var line = token.Trim();
                if (line.StartsWith("#"))
                {
                    continue;
                }
                try
                {
                    var isLogged = true;
                    if (line.EndsWith("=1"))
                    {
                        line = line.Substring(0, line.Length - 2);
                    }
                    else if (line.EndsWith("=0"))
                    {
                        isLogged = false;
                        line = line.Substring(0, line.Length - 2);
                    }
                    else if (line.Contains("="))
                    {
                        UnityEngine.Debug.LogError($"invalid Regex pattern '{line}', do not use '=' here");
                        continue;
                    }
                    const RegexOptions regexOptions = RegexOptions.Singleline | RegexOptions.CultureInvariant;
                    var filter = new RegExpFilter
                    {
                        Regex = new Regex(line, regexOptions),
                        IsLogged = isLogged
                    };
                    list.Add(filter);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"invalid Regex pattern '{line}': {e.GetType().Name} {e.Message}");
                }
            }
            return list;
        }
    }
}