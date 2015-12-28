#region Header
// **********
// RpiUO - World.cs
// Last Edit: 2015/12/26
// Look for Rpi comment
// **********
#endregion

#region References
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

using CustomsFramework;

using Server.Guilds;
using Server.Network;
#endregion

namespace Server
{
	public static class World
	{
        //Rpi - Refactored using the naming convention

        private static Dictionary<Serial, Mobile> mobilesDictionary_ps;
		private static Dictionary<Serial, Item> itemsDictionary_ps;
		private static Dictionary<CustomSerial, SaveData> dataDictionary_ps;

		private static bool isLoading_ps;
		private static bool isLoaded_ps;

		private static bool isSaving_ps;
		private static readonly ManualResetEvent diskWriteHandle_psr = new ManualResetEvent(true);

		private static Queue<IEntity> addQueue_ps, deleteQueue_ps;
		private static Queue<ICustomsEntity> customsAddQueue_ps, customsDeleteQueue_ps;

		public static bool IsSaving_s { get { return isSaving_ps; } }
		public static bool IsLoaded_s { get { return isLoaded_ps; } }
		public static bool IsLoading_s { get { return isLoading_ps; } }

		public static readonly string mobileIndexPath_sr = Path.Combine("Saves/Mobiles/", "Mobiles.idx");
		public static readonly string mobileTypesPath_sr = Path.Combine("Saves/Mobiles/", "Mobiles.tdb");
		public static readonly string mobileDataPath_sr = Path.Combine("Saves/Mobiles/", "Mobiles.bin");

		public static readonly string itemIndexPath_sr = Path.Combine("Saves/Items/", "Items.idx");
		public static readonly string itemTypesPath_sr = Path.Combine("Saves/Items/", "Items.tdb");
		public static readonly string itemDataPath_sr = Path.Combine("Saves/Items/", "Items.bin");

		public static readonly string guildIndexPath_sr = Path.Combine("Saves/Guilds/", "Guilds.idx");
		public static readonly string guildDataPath_sr = Path.Combine("Saves/Guilds/", "Guilds.bin");

		public static readonly string dataIndexPath_sr = Path.Combine("Saves/Customs/", "SaveData.idx");
		public static readonly string dataTypesPath_sr = Path.Combine("Saves/Customs/", "SaveData.tdb");
		public static readonly string dataBinaryPath_sr = Path.Combine("Saves/Customs/", "SaveData.bin");

		public static void NotifyDiskWriteComplete()
		{
			if (diskWriteHandle_psr.Set())
			{
				Console.WriteLine("Closing Save Files. ");
			}
		}

		public static void WaitForWriteCompletion()
		{
			diskWriteHandle_psr.WaitOne();
		}

		public static Dictionary<Serial, Mobile> MobilesDictionary_s { get { return mobilesDictionary_ps; } }

		public static Dictionary<Serial, Item> ItemsDictionary_s { get { return itemsDictionary_ps; } }

		public static Dictionary<CustomSerial, SaveData> DataDictionary_s { get { return dataDictionary_ps; } }

		public static bool OnDelete(IEntity an_entity)
		{
			if (isSaving_ps || isLoading_ps)
			{
				if (isSaving_ps)
				{
					AppendSafetyLog("delete", an_entity);
				}

				deleteQueue_ps.Enqueue(an_entity);

				return false;
			}

			return true;
		}

		public static bool OnDelete(ICustomsEntity an_entity)
		{
			if (isSaving_ps || isLoading_ps)
			{
				if (isSaving_ps)
				{
					AppendSafetyLog("delete", an_entity);
				}

				customsDeleteQueue_ps.Enqueue(an_entity);

				return false;
			}

			return true;
		}

		public static void Broadcast(int a_hue, bool is_ascii, string a_text)
		{   //Rpi - Refactored
			Packet _packet;

			if (is_ascii)
			{
				_packet = new AsciiMessage(Serial.MinusOne_sr, -1, MessageType.Regular, a_hue, 3, "System", a_text);
			}
			else
			{
				_packet = new UnicodeMessage(Serial.MinusOne_sr, -1, MessageType.Regular, a_hue, 3, "ENU", "System", a_text);
			}

			List<NetState> _instancesList = NetState.Instances;

			_packet.Acquire();

			for (int i = 0; i < _instancesList.Count; ++i)
			{
				if (_instancesList[i].Mobile != null)
				{
					_instancesList[i].Send(_packet);
				}
			}

			_packet.Release();

			NetState.FlushAll();
		}

		public static void Broadcast(int a_hue, bool is_ascii, string a_format, params object[] args)
		{
			Broadcast(a_hue, is_ascii, String.Format(a_format, args));
		}

		private interface IEntityEntry
		{
			Serial Serial { get; }
			int TypeID { get; }
			long Position { get; }
			int Length { get; }
		}

		private sealed class GuildEntry : IEntityEntry
		{
			private readonly BaseGuild guild_pr;
			private readonly long position_pr;
			private readonly int length_pr;

			public BaseGuild Guild { get { return guild_pr; } }

			public Serial Serial { get { return guild_pr == null ? 0 : guild_pr.Id; } }

			public int TypeID { get { return 0; } }

			public long Position { get { return position_pr; } }

			public int Length { get { return length_pr; } }

			public GuildEntry(BaseGuild a_baseGuild, long a_position, int a_length)
			{
				guild_pr = a_baseGuild;
				position_pr = a_position;
				length_pr = a_length;
			}
		}

		private sealed class ItemEntry : IEntityEntry
		{
			private readonly Item item_pr;
			private readonly int typeID_pr;
			private readonly string typeName_pr;
			private readonly long position_pr;
			private readonly int length_pr;

			public Item Item { get { return item_pr; } }

			public Serial Serial { get { return item_pr == null ? Serial.MinusOne_sr : item_pr.Serial; } }

			public int TypeID { get { return typeID_pr; } }

			public string TypeName { get { return typeName_pr; } }

			public long Position { get { return position_pr; } }

			public int Length { get { return length_pr; } }

			public ItemEntry(Item item, int typeID, string typeName, long pos, int length)
			{
				item_pr = item;
				typeID_pr = typeID;
				typeName_pr = typeName;
				position_pr = pos;
				length_pr = length;
			}
		}

		private sealed class MobileEntry : IEntityEntry
		{
			private readonly Mobile mobile_pr;
			private readonly int typeID_pr;
			private readonly string typeName_pr;
			private readonly long position_pr;
			private readonly int length_pr;

