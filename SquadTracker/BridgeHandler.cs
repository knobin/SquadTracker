//
// From: https://github.com/knobin/arcdps_bridge/blob/main/examples/client.cs
//
// Requires the arcdps_bridge.dll to be present, together with ArcDPS, and ArcDPS Unofficial Extras.
//

using System;
using System.Text;
using System.Threading;
using System.IO.Pipes;
using System.Security.Principal;

namespace BridgeHandler
{
    internal class Subscribe
    {
        public bool Combat { get; set; }
        public bool Extras { get; set; }
        public bool Squad { get; set; }
        public MessageProtocol Protocol { get; set; } = MessageProtocol.Serial;
    }

    internal class BridgeInfo
    {
        public string version { get; set; }
        public string extrasVersion { get; set; }
        public string arcVersion { get; set; }
        public bool arcLoaded { get; set; }
        public bool extrasFound { get; set; }
        public bool extrasLoaded { get; set; }
        public UInt64 validator { get; set; }
        public UInt32 majorApiVersion { get; set; }
        public UInt32 minorApiVersion { get; set; }
        public UInt32 extrasInfoVersion { get; set; }
    }

    internal class ConnectionInfo
    {
        public bool CombatEnabled { get; set; }
        public bool ExtrasEnabled { get; set; }
        public bool ExtrasFound { get; set; }
        public bool SquadEnabled { get; set; }
        public BridgeInfo Info { get; set; }
    }

    internal class cbtevent
    {
        public UInt64 time { get; set; }
        public UInt64 src_agent { get; set; }
        public UInt64 dst_agent { get; set; }
        public Int32 value { get; set; }
        public Int32 buff_dmg { get; set; }
        public UInt32 overstack_value { get; set; }
        public UInt32 skillid { get; set; }
        public UInt16 src_instid { get; set; }
        public UInt16 dst_instid { get; set; }
        public UInt16 src_master_instid { get; set; }
        public UInt16 dst_master_instid { get; set; }
        public Byte iff { get; set; }
        public Byte buff { get; set; }
        public Byte result { get; set; }
        public Byte is_activation { get; set; }
        public Byte is_buffremove { get; set; }
        public Byte is_ninety { get; set; }
        public Byte is_fifty { get; set; }
        public Byte is_moving { get; set; }
        public Byte is_statechange { get; set; }
        public Byte is_flanking { get; set; }
        public Byte is_shields { get; set; }
        public Byte is_offcycle { get; set; }
    }

    internal class ag
    {
        public string name { get; set; }
        public UInt64 id { get; set; }
        public UInt32 prof { get; set; }
        public UInt32 elite { get; set; }
        public UInt32 self { get; set; }
        public UInt16 team { get; set; }
    }

    internal class PlayerInfo
    {
        public string accountName { get; set; }
        public string characterName { get; set; }
        public Int64 joinTime { get; set; }
        public UInt32 profession { get; set; }
        public UInt32 elite { get; set; }
        public Byte role { get; set; }
        public Byte subgroup { get; set; }
        public bool inInstance { get; set; }
        public bool self { get; set; }
        public bool readyStatus { get; set; }
    }

    internal class PlayerInfoEntry
    {
        public PlayerInfo player { get; set; }
        public UInt64 validator{ get; set; }
}

    internal class CombatEvent
    {
        UInt64 id { get; set; }
        UInt64 revision { get; set; }
        public cbtevent ev { get; set; }
        public ag src { get; set; }
        public ag dst { get; set; }
        public string skillname { get; set; }
    }

    internal class SquadStatus
    {
        public string self { get; set; }
        public PlayerInfoEntry[] members { get; set; }
    }

    internal enum MessageProtocol : Byte
    {
        Serial  = 1,
        JSON    = 2
    }

    internal class Handler
    {
        private class StatusEvent
        {
            public bool success { get; set; }
            public string error { get; set; }
        }

        private enum MessageCategory : Byte
        {
            None    = 0,
            Info    = 1,
            Combat  = 2,
            Extras  = 4,
            Squad   = 8
        }

        private enum MessageType : Byte
        {
            None        = 0,

            // Info types.
            BridgeInfo  = 1,
            Status      = 2,
            Closing     = 3,

            // ArcDPS combat api types.
            CombatEvent = 4,

            // Extras event types.
            ExtrasSquadUpdate = 5,
            // ExtrasLanguageChanged = 6,   // TODO
            // ExtrasKeyBindChanged = 7,    // TODO
            // ExtrasChatMessage = 8,       // TODO

