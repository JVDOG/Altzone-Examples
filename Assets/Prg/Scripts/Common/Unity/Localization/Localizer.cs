using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace Prg.Scripts.Common.Unity.Localization
{
    internal class Language
    {
        private readonly Dictionary<string, string> _words;
        private readonly Dictionary<string, string> _altWords;

        public SystemLanguage LanguageName { get; }

        public string Locale { get; }

        internal Dictionary<string, string> Words => _words;

        public string Word(string key) =>
            _words.TryGetValue(key, out var value) ? value
            : _altWords.TryGetValue(key, out var altValue) ? altValue
            : $"[{key}]";

        public Language(SystemLanguage language, string localeName, Dictionary<string, string> words, Dictionary<string, string> altWords)
        {
            LanguageName = language;
            Locale = localeName;
            _words = words;
            _altWords = altWords;
        }
    }

    internal class Languages
    {
        private readonly List<Language> _languages = new List<Language>();

        internal ReadOnlyCollection<Language> GetLanguages => _languages.AsReadOnly();

        internal void Add(Language language)
        {
            Assert.IsTrue(_languages.FindIndex(x => x.Locale == language.Locale) == -1,
                "_languages.FindIndex(x => x.Locale == language.Locale) == -1");
            _languages.Add(language);
        }

        internal bool HasLanguage(SystemLanguage language)
        {
            return _languages.FindIndex(x => x.LanguageName == language) != -1;
        }

        internal Language GetLanguage(SystemLanguage language)
        {
            var index = _languages.FindIndex(x => x.LanguageName == language);
            if (index == -1)
            {
                if (_languages.Count > 0)
                {
                    return _languages[0];
                }
                return new Language(language, "xx", new Dictionary<string, string>(), new Dictionary<string, string>());
            }
            return _languages[index];
        }
    }

    /// <summary>
    /// Simple <c>I18N</c> implementation to localize words and phrases.
    /// </summary>
    public static class Localizer
    {
        private static Languages _languages;
        private static Language _curLanguage;

        public const SystemLanguage DefaultLanguage = SystemLanguage.Finnish;

        public static string Localize(string key) => _curLanguage.Word(key);

        public static bool HasLanguage(SystemLanguage language)
        {
            return _languages.HasLanguage(language);
        }

        public static void SetLanguage(SystemLanguage language)
        {
            Debug.Log($"SetLanguage {language}");
            _curLanguage = _languages.GetLanguage(language);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void LoadTranslations()
        {
            var config = Resources.Load<LocalizationConfig>(nameof(LocalizationConfig));
            Assert.IsNotNull(config, "config != null");
            _languages = BinAsset.Load(config.LanguagesBinFile);
            SetLanguage(DefaultLanguage);
        }

        [Conditional("UNITY_EDITOR")]
        public static void SaveTranslations()
        {
            var config = Resources.Load<LocalizationConfig>(nameof(LocalizationConfig));
            Assert.IsNotNull(config, "config != null");
            var languages = TsvLoader.LoadTranslations(config.TranslationsTsvFile);
            BinAsset.Save(languages, config.LanguagesBinFile);
        }
    }

    /// <summary>
    /// Tab Separated Values (.tsv) content loader for localized words and phrases.
    /// </summary>
    /// <remarks>
    /// File format is documented elsewhere!
    /// </remarks>
    internal static class TsvLoader
    {
        private const string DefaultLocale = "en";

        // https://en.wikipedia.org/wiki/List_of_ISO_639-1_codes
        private static readonly string[] SupportedLocales =
        {
            "key",
            "en",
            "fi",
            "sv"
        };

        private static readonly SystemLanguage[] SupportedLanguages =
        {
            SystemLanguage.Unknown,
            SystemLanguage.English,
            SystemLanguage.Finnish,
            SystemLanguage.Swedish
        };

        internal static SystemLanguage GetLanguageFor(string locale)
        {
            var index = Array.FindIndex(SupportedLocales, x => x == locale);
            Assert.IsTrue(index >= 0);
            return SupportedLanguages[index];
        }

        internal static Languages LoadTranslations(TextAsset textAsset)
        {
            var stopwatch = new Stopwatch();
            Debug.Log($"Translations tsv {textAsset.name} text len {textAsset.text.Length}");
            var lines = textAsset.text;
            var languages = new Languages();
            var maxIndex = SupportedLocales.Length;
            var dictionaries = new Dictionary<string, string>[maxIndex];
            var lineCount = 0;
            using (var reader = new StringReader(lines))
            {
                var line = reader.ReadLine();
                Assert.IsNotNull(line, "line != null");
                lineCount += 1;
                Debug.Log($"FIRST LINE: {line.Replace('\t', ' ')}");
                // key en fi sv es ru it de fr zh-CN
                var tokens = line.Split('\t');
                Assert.IsTrue(tokens.Length >= maxIndex, "tokens.Length >= maxIndex");
                for (var i = 1; i < maxIndex; ++i)
                {
                    // Translation file columns must match our understanding of locale columns!
                    var requiredLocale = SupportedLocales[i];
                    var currentLocale = tokens[i];
                    Assert.IsTrue(currentLocale == requiredLocale, "currentLocale == requiredLocale");
                    if (i == 1)
                    {
                        // Default locale must be in first locale column.
                        Assert.IsTrue(currentLocale == DefaultLocale, "currentLocale == _defaultLocale");
                    }
                    dictionaries[i] = new Dictionary<string, string>();
                }
                stopwatch.Start();
                for (;;)
                {
                    line = reader.ReadLine();
                    if (line == null)
                    {
                        break;
                    }
                    lineCount += 1;
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }
                    ParseLine(line, maxIndex, ref dictionaries);
                }
                stopwatch.Stop();
            }
            Debug.Log($"lineCount {lineCount} in {stopwatch.ElapsedMilliseconds} ms");
            Dictionary<string, string> altDictionary = null;
            for (var i = 1; i < maxIndex; ++i)
            {
                var lang = SupportedLanguages[i];
                var locale = SupportedLocales[i];
                var dictionary = dictionaries[i];
                if (i == 1)
                {
                    altDictionary = dictionary;
                }
                var language = new Language(lang, locale, dictionary, altDictionary);
                languages.Add(language);
                Debug.Log($"dictionary for {locale} {lang} has {dictionary.Count} words");
            }
            return languages;
        }

        private static void ParseLine(string line, int maxIndex, ref Dictionary<string, string>[] dictionaries)
        {
            var tokens = line.Split('\t');
            Assert.IsTrue(tokens.Length >= maxIndex, "tokens.Length >= maxIndex");
            var key = tokens[0];
            var defaultValue = tokens[1];
            Assert.IsFalse(string.IsNullOrEmpty(defaultValue), "string.IsNullOrEmpty(defaultValue)");
            for (var i = 1; i < maxIndex; ++i)
            {
                var colValue = tokens[i];
                if (string.IsNullOrEmpty(colValue))
                {
                    continue;
                }
                var dictionary = dictionaries[i];
                dictionary.Add(key, colValue);
            }
        }
    }

    /// <summary>
    /// Binary formatted <c>TextAsset</c> for localized words and phrases.
    /// </summary>
    /// <remarks>
    /// File format is proprietary and contained in this file!
    /// </remarks>
    internal static class BinAsset
    {
        private const string AssetRoot = "Assets";

        private const byte FileMark = 0xAA;
        private const byte LocaleStart = 0xBB;
        private const byte LocaleEnd = 0xCC;

        private const int FileVersion = 100;

        [Conditional("UNITY_EDITOR")]
        internal static void Save(Languages languages, TextAsset binAsset)
        {
#if UNITY_EDITOR
            string GetAssetPath(string name)
            {
                var assetFilter = $"{name} t:TextAsset";
                var foundAssets = AssetDatabase.FindAssets(assetFilter, new[] { AssetRoot });
                Assert.IsTrue(foundAssets.Length == 1, "foundAssets.Length == 1");
                return AssetDatabase.GUIDToAssetPath(foundAssets[0]);
            }

            var path = GetAssetPath(binAsset.name);
            Debug.Log($"Save Languages bin {binAsset.name} path {path}");
            int byteCount;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var languageList = languages.GetLanguages;
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write(FileMark);
                    writer.Write(FileVersion);
                    writer.Write(languageList.Count);
                    foreach (var language in languageList)
                    {
                        var locale = language.Locale;
                        var words = language.Words;
                        writer.Write(LocaleStart);
                        writer.Write(locale);
                        writer.Write(words.Count);
                        foreach (var entry in words)
                        {
                            writer.Write(entry.Key);
                            writer.Write(entry.Value);
                        }
                        writer.Write(LocaleEnd);
                    }
                }
                var bytes = stream.ToArray();
                byteCount = bytes.Length;
                File.WriteAllBytes(path, bytes);
                AssetDatabase.SaveAssets();
            }
            stopwatch.Stop();
            Debug.Log($"Save Languages bin {binAsset.name} bytes len {byteCount} in {stopwatch.ElapsedMilliseconds} ms");
            foreach (var language in languageList)
            {
                DumpLanguage(language);
            }
#endif
        }

        internal static Languages Load(TextAsset binAsset)
        {
            var bytes = binAsset.bytes;
            Debug.Log($"Load Languages bin {binAsset.name} bytes len {bytes.Length}");
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var languages = new Languages();
            using (var stream = new MemoryStream(bytes))
            {
                using (var reader = new BinaryReader(stream))
                {
                    var fileMark = reader.ReadByte();
                    Assert.IsTrue(fileMark == FileMark, "fileMark == FileMark");
                    var fileVersion = reader.ReadInt32();
                    Assert.IsTrue(fileVersion == FileVersion, "fileVersion == FileVersion");
                    var localeCount = reader.ReadInt32();
                    Assert.IsTrue(localeCount > 0, "localeCount > 0");
                    Dictionary<string, string> altDictionary = null;
                    for (var i = 0; i < localeCount; ++i)
                    {
                        var localeStart = reader.ReadByte();
                        Assert.IsTrue(localeStart == LocaleStart, "localeStart == LocaleStart");
                        var locale = reader.ReadString();
                        Assert.IsFalse(string.IsNullOrWhiteSpace(locale), "string.IsNullOrWhiteSpace(locale)");
                        var lang = TsvLoader.GetLanguageFor(locale);
                        var wordCount = reader.ReadInt32();
                        Assert.IsTrue(wordCount >= 0, "wordCount >= 0");
                        var words = new Dictionary<string, string>();
                        for (var counter = 0; counter < wordCount; ++counter)
                        {
                            var key = reader.ReadString();
                            var value = reader.ReadString();
                            words.Add(key, value);
                        }
                        var localeEnd = reader.ReadByte();
                        Assert.IsTrue(localeEnd == LocaleEnd, "localeEnd == LocaleEnd");
                        if (i == 0)
                        {
                            altDictionary = words;
                        }
                        var language = new Language(lang, locale, words, altDictionary);
                        languages.Add(language);
                    }
                }
            }
            stopwatch.Stop();
            Debug.Log($"Load Languages bin {binAsset.name} bytes len {bytes.Length} in {stopwatch.ElapsedMilliseconds} ms");
            foreach (var language in languages.GetLanguages)
            {
                DumpLanguage(language);
            }
            return languages;
        }

        [Conditional("UNITY_EDITOR")]
        private static void DumpLanguage(Language language)
        {
            Debug.Log($"Language {language.Locale} {language.LanguageName} words {language.Words.Count}");
        }
    }
}