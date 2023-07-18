﻿using NetEasy;
using StarlightRiver.Core.Systems.DummyTileSystem;
using System;
using Terraria.DataStructures;

namespace StarlightRiver.Content.Packets
{
	[Serializable]
	public class SpawnDummy : Module
	{
		private readonly int fromWho;
		private readonly int type;
		private readonly int x;
		private readonly int y;

		public SpawnDummy(int fromWho, int type, int x, int y)
		{
			this.fromWho = fromWho;
			this.type = type;
			this.x = x;
			this.y = y;
		}

		protected override void Receive()
		{
			if (Main.netMode == Terraria.ID.NetmodeID.Server)
			{
				if (DummyTile.DummyExists(x, y, type))
				{
					//this case meant that a Player went up to a tile dummy that did not exist for them, but did on the server and we want to make sure they receive it
					Projectile dummyProj = DummyTile.GetDummy(x, y, type); 
					NetMessage.SendData(Terraria.ID.MessageID.SyncProjectile, number: dummyProj.whoAmI);
					return;
				}

				var p = new Projectile();
				p.SetDefaults(type);

				Vector2 spawnPos = new Vector2(x, y) * 16 + p.Size / 2;

				int n = Projectile.NewProjectile(new EntitySource_WorldEvent(), spawnPos, Vector2.Zero, type, 1, 0);

				var key = new Point16(x, y);
				DummyTile.dummies[key] = Main.projectile[n];
			}
		}
	}
}