            // Squad event types.
            SquadStatus = 9,
            SquadAdd    = 10,
            SquadUpdate = 11,
            SquadRemove = 12
        }

        private struct MessageHeader
        {
            public MessageCategory category { get; set; }
            public MessageType type { get; set; }
            public UInt64 id { get; set; }
            public UInt64 timestamp { get; set; }
        }

        private static MessageHeader ParseMessageHeader(dynamic msg)
        {
            var header = new MessageHeader()
            {
                category = MessageCategory.None,
                type = MessageType.None
            };

            var categoryString = (string)msg["category"];
            if (Enum.TryParse(categoryString, out MessageCategory category))
                header.category = category;

            var typeString = (string)msg["type"];
            if (Enum.TryParse(typeString, out MessageType type))
                header.type = type;

            return header;
        }

        private class SerialRead<T>
        {
            public T Data;
            public int Count;
        }

        private static SerialRead<byte[]> ReadFromPipe(NamedPipeClientStream stream)
        {
            var messageBuffer = new byte[64];
            var offset = 0;
            var readCount = 0;

            do
            {
                var maxRead = messageBuffer.Length - offset;
                var count = stream.Read(messageBuffer, offset, maxRead);
                readCount += count;

                if (stream.IsConnected && !stream.IsMessageComplete)
                {
                    offset += count;
                    if (count == maxRead)
                    {
                        var buffer = new byte[(int)(messageBuffer.Length * 1.5f)];
                        messageBuffer.CopyTo(buffer, 0);
                        messageBuffer = buffer;
                    }
                }
            }
            while (stream.IsConnected && !stream.IsMessageComplete);

            // Could make a struct and have pointer and count values to mitigate this unnecessary copy.
            //byte[] final = new byte[read_count];
            //Array.Copy(messageBuffer, final, read_count);
            return new SerialRead<byte[]>()
            {
                Data = messageBuffer,
                Count = readCount
            };
        }

        private static void WriteToPipe(NamedPipeClientStream stream, string message)
        {
            var messageBytes = Encoding.UTF8.GetBytes(message);
            stream.Write(messageBytes, 0, messageBytes.Length);
        }

        public delegate void SquadInfo(SquadStatus squad);
        public delegate void PlayerChange(PlayerInfoEntry entry);
        public delegate void CombatMessage(CombatEvent evt);

        // Squad information events.
        public event SquadInfo OnSquadStatusEvent;
        public event PlayerChange OnPlayerAddedEvent;
        public event PlayerChange OnPlayerUpdateEvent;
        public event PlayerChange OnPlayerRemovedEvent;

        // Connection events.
        public delegate void ConnectedHandler(bool connected);
        public delegate void ConnectionInfoHandler(ConnectionInfo info);
        public event ConnectedHandler OnConnectionUpdate;
        public event ConnectionInfoHandler OnConnectionInfo;

        // ArcDPS event.
        public event CombatMessage OnCombatEvent;

        // Unofficial Extras event.
        // TODO.

        private class ThreadData
        {
            public NamedPipeClientStream ClientStream = null;
            public Handler Handle = null;
            public bool Run { get; set; } = false;
            public bool Connected { get; set; } = false;
            public Byte EnabledCategories { get; set; }
            public MessageProtocol Protocol { get; set; }
        }

        private Thread _t;
        private ThreadData _tData = new ThreadData();
        public static int SupportedMajorVersion { get; private set; } = 2;

        public void Start(Subscribe subscribe)
        {
            _t = new Thread(PipeThreadMain);
            _tData = new ThreadData()
            {
                Handle = this,
                EnabledCategories = (Byte)MessageCategory.None,
                Run = true,
                Connected = false,
                Protocol = subscribe.Protocol
            };
            
            if (subscribe.Combat)
                _tData.EnabledCategories |= (Byte)MessageCategory.Combat;
            if (subscribe.Extras)
                _tData.EnabledCategories |= (Byte)MessageCategory.Extras;
            if (subscribe.Squad)
                _tData.EnabledCategories |= (Byte)MessageCategory.Squad;

            _t.Start(_tData);
        }

        public void Stop()
        {
            _tData.Run = false;
            _tData.ClientStream?.Close();
            _t.Join();
        }

        public bool IsConnected()
        {
            return _tData.Connected;
        }

        public bool IsRunning()
        {
            return _tData.Run;
        }

