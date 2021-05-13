using System;
using System.Collections.Generic;
using System.IO;

namespace Yorozu.PrefabDiffViewer
{
	internal class Yaml
	{
		internal List<YamlComponent> Components = new List<YamlComponent>();
	}

	internal class YamlComponent
	{
		internal string ID;
		internal string Component;
		internal List<YamlField> Fields = new List<YamlField>();

		internal YamlComponent(string id)
		{
			ID = id;
		}
	}

	internal class YamlField
	{
		internal string Name;
		internal List<string> Values = new List<string>();

		internal YamlField(string name)
		{
			Name = name;
		}
	}

	internal static class PrefabYamlUtil
	{
		/// <summary>
		/// 適当パーサー
		/// </summary>
		internal static Yaml Parse(string path)
		{
			var text = File.ReadAllLines(path);
			var yaml = new Yaml();
			YamlComponent c = null;
			YamlField f = null;
			var isArray = false;
			for (var i = 2; i < text.Length; i++)
			{
				// New Component
				if (text[i].StartsWith("---"))
				{
					if (c != null)
						yaml.Components.Add(c);

					var index = text[i].IndexOf("&", StringComparison.Ordinal) + 1;
					c = new YamlComponent(text[i].Substring(index));
					continue;
				}

				// ComponentName
				if (!text[i].StartsWith(" "))
				{
					c.Component = text[i].Substring(0, text[i].Length - 1);
					continue;
				}

				var trim = text[i].Trim();
				// インデントが変わっていたら Array の内部判定
				if (isArray && (text[i].StartsWith("  - ") || text[i].StartsWith("    ")))
				{
					f.Values.Add(text[i].Substring(4));
					continue;
				}

				isArray = false;

				if (f != null)
					c.Fields.Add(f);

				// List or Array  stringで空の場合もある・・・
				if (trim.EndsWith(":"))
				{
					f = new YamlField(trim.Substring(0, trim.Length - 1));
					isArray = true;
					continue;
				}

				var spIndex = trim.IndexOf(":", StringComparison.Ordinal);
				f = new YamlField(trim.Substring(0, spIndex));
				f.Values.Add(trim.Substring(spIndex + 2));
			}

			if (f != null)
				c.Fields.Add(f);

			if (c != null)
				yaml.Components.Add(c);

			return yaml;
		}
	}
}
