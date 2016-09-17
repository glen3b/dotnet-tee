using System;
using System.IO;
using System.Collections.Generic;

namespace DotNetTee
{
	class MainClass
	{
		public static int Main (string[] args)
		{
			List<Stream> streams = new List<Stream> ();
			try {
				streams.Add (Console.OpenStandardOutput ());
				foreach (string file in args) {
					streams.Add (File.Open (file, FileMode.Create, FileAccess.Write));
				}

				using (Stream stdin = Console.OpenStandardInput ()) {
					byte[] buffer = new byte[2048];
					int bytes;
					while ((bytes = stdin.Read (buffer, 0, buffer.Length)) > 0) {
						foreach (var stream in streams) {
							stream.Write (buffer, 0, bytes);
							stream.Flush ();
						}
					}
				}
			} finally {
				foreach (var stream in streams) {
					stream.Close ();
				}
			}

			return 0;
		}
	}
}