        private static int SetupConnection(ThreadData tData)
        {
            // Read BridgeInfo.
            var pipeData = ReadFromPipe(tData.ClientStream);
            var stringData = Encoding.UTF8.GetString(pipeData.Data, 0, pipeData.Count);
            var msg = Newtonsoft.Json.Linq.JObject.Parse(stringData);
            pipeData.Data = null;
            pipeData.Count = 0;

            var header = ParseMessageHeader(msg);
            if (header.category != MessageCategory.Info || header.type != MessageType.BridgeInfo)
                return 1;

            var info = msg["data"]?.ToObject<BridgeInfo>();
            if (info == null)
                return 1;
            
            HandleBridgeInfo(info, tData);

            if (info.majorApiVersion != SupportedMajorVersion)
                return 2;

            // Send subscribe data to server.
            var subscribe = tData.EnabledCategories;
            var sub = "{\"subscribe\":" + subscribe.ToString() + ",\"protocol\":\"Serial\"}";
            WriteToPipe(tData.ClientStream, sub);

            // Read return status.
            pipeData = ReadFromPipe(tData.ClientStream);
            stringData = Encoding.UTF8.GetString(pipeData.Data, 0, pipeData.Count);
            pipeData.Data = null;
            pipeData.Count = 0;
            msg = Newtonsoft.Json.Linq.JObject.Parse(stringData);
            header = ParseMessageHeader(msg);
            if (header.category != MessageCategory.Info || header.type != MessageType.Status)
                return 1;

            var status = msg["data"]?.ToObject<StatusEvent>();
            return (status is { success: true }) ? 0 : 1;
        }

        private static void PipeThreadMain(Object parameterData)
        {
            var tData = (ThreadData)parameterData;
            Action<byte[], ThreadData> parseFunc = ParseMessageSerial;

            if (tData.Protocol == MessageProtocol.JSON)
                parseFunc = ParseMessageJSON;

            while (tData.Run)
            {
                tData.ClientStream = new NamedPipeClientStream(".", "arcdps-bridge", PipeDirection.InOut, PipeOptions.Asynchronous, TokenImpersonationLevel.Impersonation);
                if (tData.ClientStream == null)
                    continue;

                tData.ClientStream.Connect();
                tData.ClientStream.ReadMode = PipeTransmissionMode.Message;

                if (!tData.ClientStream.IsConnected)
                {
                    tData.ClientStream.Close();
                    tData.ClientStream = null;
                    continue;
                }

                // ClientStream is connected here.
                tData.Connected = true;

                var status = SetupConnection(tData);
                if (status == 2)
                {
                    // End thread here.
                    // API major version is incompatible.
                    tData.ClientStream.Close();
                    tData.ClientStream = null;
                    tData.Connected = false;
                    tData.Run = false;
                    continue;
                }
                
                if (status != 0)
                {
                    // Connection could not be setup properly.
                    tData.ClientStream.Close();
                    tData.ClientStream = null;
                    tData.Connected = false;
                    continue;
                }

                // Bridge is connected here, invoke callback.
                tData.Handle.OnConnectionUpdate?.Invoke(true);

                while (tData.Run && tData.ClientStream.IsConnected)
                {
                    var pipeData = ReadFromPipe(tData.ClientStream);
                    if (pipeData.Count > 1)
                        parseFunc(pipeData.Data, tData);
                    pipeData.Data = null;
                    pipeData.Count = 0;
                }

                // Stream is not connected here, or tData.Run is false.
                tData.ClientStream?.Close();
                tData.ClientStream = null;
                tData.Connected = false;

                // Bridge disconnected here, invoke callback.
                tData.Handle.OnConnectionUpdate?.Invoke(false);
            }
            
            // Thread is ending, close stream if open.
            if (tData.ClientStream != null)
            {
                tData.ClientStream.Close();
                tData.ClientStream = null;
                tData.Connected = false;
                // Bridge disconnected here, invoke callback.
                tData.Handle.OnConnectionUpdate?.Invoke(false);
            }
        }

        private static SerialRead<string> ReadStringFromBytes(byte[] data, int index)
        {
            var count = 0;

            for (var i = index; i < data.Length; i++)
            {
                if (data[i] == 0) // Null teminator found.
                    break;
                ++count;
            }

            return new SerialRead<string>()
            {
                Data = Encoding.UTF8.GetString(data, index, count),
                Count = count + 1
            };
        }

