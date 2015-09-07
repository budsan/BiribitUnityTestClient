using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;

public class TabBiribitMenu : Silver.UI.TabImmediate, BiribitListener
{
	private string address = "thatguystudio.com";
	private string password = "";
	private string clientName = "ClientName";
	private string appId = "app-client-test";
	private string chat = "";

	private int serverInfoSelected = 0;
	private int serverConnectionSelected = 0;
	private int roomSelected = 0;

	private List<string> serverInfoStrings = new List<string>();
	private List<string> serverConnectionStrings = new List<string>();
	private List<string> roomStrings = new List<string>();
	private List<string> connectionEntries = new List<string>();
	private List<string> chats = new List<string>();

	class RoomEntries
	{
		public uint RoomId = 0;
		public List<string> entries = new List<string>();
	}

	private Dictionary<uint, RoomEntries> entries = new Dictionary<uint,RoomEntries>();

	private uint connectionId = 0;

	private int slots = 4;
	private int jointSlot = 0;

	static char[] hexTable = new char[]{ '0', '1', '2' , '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'};
	private static bool IsPrintableCharacter(byte candidate)
	{
		return !(candidate < 0x20 || candidate > 127);
	}

	public override string TabName()
	{
		return "Biribit";
	}

	public override void DrawUI()
	{
		BiribitManager manager = BiribitManager.Instance;
		manager.AddListener(this);

		if (connectionId > Biribit.Client.UnassignedId)
		{
			RoomEntries roomEntries = null;
			if (!entries.TryGetValue(connectionId, out roomEntries)) {
				roomEntries = new RoomEntries();
				entries.Add(connectionId, roomEntries);
			}

			uint joinedRoom = manager.JoinedRoom(connectionId);
			if (roomEntries.RoomId != joinedRoom) {
				roomEntries.RoomId = joinedRoom;
				roomEntries.entries.Clear();
			}
		}

		ui.VerticalLayout(() =>
		{
			ui.TitleField("Menu");
			ui.LineSeparator();

			if(ui.Button("Reload scene"))
				Application.LoadLevel(Application.loadedLevel);

			ui.Separator(1);
			ui.LineSeparator();

			ui.LabelField("Address:");
			address = ui.StringField("Address", address, Silver.UI.Immediate.FlagMask.NoFieldLabel);
			ui.LabelField("Password:");
			password = ui.StringField("Password", password, Silver.UI.Immediate.FlagMask.NoFieldLabel);
			if (ui.Button("Connect"))
				manager.Connect(address, 0, password);
			

			ui.Separator(1);
			ui.LineSeparator();
			if (ui.Button("Discover in LAN"))
				manager.DiscoverServersOnLAN();

			if (ui.Button("Clear list of servers"))
				manager.ClearServerList();

			if (ui.Button("Refresh servers"))
				manager.RefreshServerList();

			Biribit.Native.ServerInfo[] serverInfoArray = manager.ServerInfo;
			if (serverInfoArray.Length > 0)
			{
				serverInfoStrings.Clear();
				foreach (Biribit.Native.ServerInfo serverInfo in serverInfoArray)
				{
					StringBuilder builder = new StringBuilder();
					builder.Append(serverInfo.name); builder.Append(", ping "); builder.Append(serverInfo.ping);
					builder.Append(serverInfo.passwordProtected != 0 ? ". Password protected." : ". No password.");
					serverInfoStrings.Add(builder.ToString());
				}
				ui.Separator(1);
				ui.LabelField("Server");
				ui.LineSeparator();
				serverInfoSelected = ui.Popup("Server", serverInfoSelected, serverInfoStrings.ToArray(), Silver.UI.Immediate.FlagMask.NoFieldLabel);
				Biribit.Native.ServerInfo info = serverInfoArray[serverInfoSelected];

				if (ui.Button("Connect selected"))
					manager.Connect(info.addr, info.port);
			}

			Biribit.Native.Connection[] serverConnectionArray = manager.Connections;
			if (serverConnectionArray.Length == 0)
			{
				connectionId = Biribit.Client.UnassignedId;
			}
			else
			{
				serverConnectionStrings.Clear();
				foreach (Biribit.Native.Connection serverConnection in serverConnectionArray)
				{
					StringBuilder builder = new StringBuilder();
					builder.Append(serverConnection.id); builder.Append(": ");
					builder.Append(serverConnection.name); builder.Append(". Ping: "); builder.Append(serverConnection.ping);
					serverConnectionStrings.Add(builder.ToString());
				}

				ui.Separator(1);
				ui.LabelField("Connection");
				ui.LineSeparator();
				serverConnectionSelected = ui.Popup("Connection", serverConnectionSelected, serverConnectionStrings.ToArray(), Silver.UI.Immediate.FlagMask.NoFieldLabel);
				Biribit.Native.Connection connection = serverConnectionArray[serverConnectionSelected];
				connectionId = connection.id;
				if (ui.Button("Disconnect"))
					manager.Disconnect(connectionId);

				if (ui.Button("Disconnect all"))
					manager.Disconnect();

				ui.Separator(1);
				ui.LineSeparator();
				clientName = ui.StringField("Client name", clientName);
				appId = ui.StringField("Application Id", appId);
				if (ui.Button("Set name and appid"))
					manager.SetLocalClientParameters(connectionId, clientName, appId);

				Biribit.Native.RemoteClient[] remoteClientsArray = manager.RemoteClients(connectionId);
				uint localClientId = manager.LocalClientId(connectionId);
				if (remoteClientsArray.Length > 0)
				{
					ui.Separator(1);
					ui.LabelField("Client list");
					ui.LineSeparator();
					foreach (Biribit.Native.RemoteClient remoteClient in remoteClientsArray)
					{
						StringBuilder builder = new StringBuilder();
						builder.Append(remoteClient.id); builder.Append(": ");
						builder.Append(remoteClient.name);
						if (remoteClient.id == localClientId)
							builder.Append(" << YOU");
						ui.LabelField(builder.ToString(), 14);
					}
				}

				ui.Separator(1);
				ui.LabelField("Create room");
				ui.LineSeparator();
				ui.HorizontalLayout(() =>
				{
					slots = ui.IntField("Num slots", slots);
					if (ui.Button("Create"))
						manager.CreateRoom(connectionId, (byte)slots);
				});
				
				ui.HorizontalLayout(() =>
				{
					jointSlot = ui.IntField("Joining slot", jointSlot);
				if (ui.Button("& Join"))
						manager.CreateRoom(connectionId, (byte)slots, (byte)jointSlot);
				});

				if (ui.Button("Random or create")) {
					manager.JoinRandomOrCreateRoom(connectionId, (byte)slots);
				}

				Biribit.Room[] roomArray = manager.Rooms(connectionId);
				uint joinedRoomId = manager.JoinedRoom(connectionId);
				uint joinedRoomSlot = manager.JoinedRoomSlot(connectionId);
				if (roomArray.Length > 0)
				{
					roomStrings.Clear();
					foreach (Biribit.Room room in roomArray)
					{
						StringBuilder builder = new StringBuilder();
						builder.Append("Room "); builder.Append(room.id);

						if (room.id == joinedRoomId)
						{
							builder.Append(" | Joined: ");
							builder.Append(joinedRoomSlot);
						}

						roomStrings.Add(builder.ToString());
					}

					ui.Separator(1);
					ui.LabelField("Rooms");
					ui.LineSeparator();
					roomSelected = ui.Popup("Room", roomSelected, roomStrings.ToArray(), Silver.UI.Immediate.FlagMask.NoFieldLabel);
					Biribit.Room rm = roomArray[roomSelected];

					if (ui.Button("Join"))
						manager.JoinRoom(connectionId, rm.id);

					if (ui.Button("Leave"))
						manager.JoinRoom(connectionId, 0);

					if (ui.Button("Refresh rooms"))
						manager.RefreshRooms(connectionId);
					
					ui.Separator(1);
					ui.LabelField("Room");
					ui.LineSeparator();

					for (int i = 0; i < rm.slots.Length; i++)
					{
						if (rm.slots[i] == Biribit.Client.UnassignedId)
							ui.LabelField("Slot " + i.ToString() + ": Free", 14);
						else
						{
							int pos = manager.RemoteClients(connectionId, rm.slots[i]);
							Debug.Log("pos: " + pos);
							if (pos < 0)
								ui.LabelField("Slot " + i.ToString() + ": " + rm.slots[i], 14);
							else
								ui.LabelField("Slot " + i.ToString() + ": " + manager.RemoteClients(connectionId)[pos].name, 14);
						}
					}
				}
				else
				{
					if (ui.Button("Refresh rooms"))
						manager.RefreshRooms(connectionId);
				}

				foreach (KeyValuePair<uint, RoomEntries> pair in entries)
				{
					ui.LabelField("Connection " + pair.Key + ". Room " + pair.Value.RoomId + ".");
					ui.LineSeparator();
					
					foreach(string entry in pair.Value.entries)
						ui.LabelField(entry, 14);
				}
			}

			ui.Separator(1);
			ui.LabelField("Notifications");
			ui.LineSeparator();
			if (connectionId > Biribit.Client.UnassignedId)
			{
				chat = ui.StringField("Chat", chat, Silver.UI.Immediate.FlagMask.NoFieldLabel);
				if (ui.Button("Send") && !string.IsNullOrEmpty(chat.Trim()))
				{
					byte[] data = ASCIIEncoding.ASCII.GetBytes(chat.Trim());
					manager.SendBroadcast(connectionId, data);
					chat = "";
				}

				if (ui.Button("Send as entry") && !string.IsNullOrEmpty(chat.Trim()))
				{
					byte[] data = ASCIIEncoding.ASCII.GetBytes(chat.Trim());
					manager.SendEntry(connectionId, data);
					chat = "";
				}
			}

			foreach (string linechat in chats) {
				ui.LabelField(linechat, 14);
			}

			ui.Separator(1);
			ui.LabelField("Entries");
			ui.LineSeparator();
			foreach (string lineentry in connectionEntries){
				ui.LabelField(lineentry, 14);
			}
		});
	}