			public Mobile Mobile { get { return mobile_pr; } }

			public Serial Serial { get { return mobile_pr == null ? Serial.MinusOne_sr : mobile_pr.Serial; } }

			public int TypeID { get { return typeID_pr; } }

			public string TypeName { get { return typeName_pr; } }

			public long Position { get { return position_pr; } }

			public int Length { get { return length_pr; } }

			public MobileEntry(Mobile mobile, int typeID, string typeName, long pos, int length)
			{
				mobile_pr = mobile;
				typeID_pr = typeID;
				typeName_pr = typeName;
				position_pr = pos;
				length_pr = length;
			}
		}

		public sealed class DataEntry : ICustomsEntry
		{
			private readonly SaveData data_pr;
			private readonly int typeID_pr;
			private readonly string typeName_pr;
			private readonly long position_pr;
			private readonly int length_pr;

			public DataEntry(SaveData a_saveData, int a_typeID, string a_typeName, long a_position, int a_length)
			{
				data_pr = a_saveData;
				typeID_pr = a_typeID;
				typeName_pr = a_typeName;
				position_pr = a_position;
				length_pr = a_length;
			}

			public SaveData Data { get { return data_pr; } }
			public CustomSerial Serial { get { return data_pr == null ? CustomSerial.MinusOne : data_pr.Serial; } }
			public int TypeID { get { return typeID_pr; } }
			public string TypeName { get { return typeName_pr; } }
			public long Position { get { return position_pr; } }
			public int Length { get { return length_pr; } }
		}

		private static string loadingType_ps;

		public static string LoadingType_s { get { return loadingType_ps; } }

		private static readonly Type[] serialTypeArray_psr = new Type[1] {typeof(Serial)};
		private static readonly Type[] customSerialTypeArray_psr = new Type[1] {typeof(CustomSerial)};

		private static List<Tuple<ConstructorInfo, string>> ReadTypes(BinaryReader a_binaryReader)
		{
            //Rpi - Refactored using the naming convention
			int _count = a_binaryReader.ReadInt32();

			List<Tuple<ConstructorInfo,string>> _typesTupleList = new List<Tuple<ConstructorInfo, string>>(_count);

			for (int i = 0; i < _count; ++i)
			{
				string _typeName = a_binaryReader.ReadString();

				Type _type = ScriptCompiler.FindTypeByFullName(_typeName);

				if (_type == null)
				{
					Console.WriteLine("failed");

					if (!Core.Service)
					{
						Console.WriteLine("Error: Type '{0}' was not found. Delete all of those types? (y/n)", _typeName);

						if (Console.ReadKey(true).Key == ConsoleKey.Y)
						{
							_typesTupleList.Add(null);
							Console.Write("World: Loading...");
							continue;
						}

						Console.WriteLine("Types will not be deleted. An exception will be thrown.");
					}
					else
					{
						Console.WriteLine("Error: Type '{0}' was not found.", _typeName);
					}

					throw new Exception(String.Format("Bad type '{0}'", _typeName));
				}

				ConstructorInfo _constructor = _type.GetConstructor(serialTypeArray_psr);
				ConstructorInfo _customConstructor = _type.GetConstructor(customSerialTypeArray_psr);

				if (_constructor != null)
				{
					_typesTupleList.Add(new Tuple<ConstructorInfo, string>(_constructor, _typeName));
				}
				else if (_customConstructor != null)
				{
					_typesTupleList.Add(new Tuple<ConstructorInfo, string>(_customConstructor, _typeName));
				}
				else
				{
					throw new Exception(String.Format("Type '{0}' does not have a serialization constructor", _type));
				}
			}

			return _typesTupleList;
		}

        private static object lockObject_ps = new object();

