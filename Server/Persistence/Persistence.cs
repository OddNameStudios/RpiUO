﻿#region Header
// **********
// RpiUO - Persistence.cs
// Last Edit: 2015/12/21
// Look for Rpi comment
// **********
#endregion

#region References
using System;
using System.IO;
#endregion

namespace Server
{
	public static class Persistence
	{
		public static void Serialize(string path, Action<GenericWriter> serializer)
		{
			Serialize(new FileInfo(path), serializer);
		}

		public static void Serialize(FileInfo file, Action<GenericWriter> serializer)
		{
			if (file.Directory != null && !file.Directory.Exists)
			{
				file.Directory.Create();
			}

			if (!file.Exists)
			{
				file.Create().Close();
			}

			using (var fs = file.OpenWrite())
			{
				var writer = new BinaryFileWriter(fs, true);

				try
				{
					serializer(writer);
				}
				finally
				{
					writer.Flush();
					writer.Close();
				}
			}
		}

		public static void Deserialize(string path, Action<GenericReader> deserializer)
		{
			Deserialize(path, deserializer, true);
		}

		public static void Deserialize(FileInfo file, Action<GenericReader> deserializer)
		{
			Deserialize(file, deserializer, true);
		} 

		public static void Deserialize(string path, Action<GenericReader> deserializer, bool ensure)
		{
			Deserialize(new FileInfo(path), deserializer, ensure);
		}

		public static void Deserialize(FileInfo file, Action<GenericReader> deserializer, bool ensure)
		{
			if (file.Directory != null && !file.Directory.Exists)
			{
				if (!ensure)
				{
					throw new DirectoryNotFoundException();
				}

				file.Directory.Create();
			}

			bool created = false;

			if (!file.Exists)
			{
				if (!ensure)
				{
					throw new FileNotFoundException();
				}

				file.Create().Close();
				created = true;
			}

			using (var fs = file.OpenRead())
			{
				var reader = new BinaryFileReader(new BinaryReader(fs));

                //Rpi - Added verification process before trying to read for the first time, and there is no data.
                if (reader.PeekChar() != -1)
                {
                    try
                    {
                        deserializer(reader);
                    }
                    catch (EndOfStreamException eos)
                    {
                        if (!created)
                        {
                            Console.WriteLine("[Persistence]: {0}", eos);
                        }
                    }
                    finally
                    {
                        reader.Close();
                    }
                }

			}
		}
	}
}