	void BiribitListener.OnConnected(uint connectionId)
	{
		StringBuilder b = new StringBuilder();
		b.Append("[You have been connected on "); b.Append(connectionId); b.Append(" connection].");

		chats.Reverse();
		chats.Add(b.ToString());
		chats.Reverse();
	}

	void BiribitListener.OnDisconnected(uint connectionId)
	{
		StringBuilder b = new StringBuilder();
		b.Append("[You have been disconnected on "); b.Append(connectionId); b.Append(" connection].");

		chats.Reverse();
		chats.Add(b.ToString());
		chats.Reverse();
	}

	void BiribitListener.OnJoinedRoom(uint connectionId, uint roomId, byte slotId)
	{
		StringBuilder b = new StringBuilder();
		b.Append("[You joined at room"); b.Append(roomId);
		b.Append(" as a player ");
		b.Append(slotId + 1); b.Append("].");

		chats.Reverse();
		chats.Add(b.ToString());
		chats.Reverse();
	}

	void BiribitListener.OnJoinedRoomPlayerJoined(uint connectionId, uint clientId, byte slotId)
	{
		StringBuilder b = new StringBuilder();
		b.Append("[Client "); b.Append(clientId);
		b.Append(" joined the the room as a player ");
		b.Append(slotId+1); b.Append("].");

		chats.Reverse();
		chats.Add(b.ToString());
		chats.Reverse();
	}