        private static SerialRead<PlayerInfo> ParsePlayerInfo(byte[] data, int index)
        {
            var offset = index;

            var account = ReadStringFromBytes(data, offset);
            var character = ReadStringFromBytes(data, offset + account.Count);

            offset += account.Count + character.Count;

            var serial = new SerialRead<PlayerInfo>
            {
                Data = new PlayerInfo
                {
                    accountName = account.Data,
                    characterName = character.Data,
                    joinTime = BitConverter.ToInt64(data, offset),
                    profession = BitConverter.ToUInt32(data, offset + 8),
                    elite = BitConverter.ToUInt32(data, offset + 8 + 4),
                    role = data[offset + 8 + 4 + 4],
                    subgroup = data[offset + 8 + 4 + 4 + 1],
                    inInstance = BitConverter.ToBoolean(data, offset + 8 + 4 + 4 + 1 + 1),
                    self = BitConverter.ToBoolean(data, offset + 8 + 4 + 4 + 1 + 1 + 1),
                    readyStatus = BitConverter.ToBoolean(data, offset + 8 + 4 + 4 + 1 + 1 + 1 + 1)
                },

                Count = account.Count + character.Count + 8 + 4 + 4 + 1 + 1 + 1 + 1 + 1
            };

            return serial;
        }

        private static SerialRead<PlayerInfoEntry> ParsePlayerInfoEntry(byte[] data, int index)
        {
            var serial = new SerialRead<PlayerInfoEntry>();

            var player = ParsePlayerInfo(data, index);
            serial.Data = new PlayerInfoEntry()
            {
                player = player.Data,
                validator = BitConverter.ToUInt64(data, index + player.Count)
            };

            serial.Count = player.Count + 8;
            return serial;
        }

        private static SerialRead<SquadStatus> ParseSquadStatus(byte[] data, int index)
        {
            var serial = new SerialRead<SquadStatus>();
            var self = ReadStringFromBytes(data, index);

            serial.Data = new SquadStatus
            {
                self = self.Data
            };
            var offset = index + self.Count;

            var count = BitConverter.ToUInt64(data, offset);
            offset += 8;

            serial.Data.members = new PlayerInfoEntry[count];

            for (UInt64 i = 0; i < count; i++)
            {
                var entry = ParsePlayerInfoEntry(data, offset);
                serial.Data.members[i] = entry.Data;
                offset += entry.Count;
            }

            serial.Count = offset - index;
            return serial;
        }

        private static SerialRead<BridgeInfo> ParseBridgeInfo(byte[] data, int index)
        {
            var offset = index;

            var majorApiVersion = BitConverter.ToUInt32(data, offset);
            offset += 4;

            var minorApiVersion = BitConverter.ToUInt32(data, offset);
            offset += 4;

            var validator = BitConverter.ToUInt64(data, offset);
            offset += 8;

            var version = ReadStringFromBytes(data, index);
            offset += version.Count;

            var extras_version = ReadStringFromBytes(data, index);
            offset += extras_version.Count;

            var arc_version = ReadStringFromBytes(data, index);
            offset += arc_version.Count;

            var extrasInfoVersion = BitConverter.ToUInt32(data, offset);
            offset += 4;

            var serial = new SerialRead<BridgeInfo>()
            {
                Data = new BridgeInfo()
                {
                    version = version.Data,
                    extrasVersion = extras_version.Data,
                    arcVersion = arc_version.Data,
                    validator = validator,
                    majorApiVersion = majorApiVersion,
                    minorApiVersion = minorApiVersion,
                    extrasInfoVersion = extrasInfoVersion,
                    arcLoaded = BitConverter.ToBoolean(data, offset),
                    extrasFound = BitConverter.ToBoolean(data, offset + 1),
                    extrasLoaded = BitConverter.ToBoolean(data, offset + 2),
                },
                Count = version.Count + extras_version.Count + arc_version.Count + 8 + 3
            };

            return serial;
        }

