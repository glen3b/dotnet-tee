using System;
using System.IO;
using System.Collections.Generic;

namespace DotNetTee
{
	class MainClass
	{
		public struct TeeOptions
		{
			public FileMode Mode;
			public bool AcknowledgeInterrupts;

			public static TeeOptions Default {
				get {
					TeeOptions def = new TeeOptions ();
					def.Mode = FileMode.Create;
					def.AcknowledgeInterrupts = true;
					return def;
				}
			}
		}

		public static TeeOptions ParseArgs (string[] args, out int fileIndexStart, ref int retCode)
		{
			// This should always be overwritten later - default to assuming we have no files and all options
			fileIndexStart = args.Length;

			TeeOptions opts = TeeOptions.Default;
			for (int i = 0; i < args.Length; i++) {
				if (!args [i].StartsWith ("-")) {
					// Not an option - done parsing
					fileIndexStart = i;
					break;
				}

				if (args [i].Equals ("--")) {
					// POSIX? signification for end of options
					// Next index is a file
					fileIndexStart = i + 1;
					break;
				}

				// We know it starts with a dash or two and it is in fact an option
				switch (args [i].ToLowerInvariant ()) {
				case "-a":
				case "--append":
					opts.Mode = FileMode.Append;
					break;
				case "-i":
				case "-ignore-interrupts":
					opts.AcknowledgeInterrupts = false;
					break;
				case "--help":
					// TODO better help page implementation and display
					Console.WriteLine ("DotNetTee [OPTION]... [FILE]...");
					Console.WriteLine ("-a, --append: Append to the given files instead of overwriting them.");
					Console.WriteLine ("-i, --ignore-interrupts: Ignore the ^C interrupt signal.");
					Console.WriteLine ("--help: Display this help page.");

					// This will cause termination in the enclosing trycatch
					throw new Exception();
				default:
					retCode = 1;
					Console.Error.WriteLine("Unrecognized option '{0}' - try passing '--help'", args[i]);

					// This will cause termination in the enclosing trycatch
					throw new Exception();
				}
			}

			return opts;
		}

		public static int Main (string[] args)
		{
			int retCode = 0;
			int fileIndexStart = 0;
			TeeOptions opts = TeeOptions.Default;
			try {
				opts = ParseArgs (args, out fileIndexStart, ref retCode);
			} catch {
				// Errored out
				return retCode;
			}

			if (!opts.AcknowledgeInterrupts) {
				// ^C does nothing to us
				// Wait for EOF, ^D by default in bash
				Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) => e.Cancel = true;
			}

			List<Stream> streams = new List<Stream> ();
			try {
				streams.Add (Console.OpenStandardOutput ());
				for (int i = fileIndexStart; i < args.Length; i++) {
					string file = args [i];
					try {
						streams.Add (File.Open (file, opts.Mode, FileAccess.Write));
					} catch (Exception ex) {
						// TODO access invocation so our prefix can be logical
						// Also - maybe we should use a better error message, more specific to IO-type errors?
						retCode = 1;
						Console.Error.WriteLine ("DotNetTee: {0}: {1}", file, ex.Message);
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
