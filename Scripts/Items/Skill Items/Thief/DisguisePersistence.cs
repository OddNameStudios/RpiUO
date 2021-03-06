#region Header
// **********
// RpiUO - DisguisePersistence.cs
// Last Edit: 2015/12/23
// Look for Rpi comment
// **********
#endregion

#region References
//using System.Linq;
#endregion

namespace Server.Items
{
	public static class DisguisePersistence
	{
        //Rpi - Changed the literal string to use foward slash convention, so it works on Linux.
		public const string FilePath = "Saves/Disguises/Persistence.bin";

		public static void Configure()
		{
			EventSink.WorldSave += OnSave;
			EventSink.WorldLoad += OnLoad;
		}

		private static void OnSave(WorldSaveEventArgs e)
		{
			Persistence.Serialize(
				FilePath,
				writer =>
				{
					writer.Write(0); // version

					writer.Write(DisguiseTimers.Timers.Count);

                    //Rpi - Replaces the linq code below
                    Mobile mobile;
                    foreach (var key in DisguiseTimers.Timers.Keys)
                    {
                        if (key is Mobile)
                        {
                            mobile = key as Mobile;
                            writer.Write(mobile);
                            writer.Write(DisguiseTimers.TimeRemaining(mobile));
                            writer.Write(mobile.NameMod);
                        }

                    }

                    //Rpi - Remove linq code for better performance
					/*foreach (var mobile in DisguiseTimers.Timers.Keys.OfType<Mobile>())
					{
						writer.Write(mobile);
						writer.Write(DisguiseTimers.TimeRemaining(mobile));
						writer.Write(mobile.NameMod);
					}*/
				});
		}

		private static void OnLoad()
		{
			Persistence.Deserialize(
				FilePath,
				reader =>
				{
                    //Rpi - Added verification process before trying to read for the first time, and there is no data.
                    if (reader.PeekChar() != -1)
                    {
                        var version = reader.ReadInt();

                        switch (version)
                        {
                            case 0:
                                {
                                    var count = reader.ReadInt();

                                    for (var i = 0; i < count; ++i)
                                    {
                                        var m = reader.ReadMobile();
                                        DisguiseTimers.CreateTimer(m, reader.ReadTimeSpan());
                                        m.NameMod = reader.ReadString();
                                    }
                                }
                                break;
                        }
                    }
				});
		}
	}
}