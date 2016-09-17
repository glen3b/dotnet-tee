using System;
using System.IO;
using System.Collections.Generic;

namespace DotNetTee
{
	class MainClass
	{
		public static int Main (string[] args)
		{
			int retCode = 0;
			List<Stream> streams = new List<Stream> ();
			try {
				streams.Add (Console.OpenStandardOutput ());
				foreach (string file in args) {
					try{
					streams.Add (File.Open (file, FileMode.Create, FileAccess.Write));
					}catch(Exception ex){
						// TODO access invocation so our prefix can be logical
						// Also - maybe we should use a better error message, more specific to IO-type errors?
						retCode = 1;
						Console.Error.WriteLine("DotNetTee: {0}: {1}", file, ex.Message);
					}
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

			return retCode;
		}
	}
}
