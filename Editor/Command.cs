using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Yorozu.PrefabDiffViewer
{
	internal static class Command
	{
		/// <summary>
		///     パス一覧
		///     環境変数から取得しようとしたが
		/// </summary>
		private static readonly string[] checkPaths =
		{
			"/opt/local/bin",
			"/opt/local/sbin",
			"/usr/local/bin",
			"/usr/local/sbin",
			"/usr/bin",
			"/usr/sbin",
			"/bin",
			"/sbin"
		};
		private static readonly string[] paths;

		static Command()
		{
			// 存在するパスだけを抽出
			paths = checkPaths.Where(Directory.Exists).ToArray();
		}

		/// <summary>
		///     Path 内にバイナリがあるかチェック
		/// </summary>
		/// <param name="command"></param>
		/// <returns></returns>
		private static string FindPath(string command)
		{
#if UNITY_EDITOR_OSX
			foreach (var path in paths)
			{
				var fullPath = Path.Combine(path, command);

				if (File.Exists(fullPath))
					return fullPath;
			}

			return string.Empty;
#else
			return command;
#endif
		}

		/// <summary>
		///     内部でコマンドをパース
		/// </summary>
		/// <param name="command"></param>
		/// <returns></returns>
		public static string Exec(string command)
		{
			command = command.Trim();
			var index = command.IndexOf(' ');

			if (index < 0)
				return Exec(command, "");

			var c = command.Substring(0, index);
			var args = command.Substring(index + 1);

			return Exec(c, args);
		}

		private static string Exec(string command, string args)
		{
			var path = FindPath(command);

			if (string.IsNullOrEmpty(path))
				return $"File Not Exits. {command}";

			var progress = new ProcessStartInfo(path, args)
			{
				WindowStyle = ProcessWindowStyle.Hidden,
				UseShellExecute = false,
				RedirectStandardOutput = true
			};

			using (var p = Process.Start(progress))
			{
				var output = p.StandardOutput.ReadToEnd();
				p.WaitForExit(5000);

				return output;
			}
		}
	}
}