        private static void ParseMessageSerial(byte[] pipeData, ThreadData tData)
        {
            var header = new MessageHeader()
            {
                category = (MessageCategory)pipeData[0],
                type = (MessageType)pipeData[1],
                id = BitConverter.ToUInt64(pipeData, 2),
                timestamp = BitConverter.ToUInt64(pipeData, 10)
            };

            const int headerOffset = 18;

            switch (header.category)
            {
                case MessageCategory.Info:
                {
                    if (header.type == MessageType.Closing)
                    {
                        // Server closing the connection, no more events will be sent.
                        tData.Run = false;
                    }
                    else if (header.type == MessageType.BridgeInfo)
                    {
                        var info = ParseBridgeInfo(pipeData, headerOffset);
                        HandleBridgeInfo(info.Data, tData);
                    }

                    // MessageType.Status is only sent in json form and also only in the setup phase.
                    // So it will never be received and handled here.
                    break;
                }
                case MessageCategory.Combat:
                { 
                    // TODO(knobin): Add Serial parsing for Combat events.
                    // tData.Handle.OnCombatEvent?.Invoke(entry);
                    break;
                }
                case MessageCategory.Extras:
                {
                    // TODO(knobin): Add Serial parsing for Extras events.
                    // tData.Handle.OnExtrasEvent?.Invoke(evt.extras);
                    break;
                }
                case MessageCategory.Squad:
                {
                    if (header.type == MessageType.SquadStatus)
                    {
                        var squad = ParseSquadStatus(pipeData, headerOffset);
                        tData.Handle.OnSquadStatusEvent?.Invoke(squad.Data);
                    }
                    else if (header.type == MessageType.SquadAdd || header.type == MessageType.SquadUpdate || header.type == MessageType.SquadRemove)
                    {
                        var entry = ParsePlayerInfoEntry(pipeData, headerOffset);
                        HandleSquadEvent(entry.Data, header.type, tData);
                    }
                    break;
                }
                case MessageCategory.None:
                default:
                    break;
            }
        }

        private static void ParseMessageJSON(byte[] pipeData, ThreadData tData)
        {
            var stringData = Encoding.UTF8.GetString(pipeData, 0, pipeData.Length);
            var msg = Newtonsoft.Json.Linq.JObject.Parse(stringData);
            var header = ParseMessageHeader(msg);

            switch (header.category)
            {
                case MessageCategory.Info:
                {
                    if (header.type == MessageType.Closing)
                    {
                        // Server closing the connection, no more events will be sent.
                        tData.Run = false;
                    }
                    else if (header.type == MessageType.BridgeInfo)
                    {
                        var info = msg["data"]?.ToObject<BridgeInfo>();
                        if (info != null)
                            HandleBridgeInfo(info, tData);
                    }
                    break;
                }
                case MessageCategory.Combat:
                {
                    var evt = msg["data"]?.ToObject<CombatEvent>();
                    if (evt != null)
                        tData.Handle.OnCombatEvent?.Invoke(evt);
                    break;
                }
                case MessageCategory.Extras:
                {
                    // TODO(knobin): Add JSON parsing for Extras events.
                    // tData.Handle.OnExtrasEvent?.Invoke(evt.extras);
                    break;
                }
                case MessageCategory.Squad:
                {
                    if (header.type == MessageType.SquadStatus)
                    {
                        var squadStatus = msg["data"]?.ToObject<SquadStatus>();
                        if (squadStatus != null)
                            tData.Handle.OnSquadStatusEvent?.Invoke(squadStatus);
                    }
                    else if (header.type == MessageType.SquadAdd || header.type == MessageType.SquadUpdate || header.type == MessageType.SquadRemove)
                    {
                        var entry = msg["data"]?.ToObject<PlayerInfoEntry>();
                        if (entry != null)
                            HandleSquadEvent(entry, header.type, tData);
                    }
                    break;
                }
                case MessageCategory.None:
                default:
                    break;
            }
        }

        private static void HandleBridgeInfo(BridgeInfo bInfo, ThreadData tData)
        {
            // Connection info received here, invoke callback.
            var info = new ConnectionInfo()
            {
                CombatEnabled = bInfo.arcLoaded,
                ExtrasEnabled = bInfo.extrasLoaded,
                ExtrasFound = bInfo.extrasFound,
                SquadEnabled = bInfo.arcLoaded && bInfo.extrasLoaded,
                Info = bInfo
            };
            tData.Handle.OnConnectionInfo?.Invoke(info);
        }

        private static void HandleSquadEvent(PlayerInfoEntry entry, MessageType type, ThreadData tData)
        {
            if (type == MessageType.SquadAdd)
                tData.Handle.OnPlayerAddedEvent?.Invoke(entry);
            else if (type == MessageType.SquadUpdate)
                tData.Handle.OnPlayerUpdateEvent?.Invoke(entry);
            else if (type == MessageType.SquadRemove)
                tData.Handle.OnPlayerRemovedEvent?.Invoke(entry);
        }
    }
}
