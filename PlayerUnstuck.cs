﻿using System;
using System.Collections.Generic;
using System.Globalization;
using Exiled.API.Enums;
using Exiled.API.Extensions;
using Exiled.API.Features;
using UnityEngine;

namespace ArithFeather.PlayerUnstuck
{
	public class PlayerUnstuck : Plugin<Config>
	{
		public static Config Configs { get; private set; }
		public static CultureInfo CachedCultureInfo { get; private set; }

		public PlayerUnstuck()
		{
			StuckInRoom.InitializePool();
		}

		public override string Author => "Arith";
		public override Version Version => new Version(2, 11, 1);
		public override Version RequiredExiledVersion => new Version(2, 1, 3);

		public Dictionary<DoorType, StuckInRoom>
			ScpTryingToEscape = new Dictionary<DoorType, StuckInRoom>(Config.CacheSize);

		public override void OnEnabled()
		{
			Configs = Config;

			ClampWarning();

			Exiled.Events.Handlers.Player.InteractingDoor += Player_InteractingDoor;
			Exiled.Events.Handlers.Server.WaitingForPlayers += Server_WaitingForPlayers;
			Exiled.Events.Handlers.Server.ReloadedConfigs += ClampWarning;

			base.OnEnabled();
		}

		public override void OnDisabled()
		{
			Exiled.Events.Handlers.Player.InteractingDoor -= Player_InteractingDoor;
			Exiled.Events.Handlers.Server.WaitingForPlayers -= Server_WaitingForPlayers;
			Exiled.Events.Handlers.Server.ReloadedConfigs -= ClampWarning;

			base.OnDisabled();
		}

		private void ClampWarning()
		{
			Config.WarnDoorOpeningIn = Mathf.Clamp(Config.WarnDoorOpeningIn, 0, Config.TimeBeforeDoorOpens - 1);

			try
			{
				CachedCultureInfo = CultureInfo.GetCultureInfo(Config.LanguageCultureInfo);
			}
			catch
			{
				Log.Error("Wrong Culture Info. Defaulting to local language.");

				CachedCultureInfo = CultureInfo.DefaultThreadCurrentCulture;
			}

			Config.WarnBroadcast = string.Format(Config.WarnBroadcast, Config.TimeBeforeDoorOpens.ToString(CachedCultureInfo.NumberFormat));

		}

		private void Server_WaitingForPlayers()
		{
			ScpTryingToEscape.Clear();
			_fixedPoints.Clear();

			// Fix points
			var rooms = Map.Rooms;
			var roomCount = rooms.Count;

			foreach (var doorPoint in _rawPoints)
			{
				var point = doorPoint.Value;
				var roomType = point.RoomType;

				for (int i = 0; i < roomCount; i++)
				{
					var room = rooms[i];

					if (room.Type == roomType)
						_fixedPoints.Add(doorPoint.Key,
							new DoorPoint(roomType, room.Transform.TransformPoint(point.Position)));

				}
			}
		}

		private void Player_InteractingDoor(Exiled.Events.EventArgs.InteractingDoorEventArgs ev)
		{
			var door = ev.Door;
			var doorType = door.Type();
			var player = ev.Player;

			// If door closed and access denied and they are an SCP and they aren't already trying to escape.
			if (door.IsConsideredOpen() || ev.IsAllowed || (Config.SCPOnly && (!Config.SCPOnly || player.Team != Team.SCP)) ||
				player.Role == RoleType.Scp079 || player.Role == RoleType.Scp106 ||
				ScpTryingToEscape.ContainsKey(doorType)) return;

			if (_fixedPoints.TryGetValue(doorType, out var point))
			{

				var roomCheckPos = point.Position;
				var playerDistanceToRoom = Vector3.Distance(roomCheckPos, player.Position);
				var doorDistanceToRoom = Vector3.Distance(roomCheckPos, door.transform.position);

				if (playerDistanceToRoom < doorDistanceToRoom)
					ScpTryingToEscape.Add(doorType, StuckInRoom.SetPlayerStuck(ev.Player, door, this));
			}
		}

		private class DoorPoint
		{
			public readonly RoomType RoomType;
			public readonly Vector3 Position;

			public DoorPoint(RoomType roomType, Vector3 position)
			{
				this.RoomType = roomType;
				Position = position;
			}
		}

		private readonly Dictionary<DoorType, DoorPoint> _fixedPoints = new Dictionary<DoorType, DoorPoint>();

		private readonly Dictionary<DoorType, DoorPoint> _rawPoints = new Dictionary<DoorType, DoorPoint>
		{
			{DoorType.LczArmory, new DoorPoint(RoomType.LczArmory, new Vector3(2.468124f, 1.43f, -0.01200104f))},
			{DoorType.Scp012, new DoorPoint(RoomType.Lcz012, new Vector3(5.32489f, 1.430002f, -6.420892f))},
			{DoorType.Scp914, new DoorPoint(RoomType.Lcz914, new Vector3(1.833687f, 1.430001f, 0.1032865f))},
			{DoorType.HID, new DoorPoint(RoomType.HczHid, new Vector3(0.06840611f, 1.429993f, -9.714242f))},
			{DoorType.Scp079First, new DoorPoint(RoomType.Hcz079, new Vector3(15.22645f, -3.113281f, 0.02222576f))},
			{DoorType.Scp079Second, new DoorPoint(RoomType.Hcz079, new Vector3(6.521089f, -3.144531f, -14.89046f))},
			{DoorType.Scp106Bottom, new DoorPoint(RoomType.Hcz106, new Vector3(21.49849f, -18.66949f, -20.07209f))},
			{DoorType.Scp106Primary, new DoorPoint(RoomType.Hcz106, new Vector3(28.74622f, 1.329468f, 0.02032089f))},
			{DoorType.Scp106Secondary, new DoorPoint(RoomType.Hcz106, new Vector3(29.03088f, 1.329468f, -29.51972f))},
			{DoorType.Scp049Armory, new DoorPoint(RoomType.Hcz049, new Vector3(6.269577f, 265.4324f, 6.756578f))},
			{DoorType.NukeArmory, new DoorPoint(RoomType.HczNuke, new Vector3(-3.545931f, 401.43f, 18.09708f))},
			{DoorType.HczArmory, new DoorPoint(RoomType.HczArmory, new Vector3(2.363129f, 1.429993f, 0.1472318f))},
			{DoorType.Scp096, new DoorPoint(RoomType.Hcz096, new Vector3(-1.850082f, 1.429993f, -0.1229479f))},

			{DoorType.Intercom, new DoorPoint(RoomType.EzIntercom, new Vector3(7.382059f, -0.4788208f, 1.433885f))},

			{DoorType.NukeSurface, new DoorPoint(RoomType.Surface, new Vector3(40.67078f, -11.0451f, -36.2037f))}
		};
	}
}