	void BiribitListener.OnJoinedRoomPlayerLeft(uint connectionId, uint clientId, byte slotId)
	{
		StringBuilder b = new StringBuilder();
		b.Append("[Player "); b.Append(slotId + 1); b.Append(" left the room].");

		chats.Reverse();
		chats.Add(b.ToString());
		chats.Reverse();
	}

	void BiribitListener.OnBroadcast(Biribit.BroadcastEvent evnt)
	{
		StringBuilder b = new StringBuilder();
		var secs = evnt.when / 1000;
		var mins = secs / 60;
		b.Append(mins % 60); b.Append(":"); b.Append(secs % 60);
		b.Append("[Room "); b.Append(evnt.room_id); b.Append(", Player ");
		b.Append(evnt.slot_id); b.Append("]: ");
		b.Append(ASCIIEncoding.ASCII.GetString(evnt.data));

		chats.Reverse();
		chats.Add(b.ToString());
		chats.Reverse();
	}

	void BiribitListener.OnEntriesChanged(uint connectionId)
	{
		List<Biribit.Entry> entries = BiribitManager.Instance.JoinedRoomEntries(connectionId);
		for (int i = connectionEntries.Count + 1; i <= entries.Count; i++)
		{
			Biribit.Entry entry = entries[i];
			bool isPrintable = true;
			StringBuilder hex = new StringBuilder();
			for (int it = 0; it < entry.data.Length; it++)
			{
				byte val = entry.data[it];
				bool ok = ((it + 1) == entry.data.Length && val == 0) || IsPrintableCharacter(val);
				isPrintable = ok && isPrintable;

				char bytehex1 = hexTable[(val & 0xF0) >> 4];
				char bytehex2 = hexTable[(val & 0x0F) >> 0];
				hex.Append(bytehex1);
				hex.Append(bytehex2);
			}

			StringBuilder ss = new StringBuilder();
			ss.Append("[Player "); ss.Append(entry.slot_id + 1); ss.Append("]: ");
			if (isPrintable)
				ss.Append(Encoding.ASCII.GetString(entry.data));
			else
				ss.Append(hex.ToString());

			connectionEntries.Add(ss.ToString());
		}
	}

	void BiribitListener.OnLeaveRoom(uint connectionId)
	{
		StringBuilder b = new StringBuilder();
		b.Append("[You have left the room.]");

		chats.Reverse();
		chats.Add(b.ToString());
		chats.Reverse();
	}
}