        public static void Load()
        {
            //Rpi - Refactored using the naming convention
            lock (lockObject_ps)
            {


                if (isLoaded_ps == true)
                {
                    return;
                }

                isLoaded_ps = true;
                loadingType_ps = null;

                Utility.PushColor(ConsoleColor.Yellow);
                Console.Write("World: Loading...");
                Utility.PopColor();

                Stopwatch _stopWatch = Stopwatch.StartNew();

                isLoading_ps = true;

                addQueue_ps = new Queue<IEntity>();
                deleteQueue_ps = new Queue<IEntity>();
                customsAddQueue_ps = new Queue<ICustomsEntity>();
                customsDeleteQueue_ps = new Queue<ICustomsEntity>();

                int _mobileCount = 0, _itemCount = 0, _guildCount = 0, _dataCount = 0;

                object[] _constructorArgs = new object[1];

                List<ItemEntry> _itemEntryList = new List<ItemEntry>();
                List<MobileEntry> _mobileEntryList = new List<MobileEntry>();
                List<GuildEntry> _guildEntryList = new List<GuildEntry>();
                List<DataEntry> _dataEntryList = new List<DataEntry>();

                if (File.Exists(mobileIndexPath_sr) && File.Exists(mobileTypesPath_sr))
                {
                    using (FileStream _indexFileStream = new FileStream(mobileIndexPath_sr, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        BinaryReader _indexBinaryReader = new BinaryReader(_indexFileStream);

                        using (FileStream _typeDatabaseFileStream = new FileStream(mobileTypesPath_sr, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            BinaryReader _typeDatabaseBinaryReader = new BinaryReader(_typeDatabaseFileStream);

                            List<Tuple<ConstructorInfo, string>> _typesTupleList = ReadTypes(_typeDatabaseBinaryReader);

                            //Rpi
                            //Lets look if there is anything before try to read.
                            //In Rpi, therer is no Arm7 safe code to ReadInt32()
                            //so we should check it first
                            if (_indexBinaryReader.BaseStream.Length > 4)
                            {
                                _mobileCount = _indexBinaryReader.ReadInt32();

                                mobilesDictionary_ps = new Dictionary<Serial, Mobile>(_mobileCount);


                                //Rpi - Optimized for less memory write       
                                int _typeID;
                                int _serial;
                                long _position;
                                int _length;
                                Tuple<ConstructorInfo, string> _typeTuple;

                                Mobile _mobile;
                                ConstructorInfo _constructorInfo;
                                string _typeName;

                                for (int i = 0; i < _mobileCount; ++i)
                                {
                                    _typeID = _indexBinaryReader.ReadInt32();
                                    _serial = _indexBinaryReader.ReadInt32();
                                    _position = _indexBinaryReader.ReadInt64();
                                    _length = _indexBinaryReader.ReadInt32();

                                    _typeTuple = _typesTupleList[_typeID];

                                    if (_typeTuple == null)
                                    {
                                        continue;
                                    }

                                    _mobile = null;
                                    _constructorInfo = _typeTuple.Item1;
                                    _typeName = _typeTuple.Item2;

                                    try
                                    {
                                        _constructorArgs[0] = (Serial)_serial;
                                        _mobile = _constructorInfo.Invoke(_constructorArgs) as Mobile;
                                    }
                                    catch
                                    { }

                                    if (_mobile != null)
                                    {
                                        _mobileEntryList.Add(new MobileEntry(_mobile, _typeID, _typeName, _position, _length));
                                        World.AddMobile(_mobile);
                                    }
                                }
                            }
                            else
                            {
                                //If there is no mobiles to read, creates an empty dictionary
                                mobilesDictionary_ps = new Dictionary<Serial, Mobile>();
                            }

                            _typeDatabaseBinaryReader.Close();
                        }

                        _indexBinaryReader.Close();
                    }
                }
                else
                {
                    mobilesDictionary_ps = new Dictionary<Serial, Mobile>();
                }

                if (File.Exists(itemIndexPath_sr) && File.Exists(itemTypesPath_sr))
                {
                    using (FileStream _indexFileStream = new FileStream(itemIndexPath_sr, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        BinaryReader _indexBinaryReader = new BinaryReader(_indexFileStream);

                        using (FileStream _typeDatabaseFileStream = new FileStream(itemTypesPath_sr, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            BinaryReader _typeDatabaseBinaryReader = new BinaryReader(_typeDatabaseFileStream);

                            List<Tuple<ConstructorInfo, string>> _typesTupleList = ReadTypes(_typeDatabaseBinaryReader);

                            //Rpi
                            //Lets look if there is anything before try to read.
                            //In Rpi, therer is no Arm7 safe code to ReadInt32()
                            //so we should check it first
                            if (_indexBinaryReader.BaseStream.Length > 4)
                            {
                                _itemCount = _indexBinaryReader.ReadInt32();

                                itemsDictionary_ps = new Dictionary<Serial, Item>(_itemCount);

                                //Rpi - Optimized for less memory write  

                                int _typeID;
                                int _serial;
                                long _position;
                                int _length;

                                Tuple<ConstructorInfo, string> _typeTuple;
                                Item _item;
                                ConstructorInfo _constructorInfo;
                                string _typeName;

                                for (int i = 0; i < _itemCount; ++i)
                                {
                                    _typeID = _indexBinaryReader.ReadInt32();
                                    _serial = _indexBinaryReader.ReadInt32();
                                    _position = _indexBinaryReader.ReadInt64();
                                    _length = _indexBinaryReader.ReadInt32();

                                    _typeTuple = _typesTupleList[_typeID];

                                    if (_typeTuple == null)
                                    {
                                        continue;
                                    }

                                    _item = null;
                                    _constructorInfo = _typeTuple.Item1;
                                    _typeName = _typeTuple.Item2;

                                    try
                                    {
                                        _constructorArgs[0] = (Serial)_serial;
                                        _item = _constructorInfo.Invoke(_constructorArgs) as Item;
                                    }
                                    catch
                                    { }

                                    if (_item != null)
                                    {
                                        _itemEntryList.Add(new ItemEntry(_item, _typeID, _typeName, _position, _length));
                                        World.AddItem(_item);
                                    }
                                }
                            }
                            else
                            {
                                //If there is no itens to ready, creat an empty dictionary
                                itemsDictionary_ps = new Dictionary<Serial, Item>();
                            }

                            _typeDatabaseBinaryReader.Close();
                        }

                        _indexBinaryReader.Close();
                    }
                }
                else
                {
                    itemsDictionary_ps = new Dictionary<Serial, Item>();
                }

                if (File.Exists(guildIndexPath_sr))
                {
                    using (FileStream _indexFileStream = new FileStream(guildIndexPath_sr, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        BinaryReader _indexBinaryReader = new BinaryReader(_indexFileStream);

                        //Rpi
                        //Lets look if there is anything before try to read.
                        //In Rpi, therer is no Arm7 safe code to ReadInt32()
                        //so we should check it first
                        if (_indexBinaryReader.BaseStream.Length > 4)
                        {
                            _guildCount = _indexBinaryReader.ReadInt32();

                            CreateGuildEventArgs _createGuildEventArgs = new CreateGuildEventArgs(-1);

                            //Rpi - Optimized for less memory write 
                            int _id;
                            long _position;
                            int _length;
                            BaseGuild _baseGuild;

                            for (int i = 0; i < _guildCount; ++i)
                            {
                                /*_typeID = */
                                _indexBinaryReader.ReadInt32(); //no typeid for guilds
                                _id = _indexBinaryReader.ReadInt32();
                                _position = _indexBinaryReader.ReadInt64();
                                _length = _indexBinaryReader.ReadInt32();

                                _createGuildEventArgs.Id = _id;
                                EventSink.InvokeCreateGuild(_createGuildEventArgs);
                                _baseGuild = _createGuildEventArgs.Guild;
                                if (_baseGuild != null)
                                {
                                    _guildEntryList.Add(new GuildEntry(_baseGuild, _position, _length));
                                }
                            }
                        }

                        _indexBinaryReader.Close();
                    }
                }

                if (File.Exists(dataIndexPath_sr) && File.Exists(dataTypesPath_sr))
                {
                    using (FileStream _indexFileStream = new FileStream(dataIndexPath_sr, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        BinaryReader _indexBinaryReader = new BinaryReader(_indexFileStream);

                        using (FileStream _typeDatabaseFileStream = new FileStream(dataTypesPath_sr, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            BinaryReader _typeDatabaseBinaryReader = new BinaryReader(_typeDatabaseFileStream);

                            List<Tuple<ConstructorInfo, string>> types = ReadTypes(_typeDatabaseBinaryReader);

                            //Rpi
                            //Lets look if there is anything before try to read.
                            //In Rpi, therer is no Arm7 safe code to ReadInt32()
                            //so we should check it first
                            if (_indexBinaryReader.BaseStream.Length > 4)
                            {
                                _dataCount = _indexBinaryReader.ReadInt32();
                                dataDictionary_ps = new Dictionary<CustomSerial, SaveData>(_dataCount);


                                int _typeID;
                                int _serial;
                                long _position;
                                int _length;

                                SaveData _saveData;
                                ConstructorInfo _constructorInfo;
                                string _typeName;

                                for (int i = 0; i < _dataCount; ++i)
                                {
                                    _typeID = _indexBinaryReader.ReadInt32();
                                    _serial = _indexBinaryReader.ReadInt32();
                                    _position = _indexBinaryReader.ReadInt64();
                                    _length = _indexBinaryReader.ReadInt32();

                                    var objects = types[_typeID];

                                    if (objects == null)
                                    {
                                        continue;
                                    }

                                    _saveData = null;
                                    _constructorInfo = objects.Item1;
                                    _typeName = objects.Item2;

                                    try
                                    {
                                        _constructorArgs[0] = (CustomSerial)_serial;
                                        _saveData = _constructorInfo.Invoke(_constructorArgs) as SaveData;
                                    }
                                    catch
                                    {
                                        Utility.PushColor(ConsoleColor.Red);
                                        Console.WriteLine("Error loading {0}, Serial: {1}", _typeName, _serial);
                                        Utility.PopColor();
                                    }

                                    if (_saveData != null)
                                    {
                                        _dataEntryList.Add(new DataEntry(_saveData, _typeID, _typeName, _position, _length));
                                        World.AddData(_saveData);
                                    }
                                }
                            }
                            else
                            {
                                dataDictionary_ps = new Dictionary<CustomSerial, SaveData>();
                            }

                            _typeDatabaseBinaryReader.Close();
                        }

                        _indexBinaryReader.Close();
                    }
                }
                else
                {
                    dataDictionary_ps = new Dictionary<CustomSerial, SaveData>();
                }

                bool _failedMobiles = false, _failedItems = false, _failedGuilds = false, _failedData = false;
                Type _failedType = null;
                Serial _failedSerial = Serial.Zero_sr;
                CustomSerial _failedCustomSerial = CustomSerial.Zero;
                Exception _failedException = null;
                int _failedTypeID = 0;

                if (File.Exists(mobileDataPath_sr))
                {
                    using (FileStream _mobileDataFileStream = new FileStream(mobileDataPath_sr, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        BinaryFileReader _mobileDataBinaryFileReader = new BinaryFileReader(new BinaryReader(_mobileDataFileStream));

                        MobileEntry _mobileEntry;
                        Mobile _mobile;

                        for (int i = 0; i < _mobileEntryList.Count; ++i)
                        {
                            _mobileEntry = _mobileEntryList[i];
                            _mobile = _mobileEntry.Mobile;

                            if (_mobile != null)
                            {
                                _mobileDataBinaryFileReader.Seek(_mobileEntry.Position, SeekOrigin.Begin);

                                try
                                {
                                    loadingType_ps = _mobileEntry.TypeName;
                                    _mobile.Deserialize(_mobileDataBinaryFileReader);

                                    if (_mobileDataBinaryFileReader.Position != (_mobileEntry.Position + _mobileEntry.Length))
                                    {
                                        throw new Exception(String.Format("***** Bad serialize on {0} *****", _mobile.GetType()));
                                    }
                                }
                                catch (Exception _exception)
                                {
                                    _mobileEntryList.RemoveAt(i);

                                    _failedException = _exception;
                                    _failedMobiles = true;
                                    _failedType = _mobile.GetType();
                                    _failedTypeID = _mobileEntry.TypeID;
                                    _failedSerial = _mobile.Serial;

                                    break;
                                }
                            }
                        }

                        _mobileDataBinaryFileReader.Close();
                    }
                }

                //Rpi - added for safeness
                loadingType_ps = null;

                if (!_failedMobiles && File.Exists(itemDataPath_sr))
                {
                    using (FileStream _itemDataFileStream = new FileStream(itemDataPath_sr, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        BinaryFileReader _itemDataBinaryFileReader = new BinaryFileReader(new BinaryReader(_itemDataFileStream));

                        ItemEntry _itemEntry;
                        Item _Item;

                        for (int i = 0; i < _itemEntryList.Count; ++i)
                        {
                            _itemEntry = _itemEntryList[i];
                            _Item = _itemEntry.Item;

                            if (_Item != null)
                            {
                                _itemDataBinaryFileReader.Seek(_itemEntry.Position, SeekOrigin.Begin);

                                try
                                {
                                    loadingType_ps = _itemEntry.TypeName;
                                    _Item.Deserialize(_itemDataBinaryFileReader);

                                    if (_itemDataBinaryFileReader.Position != (_itemEntry.Position + _itemEntry.Length))
                                    {
                                        throw new Exception(String.Format("***** Bad serialize on {0} *****", _Item.GetType()));
                                    }
                                }
                                catch (Exception e)
                                {
                                    _itemEntryList.RemoveAt(i);

                                    _failedException = e;
                                    _failedItems = true;
                                    _failedType = _Item.GetType();
                                    _failedTypeID = _itemEntry.TypeID;
                                    _failedSerial = _Item.Serial;

                                    break;
                                }
                            }
                        }

                        _itemDataBinaryFileReader.Close();
                    }
                }

                loadingType_ps = null;

                if (!_failedMobiles && !_failedItems && File.Exists(guildDataPath_sr))
                {
                    using (FileStream _guildDataFileStream = new FileStream(guildDataPath_sr, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        BinaryFileReader _guildDataBinaryFileReader = new BinaryFileReader(new BinaryReader(_guildDataFileStream));

                        GuildEntry _guildEntry;
                        BaseGuild _baseGuild;

                        for (int i = 0; i < _guildEntryList.Count; ++i)
                        {
                            _guildEntry = _guildEntryList[i];
                            _baseGuild = _guildEntry.Guild;

                            if (_baseGuild != null)
                            {
                                _guildDataBinaryFileReader.Seek(_guildEntry.Position, SeekOrigin.Begin);

                                try
                                {
                                    _baseGuild.Deserialize(_guildDataBinaryFileReader);

                                    if (_guildDataBinaryFileReader.Position != (_guildEntry.Position + _guildEntry.Length))
                                    {
                                        throw new Exception(String.Format("***** Bad serialize on Guild {0} *****", _baseGuild.Id));
                                    }
                                }
                                catch (Exception _exception)
                                {
                                    _guildEntryList.RemoveAt(i);

                                    _failedException = _exception;
                                    _failedGuilds = true;
                                    _failedType = typeof(BaseGuild);
                                    _failedTypeID = _baseGuild.Id;
                                    _failedSerial = _baseGuild.Id;

                                    break;
                                }
                            }
                        }

                        _guildDataBinaryFileReader.Close();
                    }
                }

                loadingType_ps = null;

                if (!_failedMobiles && !_failedItems && !_failedGuilds && File.Exists(dataBinaryPath_sr))
                {
                    using (FileStream _dataFileStream = new FileStream(dataBinaryPath_sr, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        BinaryFileReader _dataBinaryFileReader = new BinaryFileReader(new BinaryReader(_dataFileStream));

                        DataEntry _dataEntry;
                        SaveData _saveData;

                        for (int i = 0; i < _dataEntryList.Count; ++i)
                        {
                            _dataEntry = _dataEntryList[i];
                            _saveData = _dataEntry.Data;

                            if (_saveData != null)
                            {
                                _dataBinaryFileReader.Seek(_dataEntry.Position, SeekOrigin.Begin);

                                try
                                {
                                    loadingType_ps = _dataEntry.TypeName;
                                    _saveData.Deserialize(_dataBinaryFileReader);

                                    if (_dataBinaryFileReader.Position != (_dataEntry.Position + _dataEntry.Length))
                                    {
                                        throw new Exception(String.Format("***** Bad serialize on {0} *****", _saveData.GetType()));
                                    }
                                }
                                catch (Exception _exception)
                                {
                                    _dataEntryList.RemoveAt(i);

                                    _failedException = _exception;
                                    _failedData = true;
                                    _failedType = _saveData.GetType();
                                    _failedTypeID = _dataEntry.TypeID;
                                    _failedCustomSerial = _saveData.Serial;

                                    break;
                                }
                            }
                        }

                        _dataBinaryFileReader.Close();
                    }
                }

                if (_failedItems || _failedMobiles || _failedGuilds || _failedData)
                {
                    Utility.PushColor(ConsoleColor.Red);
                    Console.WriteLine("An error was encountered while loading a saved object");
                    Utility.PopColor();

                    Console.WriteLine(" - Type: {0}", _failedType);

                    if (_failedSerial != Serial.Zero_sr)
                    {
                        Console.WriteLine(" - Serial: {0}", _failedSerial);
                    }
                    else
                    {
                        Console.WriteLine(" - Serial: {0}", _failedCustomSerial);
                    }

                    if (!Core.Service)
                    {
                        Console.WriteLine("Delete the object? (y/n)");

                        if (Console.ReadKey(true).Key == ConsoleKey.Y)
                        {
                            if (_failedType != typeof(BaseGuild))
                            {
                                Console.WriteLine("Delete all objects of that type? (y/n)");

                                if (Console.ReadKey(true).Key == ConsoleKey.Y)
                                {
                                    if (_failedMobiles)
                                    {
                                        for (int i = 0; i < _mobileEntryList.Count;)
                                        {
                                            if (_mobileEntryList[i].TypeID == _failedTypeID)
                                            {
                                                _mobileEntryList.RemoveAt(i);
                                            }
                                            else
                                            {
                                                ++i;
                                            }
                                        }
                                    }
                                    else if (_failedItems)
                                    {
                                        for (int i = 0; i < _itemEntryList.Count;)
                                        {
                                            if (_itemEntryList[i].TypeID == _failedTypeID)
                                            {
                                                _itemEntryList.RemoveAt(i);
                                            }
                                            else
                                            {
                                                ++i;
                                            }
                                        }
                                    }
                                    else if (_failedData)
                                    {
                                        for (int i = 0; i < _dataEntryList.Count;)
                                        {
                                            if (_dataEntryList[i].TypeID == _failedTypeID)
                                            {
                                                _dataEntryList.RemoveAt(i);
                                            }
                                            else
                                            {
                                                ++i;
                                            }
                                        }
                                    }
                                }
                            }

                            World.SaveIndex(_mobileEntryList, mobileIndexPath_sr);
                            World.SaveIndex(_itemEntryList, itemIndexPath_sr);
                            World.SaveIndex(_guildEntryList, guildIndexPath_sr);
                            World.SaveIndex(dataIndexPath_sr, _dataEntryList);
                        }

                        Console.WriteLine("After pressing return an exception will be thrown and the server will terminate.");
                        Console.ReadLine();
                    }
                    else
                    {
                        Utility.PushColor(ConsoleColor.Red);
                        Console.WriteLine("An exception will be thrown and the server will terminate.");
                        Utility.PopColor();
                    }

                    throw new Exception(
                        String.Format(
                            "Load failed (items={0}, mobiles={1}, guilds={2}, data={3}, type={4}, serial={5})",
                            _failedItems,
                            _failedMobiles,
                            _failedGuilds,
                            _failedData,
                            _failedType,
                            (_failedSerial != Serial.Zero_sr ? _failedSerial.ToString() : _failedCustomSerial.ToString())),
                        _failedException);
                }

                EventSink.InvokeWorldLoad();

                isLoading_ps = false;

                ProcessSafetyQueues();

                foreach (Item _item in itemsDictionary_ps.Values)
                {
                    if (_item.Parent == null)
                    {
                        _item.UpdateTotals();
                    }

                    _item.ClearProperties();
                }

                foreach (Mobile _mobile in mobilesDictionary_ps.Values)
                {
                    _mobile.UpdateRegion(); // Is this really needed?
                    _mobile.UpdateTotals();

                    _mobile.ClearProperties();
                }

                foreach (SaveData _saveData in dataDictionary_ps.Values)
                {
                    _saveData.Prep();
                }

                _stopWatch.Stop();

                Utility.PushColor(ConsoleColor.Green);
                Console.WriteLine(
                    "done ({1} items, {2} mobiles, {3} customs) ({0:F2} seconds)",
                    _stopWatch.Elapsed.TotalSeconds,
                    itemsDictionary_ps.Count,
                    mobilesDictionary_ps.Count,
                    dataDictionary_ps.Count);
                Utility.PopColor();
            }
        }

		private static void ProcessSafetyQueues()
		{
			while (addQueue_ps.Count > 0)
			{
				IEntity _entity = addQueue_ps.Dequeue();

                //Rpi - Lets verify what it is, instead of try by brute force

                if(_entity is Item)
                {
                    AddItem(_entity as Item);
                }
                else if (_entity is Mobile)
                {
                    AddMobile(_entity as Mobile);
                }

                /*
				Item _item = _entity as Item;

				if (_item != null)
				{
					AddItem(_item);
				}
				else
				{
					Mobile _mobile = _entity as Mobile;

					if (_mobile != null)
					{
						AddMobile(_mobile);
					}
				}*/
			}

			while (deleteQueue_ps.Count > 0)
			{
				IEntity _entity = deleteQueue_ps.Dequeue();

                

                //Rpi - Lets verify what it is, instead of try by brute force
                if(_entity is Item)
                {
                    (_entity as Item).Delete();
                }
                else if (_entity is Mobile)
                {
                    (_entity as Mobile).Delete();
                }
                
                
                /*
                Item item = _entity as Item;

				if (item != null)
				{
					item.Delete();
				}
				else
				{
					Mobile mob = _entity as Mobile;

					if (mob != null)
					{
						mob.Delete();
					}
				}*/
			}

			while (customsAddQueue_ps.Count > 0)
			{
				ICustomsEntity _customEntity = customsAddQueue_ps.Dequeue();

                //Rpi - Lets verify what it is, instead of try by brute force
                if (_customEntity is SaveData)
                {
                    AddData(_customEntity as SaveData);
                }

                /*
				SaveData _saveData = _entity as SaveData;

				if (_saveData != null)
				{
					AddData(_saveData);
				}*/
			}

			while (customsDeleteQueue_ps.Count > 0)
			{
				ICustomsEntity _customEntity = customsDeleteQueue_ps.Dequeue();

                //Rpi - Lets verify what it is, instead of try by brute force
                if(_customEntity is SaveData)
                {
                    (_customEntity as SaveData).Delete();
                }

                /*
                SaveData data = _customEntity as SaveData;

				if (data != null)
				{
					data.Delete();
				}*/
			}
		}

		private static void AppendSafetyLog(string an_action, ICustomsEntity a_customEntity)
		{
			string message =
				String.Format(
					"Warning: Attempted to {1} {2} during world save." + "{0}This action could cause inconsistent state." +
					"{0}It is strongly advised that the offending scripts be corrected.",
					Environment.NewLine,
					an_action,
					a_customEntity);

			AppendSafetyLog(message);
		}

		private static void AppendSafetyLog(string an_action, IEntity an_entity)
		{
			string message =
				String.Format(
					"Warning: Attempted to {1} {2} during world save." + "{0}This action could cause inconsistent state." +
					"{0}It is strongly advised that the offending scripts be corrected.",
					Environment.NewLine,
					an_action,
					an_entity);

			AppendSafetyLog(message);
		}

		private static void AppendSafetyLog(string a_message)
		{
			Console.WriteLine(a_message);

			try
			{
				using (StreamWriter op = new StreamWriter("world-save-errors.log", true))
				{
					op.WriteLine("{0}\t{1}", DateTime.UtcNow, a_message);
					op.WriteLine(new StackTrace(2).ToString());
					op.WriteLine();
				}
			}
			catch
			{ }
		}

		private static void SaveIndex<T>(List<T> a_entityEntryList, string a_path) where T : IEntityEntry
		{
			if (!Directory.Exists("Saves/Mobiles/"))
			{
				Directory.CreateDirectory("Saves/Mobiles/");
			}

			if (!Directory.Exists("Saves/Items/"))
			{
				Directory.CreateDirectory("Saves/Items/");
			}

			if (!Directory.Exists("Saves/Guilds/"))
			{
				Directory.CreateDirectory("Saves/Guilds/");
			}

			using (FileStream _indexFileStream = new FileStream(a_path, FileMode.Create, FileAccess.Write, FileShare.None))
			{
				BinaryWriter _indexBinaryWriter = new BinaryWriter(_indexFileStream);

				_indexBinaryWriter.Write(a_entityEntryList.Count);

                T _entityEntry;

				for (int i = 0; i < a_entityEntryList.Count; ++i)
				{
					_entityEntry = a_entityEntryList[i];

					_indexBinaryWriter.Write(_entityEntry.TypeID);
					_indexBinaryWriter.Write(_entityEntry.Serial);
					_indexBinaryWriter.Write(_entityEntry.Position);
					_indexBinaryWriter.Write(_entityEntry.Length);
				}

				_indexBinaryWriter.Close();
			}
		}

		private static void SaveIndex<T>(string a_path, List<T> a_customEntryList) where T : ICustomsEntry
		{
			if (!Directory.Exists("Saves/Customs/"))
			{
				Directory.CreateDirectory("Saves/Customs/");
			}

			using (FileStream _indexFileStream = new FileStream(a_path, FileMode.Create, FileAccess.Write, FileShare.None))
			{
				BinaryWriter _indexBinaryWriter = new BinaryWriter(_indexFileStream);

				_indexBinaryWriter.Write(a_customEntryList.Count);

                T _customEntry;

				for (int i = 0; i < a_customEntryList.Count; ++i)
				{
					_customEntry = a_customEntryList[i];

					_indexBinaryWriter.Write(_customEntry.TypeID);
					_indexBinaryWriter.Write(_customEntry.Serial);
					_indexBinaryWriter.Write(_customEntry.Position);
					_indexBinaryWriter.Write(_customEntry.Length);
				}

				_indexBinaryWriter.Close();
			}
		}

		internal static int saves_is;

		public static void Save()
		{
			Save(true, false);
		}

        //Rpi - There is a memory leak during the save, adding about 1MB of data to ram memory
        //TODO - find what function does have the leak
		public static void Save(bool a_message, bool is_permitedBackgroundWrite)
		{
			if (isSaving_ps)
			{
				return;
			}

			++saves_is;

			NetState.FlushAll();
			NetState.Pause();

			WaitForWriteCompletion(); //Blocks Save until current disk flush is done.

			isSaving_ps = true;

			diskWriteHandle_psr.Reset();

			if (a_message)
			{
				Broadcast(0x35, true, "The world is saving, please wait.");
			}

			SaveStrategy _saveStrategy = SaveStrategy.Acquire();
			Console.WriteLine("Core: Using {0} save strategy", _saveStrategy.Name.ToLowerInvariant());

			Console.Write("World: Saving...");

			Stopwatch _stopWatch = Stopwatch.StartNew();

			if (!Directory.Exists("Saves/Mobiles/"))
			{
				Directory.CreateDirectory("Saves/Mobiles/");
			}
			if (!Directory.Exists("Saves/Items/"))
			{
				Directory.CreateDirectory("Saves/Items/");
			}
			if (!Directory.Exists("Saves/Guilds/"))
			{
				Directory.CreateDirectory("Saves/Guilds/");
			}
			if (!Directory.Exists("Saves/Customs/"))
			{
				Directory.CreateDirectory("Saves/Customs/");
			}

			/*using ( SaveMetrics metrics = new SaveMetrics() ) {*/
			_saveStrategy.Save(null, is_permitedBackgroundWrite);
			/*}*/

			try
			{
				EventSink.InvokeWorldSave(new WorldSaveEventArgs(a_message));
			}
			catch (Exception e)
			{
				throw new Exception("World Save event threw an exception.  Save failed!", e);
			}

			_stopWatch.Stop();

			isSaving_ps = false;

			if (!is_permitedBackgroundWrite)
			{
				NotifyDiskWriteComplete();
				//Sets the DiskWriteHandle.  If we allow background writes, we leave this upto the individual save strategies.
			}

			ProcessSafetyQueues();

			_saveStrategy.ProcessDecay();

			Console.WriteLine("Save finished in {0:F2} seconds.", _stopWatch.Elapsed.TotalSeconds);

			if (a_message)
			{
				Broadcast(0x35, true, "World save complete. The entire process took {0:F1} seconds.", _stopWatch.Elapsed.TotalSeconds);
			}

			NetState.Resume();
		}

		internal static List<Type> itemTypesList_is = new List<Type>();
		internal static List<Type> mobileTypesList_is = new List<Type>();
		internal static List<Type> dataTypesList_is = new List<Type>();

		public static IEntity FindEntity(Serial a_serial)
		{
			if (a_serial.IsItem)
			{
				return FindItem(a_serial);
			}
			else if (a_serial.IsMobile)
			{
				return FindMobile(a_serial);
			}

			return null;
		}

		public static ICustomsEntity FindCustomEntity(CustomSerial a_serial)
		{
			if (a_serial.IsValid)
			{
				return GetData(a_serial);
			}

			return null;
		}

		public static Mobile FindMobile(Serial a_serial)
		{
			Mobile _mobile;

			mobilesDictionary_ps.TryGetValue(a_serial, out _mobile);

			return _mobile;
		}

		public static void AddMobile(Mobile a_mobile)
		{
			if (isSaving_ps)
			{
				AppendSafetyLog("add", a_mobile);
				addQueue_ps.Enqueue(a_mobile);
			}
			else
			{
				mobilesDictionary_ps[a_mobile.Serial] = a_mobile;
			}
		}

		public static Item FindItem(Serial a_serial)
		{
			Item an_item;

			itemsDictionary_ps.TryGetValue(a_serial, out an_item);

			return an_item;
		}

		public static void AddItem(Item an_item)
		{
			if (isSaving_ps)
			{
				AppendSafetyLog("add", an_item);
				addQueue_ps.Enqueue(an_item);
			}
			else
			{
				itemsDictionary_ps[an_item.Serial] = an_item;
			}
		}

		public static void RemoveMobile(Mobile a_mobile)
		{
			mobilesDictionary_ps.Remove(a_mobile.Serial);
		}

		public static void RemoveItem(Item an_item)
		{
			itemsDictionary_ps.Remove(an_item.Serial);
		}

		public static void AddData(SaveData a_saveData)
		{
			if (isSaving_ps)
			{
				AppendSafetyLog("add", a_saveData);
				customsAddQueue_ps.Enqueue(a_saveData);
			}
			else
			{
				dataDictionary_ps[a_saveData.Serial] = a_saveData;
			}
		}

		public static void RemoveData(SaveData a_saveData)
		{
			dataDictionary_ps.Remove(a_saveData.Serial);
		}

		public static SaveData GetData(CustomSerial a_customSerial)
		{
			SaveData a_saveData;

			dataDictionary_ps.TryGetValue(a_customSerial, out a_saveData);

			return a_saveData;
		}

		public static SaveData GetData(string a_name)
		{
			foreach (SaveData a_saveData in dataDictionary_ps.Values)
			{
				if (a_saveData.Name == a_name)
				{
					return a_saveData;
				}
			}

			return null;
		}

		public static SaveData GetData(Type a_type)
		{
			foreach (SaveData a_saveData in dataDictionary_ps.Values)
			{
				if (a_saveData.GetType() == a_type)
				{
					return a_saveData;
				}
			}

			return null;
		}

		public static List<SaveData> GetDataList(Type a_type)
		{
			List<SaveData> _saveDataList = new List<SaveData>();

			foreach (SaveData _saveData in dataDictionary_ps.Values)
			{
				if (_saveData.GetType() == a_type)
				{
					_saveDataList.Add(_saveData);
				}
			}

			return _saveDataList;
		}

		public static List<SaveData> SearchData(string a_search)
		{
			string[] keywords = a_search.ToLower().Split(' ');
			List<SaveData> _saveDataList = new List<SaveData>();

            bool _isMatch;
            string _name;

			foreach (SaveData _SaveData in dataDictionary_ps.Values)
			{
				_isMatch = true;
				_name = _SaveData.Name.ToLower();

				for (int i = 0; i < keywords.Length; i++)
				{
					if (_name.IndexOf(keywords[i]) == -1)
					{
						_isMatch = false;
					}
				}

				if (_isMatch)
				{
                    _saveDataList.Add(_SaveData);
				}
			}

			return _saveDataList;
		}

		public static SaveData GetCore(Type a_type)
		{
			foreach (SaveData _saveData in dataDictionary_ps.Values)
			{
				if (_saveData.GetType() == a_type)
				{
					return _saveData;
				}
			}

			return null;
		}

		public static List<SaveData> GetCores(Type a_type)
		{
			List<SaveData> _saveDataList = new List<SaveData>();

			foreach (SaveData a_saveData in dataDictionary_ps.Values)
			{
				if (a_saveData.GetType() == a_type)
				{
                    _saveDataList.Add(a_saveData);
				}
			}

			return _saveDataList;
		}

		public static BaseModule GetModule(Mobile a_mobile)
		{
            BaseModule _baseModule;
			foreach (SaveData _saveData in dataDictionary_ps.Values)
			{
				if (_saveData is BaseModule)
				{
                    _baseModule = _saveData as BaseModule;

                    if (_baseModule.LinkedMobile == a_mobile)
                    {
                        return _baseModule;
                    }
				}
			}

			return null;
		}

		public static List<BaseModule> GetModules(Mobile a_mobile)
		{
			List<BaseModule> _baseModuleList = new List<BaseModule>();

            BaseModule _baseModule;
			foreach (SaveData _saveData in dataDictionary_ps.Values)
			{
				if (_saveData is BaseModule)
				{
                    _baseModule = _saveData as BaseModule;

					if (_baseModule.LinkedMobile == a_mobile)
					{
						_baseModuleList.Add(_baseModule);
					}
				}
			}

			return _baseModuleList;
		}

		public static BaseModule GetModule(Item an_item)
		{
            BaseModule _baseModule;
			foreach (SaveData data in dataDictionary_ps.Values)
			{
				if (data is BaseModule)
				{
					_baseModule = data as BaseModule;

					if (_baseModule.LinkedItem == an_item)
					{
						return _baseModule;
					}
				}
			}

			return null;
		}

		public static List<BaseModule> GetModules(Item an_item)
		{
			List<BaseModule> _baseModuleList = new List<BaseModule>();

            BaseModule _baseModule;
			foreach (SaveData _saveData in dataDictionary_ps.Values)
			{
				if (_saveData is BaseModule)
				{
                    _baseModule = _saveData as BaseModule;

					if (_baseModule.LinkedItem == an_item)
					{
						_baseModuleList.Add(_baseModule);
					}
				}
			}

			return _baseModuleList;
		}

		public static BaseModule GetModule(Mobile a_mobile, string a_baseModuleName)
		{
            BaseModule _baseModule;
			foreach (SaveData _saveData in dataDictionary_ps.Values)
			{
				if (_saveData is BaseModule)
				{
                    _baseModule = _saveData as BaseModule;

                    if (_baseModule.Name == a_baseModuleName && _baseModule.LinkedMobile == a_mobile)
					{
						return _baseModule;
					}
				}
			}

			return null;
		}

		public static List<BaseModule> GetModules(Mobile a_mobile, string a_baseModuleName)
		{
		    List<BaseModule> _baseModuleList = new List<BaseModule>();

            BaseModule _baseModule;
            foreach (SaveData _saveData in dataDictionary_ps.Values)
			{
				if (_saveData is BaseModule)
				{
                    _baseModule = _saveData as BaseModule;

					if (_baseModule.Name == a_baseModuleName && _baseModule.LinkedMobile == a_mobile)
					{
						_baseModuleList.Add(_baseModule);
					}
				}
			}

			return _baseModuleList;
		}

		public static BaseModule GetModule(Mobile a_mobile, Type a_type)
		{
            BaseModule _baseModule;
			foreach (SaveData _SaveData in dataDictionary_ps.Values)
			{
				if (_SaveData is BaseModule)
				{
                    _baseModule = _SaveData as BaseModule;

					if (_baseModule.GetType() == a_type && _baseModule.LinkedMobile == a_mobile)
					{
						return _baseModule;
					}
				}
			}

			return null;
		}

		public static BaseModule GetModule(Item an_item, string a_baseModuleName)
		{
            BaseModule _baseModule;
			foreach (SaveData _saveData in dataDictionary_ps.Values)
			{
				if (_saveData is BaseModule)
				{
					_baseModule = _saveData as BaseModule;

					if (_baseModule.Name == a_baseModuleName && _baseModule.LinkedItem == an_item)
					{
						return _baseModule;
					}
				}
			}

			return null;
		}

		public static List<BaseModule> GetModules(Item an_item, string a_baseModuleName)
		{
			List<BaseModule> _baseModuleList = new List<BaseModule>();

            BaseModule _baseModule;
			foreach (SaveData _saveData in dataDictionary_ps.Values)
			{
				if (_saveData is BaseModule)
				{
                    _baseModule = _saveData as BaseModule;

					if (_baseModule.Name == a_baseModuleName && _baseModule.LinkedItem == an_item)
					{
						_baseModuleList.Add(_baseModule);
					}
				}
			}

			return _baseModuleList;
		}

		public static BaseModule GetModule(Item an_item, Type an_type)
		{
            BaseModule _baseModule;
			foreach (SaveData _saveData in dataDictionary_ps.Values)
			{
				if (_saveData is BaseModule)
				{
					_baseModule = _saveData as BaseModule;

					if (_baseModule.GetType() == an_type && _baseModule.LinkedItem == an_item)
					{
						return _baseModule;
					}
				}
			}

			return null;
		}

		public static List<BaseModule> SearchModules(Mobile a_mobile, string a_search)
		{
			string[] _searchArray = a_search.ToLower().Split(' ');
			List<BaseModule> _baseModuleList = new List<BaseModule>();

            BaseModule _baseModule;
            bool _isMatch;
            string _moduleName;

			foreach (SaveData _saveData in dataDictionary_ps.Values)
			{
				if (_saveData is BaseModule)
				{
				    _baseModule = _saveData as BaseModule;

					_isMatch = true;
					_moduleName = _baseModule.Name.ToLower();

					for (int i = 0; i < _searchArray.Length; i++)
					{
						if (_moduleName.IndexOf(_searchArray[i]) == -1)
						{
							_isMatch = false;
						}
					}

					if (_isMatch && _baseModule.LinkedMobile == a_mobile)
					{
						_baseModuleList.Add(_baseModule);
					}
				}
			}

			return _baseModuleList;
		}

		public static List<BaseModule> SearchModules(Item an_item, string a_baseModuleName)
		{
			string[] _namesArray = a_baseModuleName.ToLower().Split(' ');
			List<BaseModule> _baseModulesList = new List<BaseModule>();

            BaseModule _baseModule;
            bool _isMatch;
            string _name;
			foreach (SaveData _saveData in dataDictionary_ps.Values)
			{
				if (_saveData is BaseModule)
				{
					_baseModule = _saveData as BaseModule;

					_isMatch = true;
					_name = _baseModule.Name.ToLower();

					for (int i = 0; i < _namesArray.Length; i++)
					{
						if (_name.IndexOf(_namesArray[i]) == -1)
						{
							_isMatch = false;
						}
					}

					if (_isMatch && _baseModule.LinkedItem == an_item)
					{
						_baseModulesList.Add(_baseModule);
					}
				}
			}

			return _baseModulesList;
		}
	}
}