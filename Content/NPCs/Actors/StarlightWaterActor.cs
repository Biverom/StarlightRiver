﻿using StarlightRiver.Content.Items.Food;
using StarlightRiver.Content.Items.Misc;
using StarlightRiver.Content.Items.Starwood;
using StarlightRiver.Content.Items.Vanity;
using System;
using System.Collections.Generic;
using System.Net;
using Terraria.ID;
using Terraria.ModLoader;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static Terraria.ModLoader.ModContent;

namespace StarlightRiver.Content.NPCs.Actors
{
	public class StarlightWaterActor : ModNPC
	{
		const int DUST_RANGE = 250;//used for horizontal dust distance and circularrange of underwater lights
		const int ITEM_RANGE = 200;//circular range for detecting items

		public Item targetItem;
		public int targetItemTransformType = 0;

		public int transformTimer = 0;
		const float TransformTimerLength = 300f;//time it takes to transform the item
		public int windDown = 0;
		const int WindDownTimerLength = 240;

		public bool HasTransformedItem = false;

		public override string Texture => AssetDirectory.Invisible;

		public override void SetDefaults()
		{
			NPC.width = 1;
			NPC.height = 1;
			NPC.lifeMax = 100;
			NPC.immortal = true;
			NPC.dontTakeDamage = true;
			NPC.friendly = true;
			NPC.aiStyle = -1;
			NPC.noGravity = true;
		}

		public void ResetConversion()
		{
			if (targetItem != null)
				targetItem.GetGlobalItem<TransformableItem>().starlightWaterActor = null;

			targetItem = null;
			targetItemTransformType = 0;
			transformTimer = 0;
			windDown = 0;//if HasTransformedItem is true setting this to zero will immediately despawn this
			//this does not reset HasTransformedItem, since if the item has been transformed we want this to immediately despawn and not to reset
		}

		public override void AI()
		{
			if (NPC.wet || WorldGen.SolidTile((int)(NPC.position.X / 16), (int)(NPC.position.Y / 16)))//float to surface if in water or blocks
				NPC.position.Y -= 1;
			else if (Main.tile[(int)(NPC.position.X / 16), (int)(NPC.position.Y / 16)].LiquidAmount < 1)
				NPC.position.Y += 1;

			if (!HasTransformedItem)//skips day check, item glow, and stops producing dust if item has been transformed and npc is waiting to depspawn
			{
				//stop everything and immediately despawn if day
				//does not get checked if on windDown, since the npc will be despawning soon anyway
				//this could also be skipped if an item is being transformed
				if (Main.dayTime)
				{
					ResetConversion();
					NPC.active = false;
				}

				if (Main.netMode == NetmodeID.SinglePlayer || Main.netMode == NetmodeID.MultiplayerClient)//clientside only
				{
					float distToPlayer = Vector2.Distance(Main.LocalPlayer.Center, NPC.Center);

					if (distToPlayer < StarwaterConversion.MaxItemGlow)//makes the transformable items of nearby players glow
						StarwaterConversion.StarwaterGlobalItemGlow = Math.Max(StarwaterConversion.StarwaterGlobalItemGlow, 1 - distToPlayer / StarwaterConversion.MaxItemGlow);
				}

				#region surface light pillars and surface homing dust
				Vector2 surfaceLightPos = NPC.Center + Vector2.UnitX * Main.rand.NextFloat(-DUST_RANGE, DUST_RANGE) + Vector2.UnitY * Main.rand.NextFloat(-6, 0);
				Tile tile = Framing.GetTileSafely(surfaceLightPos);
				Tile tileDown = Framing.GetTileSafely(surfaceLightPos + Vector2.UnitY * 16);

				if ((tile.LiquidAmount > 0 && tile.LiquidType == LiquidID.Water || tileDown.LiquidAmount > 0 && tileDown.LiquidType == LiquidID.Water) && Main.rand.Next(10) > 3)//surface lights
				{
					//smaller dusts that home in on item if it exists (the dusts do the checking)
					var d = Dust.NewDustPerfect(surfaceLightPos, ModContent.DustType<Dusts.AuroraSuction>(), Vector2.Zero, 200, new Color(Main.rand.NextBool(30) ? 200 : 0, Main.rand.Next(150), 255));
					d.customData = new Dusts.AuroraSuctionData(this, Main.rand.NextFloat(0.6f, 0.8f));

					//vertical light above water
					if (Main.rand.NextBool())
					{
						bool red = Main.rand.NextBool(35);
						bool green = Main.rand.NextBool(15) && !red;
						var color = new Color(red ? 255 : Main.rand.Next(10), green ? 255 : Main.rand.Next(100), Main.rand.Next(240, 255));

						Dust.NewDustPerfect(surfaceLightPos + new Vector2(0, Main.rand.Next(-4, 1)), ModContent.DustType<Dusts.VerticalGlow>(), Vector2.UnitX * Main.rand.NextFloat(-0.15f, 0.15f), 200, color);
					}
				}
				#endregion

				#region circular area of dust
				Vector2 circularLightPos = NPC.Center + Vector2.UnitX.RotatedByRandom(6.28f) * Main.rand.NextFloat(-DUST_RANGE, DUST_RANGE);
				Tile tile2 = Framing.GetTileSafely(circularLightPos);

				if (tile2.LiquidAmount > 0 && tile2.LiquidType == LiquidID.Water && Main.rand.NextBool(2))//under water lights
				{
					var d = Dust.NewDustPerfect(circularLightPos, ModContent.DustType<Dusts.AuroraSuction>(), Vector2.One.RotatedByRandom(6.28f) * Main.rand.NextFloat(), 0, new Color(0, 50, 255), 0.5f);
					d.customData = new Dusts.AuroraSuctionData(this, Main.rand.NextFloat(0.4f, 0.5f));
				}
				#endregion
			}

			if(HasTransformedItem)//if this has already transformed the item
			{
				windDown--;//this timer counts down for some reason

				if(windDown <= 0)
				{
					ResetConversion();
					NPC.active = false;
				}
			}
			else if (targetItem is null)//not transforming, looking for new item
			{
				//could add a delay instead of searching every tick
				for (int k = 0; k < Main.maxItems; k++)//finds nearby items
				{
					Item Item = Main.item[k];
					if (Item.TryGetGlobalItem<TransformableItem>(out TransformableItem GlobalItem))//sometimes this can return null
					{
						//in water, active & not empty, within range
						if (Item.wet && Item.active && !Item.IsAir && Helpers.Helper.CheckCircularCollision(NPC.Center, ITEM_RANGE, Item.Hitbox))
						{
							//if item has a valid type to convert to and isnt being used by another WaterActor
							int ConversionType = StarwaterConversion.GetConversionType(Item);
							if (ConversionType != 0 && GlobalItem.starlightWaterActor == null)
							{
								//it should not be necessary to reset timer and winddown values
								targetItem = Item;
								GlobalItem.starlightWaterActor = this;
								targetItemTransformType = ConversionType;
							}
						}
					}
				}
			}
			else//has valid item
			{
				if(!targetItem.TryGetGlobalItem<TransformableItem>(out TransformableItem GlobalItem) || targetItem.IsAir || !targetItem.active || GlobalItem.starlightWaterActor == null)
				{
					//something has gone wrong and the item either no longer has a valid global item, the item no longer exists, or the item's reference no longer exists
					ResetConversion();
					return;
				}

				if (targetItemTransformType == targetItem.type)//temp safty check
				{
					Main.NewText("Something went wrong with item conversion: item turning into air!", Color.Red);
					ResetConversion();
				}

				if (targetItem.beingGrabbed)
				{
					//delay all actions if this item is in the process of being picked up by the player
					return;
				}

				//if above saftey checks succeed, progress the conversion timer
				transformTimer++;

				Lighting.AddLight(targetItem.Center, new Vector3(10, 13, 25) * 0.04f * transformTimer / TransformTimerLength);

				//when the end of transform timer has been reached
				if (transformTimer > TransformTimerLength)
				{
					if(targetItem.stack > 1)
					{
						targetItem.stack--;
						targetItem.wet = false;
						Item.NewItem(targetItem.GetSource_FromThis(), targetItem.Center, targetItem);
						targetItem.stack = 1;//likely not needed
					}

					GlobalItem.starlightWaterActor = null;//likely not needed, but just in case removes reference to this from old global item instance

					//running this on an item clears it's global item instances
					targetItem.SetDefaults(targetItemTransformType);

					if(targetItem.TryGetGlobalItem<TransformableItem>(out TransformableItem GlobalItem2))
						GlobalItem2.starlightWaterActor = this;//sets ref on new global item instance to this to finish the cooldown

					targetItem.velocity.Y -= 5;
					HasTransformedItem = true;
					windDown = WindDownTimerLength;

					for (int i = 0; i < 40; i++)
						Dust.NewDustPerfect(targetItem.Center, ModContent.DustType<Dusts.BlueStamina>(), Vector2.One.RotatedByRandom(6.28f) * Main.rand.NextFloat(10));

					//for (int k = 0; k < Main.maxPlayers; k++)//unknown use
					//{
					//	Player Player = Main.player[k];
					//}

					//at this point, the npc idles until the timer runs out while the item finishes it's animation, at which point it despawns
				}
			}
		}
	}

	public static class StarwaterConversion
	{
		public static float StarwaterGlobalItemGlow;//brightness for item glow, taken from distance of closetest starwater actor (0 - 1 scale)
		public const float MaxItemGlow = 500f;

		public static Dictionary<int, int> StarlightWaterConversion;//from/to

		//returns 0 if there is no defined type
		public static int GetConversionType(Item item)
		{
			if (item.vanity)
			{
				if (item.headSlot != -1 && item.type != ItemType<AncientStarwoodHat>())
					return ItemType<AncientStarwoodHat>();
				else if (item.bodySlot != -1 && item.type != ItemType<AncientStarwoodChest>())
					return ItemType<AncientStarwoodChest>();
				else if (item.legSlot != -1 && item.type != ItemType<AncientStarwoodBoots>())
					return ItemType<AncientStarwoodBoots>();
			}

			if(StarlightWaterConversion.TryGetValue(item.type, out int conversionType))
			{
				return conversionType;
			}

			return 0;
		}

		private static void ResetInventoryGlow(StarlightPlayer Player)
		{
			//Main.NewText(StarwaterGlobalItemGlow);
			if (StarwaterGlobalItemGlow > 0.075f)
				StarwaterGlobalItemGlow *= 0.985f;//fades out the shine on items
			else
				StarwaterGlobalItemGlow = 0;//rounds to zero as there is a check on the item to save performance
		}

		public static void Load()
		{
			StarlightPlayer.ResetEffectsEvent += ResetInventoryGlow;

			StarlightWaterConversion = new()
			{
				{ ItemID.WoodHelmet, ItemType<StarwoodHat>() },
				{ ItemID.WoodBreastplate, ItemType<StarwoodChest>() },
				{ ItemID.WoodGreaves, ItemType<StarwoodBoots>() },
				{ ItemID.WoodenBoomerang, ItemType<StarwoodBoomerang>() },
				{ ItemID.WandofSparking, ItemType<StarwoodStaff>() },
				{ ItemType<Sling>(), ItemType<StarwoodSlingshot>() },
				{ ItemID.BottledWater, ItemType<StarlightWater>() },
				{ ItemID.LesserManaPotion, ItemType<StarlightWater>() },
			};
		}

		public static void Unload()
		{
			StarlightPlayer.ResetEffectsEvent -= ResetInventoryGlow;
			StarlightWaterConversion = null;
		}
	}

	public class TransformableItem : GlobalItem
	{
		public StarlightWaterActor starlightWaterActor = null;

		public override bool OnPickup(Item item, Player player)//completely stops conversion on pickup since this cant be detected by the WaterActor
		{
			if(starlightWaterActor != null)
			{
				starlightWaterActor.ResetConversion();//fine to be called even if the item has been transformed already
				starlightWaterActor = null;//should be null anyways
				item.wet = false;//for some reason this does not get cleared when the player picks up an item
			}

			return base.OnPickup(item, player);
		}

		public override bool InstancePerEntity => true;

		public override GlobalItem Clone(Item item, Item itemClone)
		{
			return item.TryGetGlobalItem<TransformableItem>(out TransformableItem gi) ? gi : base.Clone(item, itemClone);
		}

		public override bool PreDrawInInventory(Item Item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color ItemColor, Vector2 origin, float scale)
		{
			if (StarwaterConversion.StarwaterGlobalItemGlow != 0 && StarwaterConversion.GetConversionType(Item) != 0)
			{
				RasterizerState RasterizerCullMode = spriteBatch.GraphicsDevice.RasterizerState;
				SamplerState SamplerMode = spriteBatch.GraphicsDevice.SamplerStates[0];

				spriteBatch.End();
				spriteBatch.Begin(default, BlendState.Additive, SamplerMode, default, RasterizerCullMode, default, Main.UIScaleMatrix);

				Texture2D tex = ModContent.Request<Texture2D>("StarlightRiver/Assets/Keys/GlowSoft").Value;
				spriteBatch.Draw(tex, position, null, new Color(130, 200, 255) * (StarwaterConversion.StarwaterGlobalItemGlow + (float)Math.Sin(StarlightWorld.visualTimer) * 0.2f), 0, tex.Size() / 2, 1, 0, 0);

				spriteBatch.End();
				spriteBatch.Begin(default, default, SamplerMode, default, RasterizerCullMode, default, Main.UIScaleMatrix);
			}

			return base.PreDrawInInventory(Item, spriteBatch, position, frame, drawColor, ItemColor, origin, scale);
		}

		public override void PostUpdate(Item Item)
		{
			if(starlightWaterActor != null)//makes item float upwards
			{
				if (starlightWaterActor.transformTimer > 0)
				{
					Item.velocity *= 0.8f;

					if (Item.wet)
						Item.velocity.Y -= 0.15f;
				}

				if (starlightWaterActor.windDown > 0)
				{
					var d = Dust.NewDustPerfect(Item.Center + Vector2.One.RotatedByRandom(6.28f) * 16 * starlightWaterActor.windDown / 240f, ModContent.DustType<Dusts.Aurora>(), Vector2.UnitY * Main.rand.NextFloat(-2, -4), 0, new Color(0, Main.rand.Next(255), 255), 1);
					d.customData = Main.rand.NextFloat(0.2f, 0.3f) * starlightWaterActor.windDown / 240f;

					Lighting.AddLight(Item.Center, new Vector3(10, 13, 25) * 0.08f * starlightWaterActor.windDown / 240f);

					if (Item.velocity.Y > 0)
						Item.velocity.Y *= 0.7f;
				}
			}

			//Might move this later? idk. Kind of 2 things in 1 class but eh
			if (Item.type == ItemID.FallenStar && Item.wet)
			{
				Item.active = false;
				NPC.NewNPC(null, (int)Item.Center.X, (int)Item.Center.Y + 16, ModContent.NPCType<StarlightWaterActor>());

				for (int k = 0; k < 40; k++)
				{
					float rot = Main.rand.NextFloat(6.28f);
					Dust.NewDustPerfect(Item.Center + Vector2.One.RotatedBy(rot) * 16, ModContent.DustType<Dusts.BlueStamina>(), Vector2.One.RotatedBy(rot) * Main.rand.NextFloat(5));
				}
			}
		}

		public override bool PreDrawInWorld(Item Item, SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
		{
			if (starlightWaterActor != null)
			{
				if (starlightWaterActor.transformTimer > 0)
				{
					RasterizerState RasterizerCullMode = spriteBatch.GraphicsDevice.RasterizerState;
					SamplerState SamplerMode = spriteBatch.GraphicsDevice.SamplerStates[0];

					spriteBatch.End();
					spriteBatch.Begin(default, BlendState.Additive, SamplerMode, default, RasterizerCullMode, default, Main.GameViewMatrix.TransformationMatrix);

					Texture2D tex = ModContent.Request<Texture2D>("StarlightRiver/Assets/Tiles/Moonstone/GlowSmall").Value;

					float alphaMaster = (float)Math.Sin(starlightWaterActor.transformTimer / 300f * 3.14f);

					float alpha = (1.0f + (float)Math.Sin(starlightWaterActor.transformTimer / 75f * 3.14f) * 0.5f) * alphaMaster;
					spriteBatch.Draw(tex, Item.Center + Vector2.UnitX * 20 - Main.screenPosition, null, new Color(100, 100 + (int)(50 * alpha), 255) * alpha, 0, new Vector2(tex.Width / 2, tex.Height - 15), 4.5f * alphaMaster, 0, 0);

					float alpha2 = (1.0f + (float)Math.Sin(starlightWaterActor.transformTimer / 150f * 3.14f) * 0.5f) * alphaMaster;
					spriteBatch.Draw(tex, Item.Center - Main.screenPosition, null, new Color(100, 70 + (int)(50 * alpha2), 255) * alpha2, 0, new Vector2(tex.Width / 2, tex.Height - 15), 5 * alphaMaster, 0, 0);

					float alpha3 = (1.0f + (float)Math.Sin(starlightWaterActor.transformTimer / 37.5f * 3.14f) * 0.5f) * alphaMaster;
					spriteBatch.Draw(tex, Item.Center + Vector2.UnitX * -20 - Main.screenPosition, null, new Color(100, 30 + (int)(50 * alpha3), 255) * alpha3, 0, new Vector2(tex.Width / 2, tex.Height - 15), 3f * alphaMaster, 0, 0);

					float rot = Main.rand.NextFloat(6.28f);
					Dust.NewDustPerfect(Item.Center + Vector2.One.RotatedBy(rot) * 16, ModContent.DustType<Dusts.BlueStamina>(), Vector2.One.RotatedBy(rot) * -1.2f);

					var d = Dust.NewDustPerfect(Item.Center + Vector2.One.RotatedBy(rot) * (16 + 8 * alphaMaster), ModContent.DustType<Dusts.Aurora>(), Vector2.UnitY * Main.rand.NextFloat(-9, -6), 0, new Color(0, Main.rand.Next(255), 255), 1);
					d.customData = Main.rand.NextFloat(0.2f, 0.5f) * alphaMaster;

					spriteBatch.End();
					spriteBatch.Begin(default, default, SamplerMode, default, RasterizerCullMode, default, Main.GameViewMatrix.TransformationMatrix);
				}

				if (starlightWaterActor.windDown > 0)
				{
					RasterizerState RasterizerCullMode = spriteBatch.GraphicsDevice.RasterizerState;
					SamplerState SamplerMode = spriteBatch.GraphicsDevice.SamplerStates[0];

					spriteBatch.End();
					spriteBatch.Begin(default, BlendState.Additive, SamplerMode, default, RasterizerCullMode, default, Main.GameViewMatrix.TransformationMatrix);

					Texture2D tex = ModContent.Request<Texture2D>("StarlightRiver/Assets/Keys/GlowSoft").Value;
					spriteBatch.Draw(tex, Item.Center - Main.screenPosition, null, new Color(100, 150, 255) * (starlightWaterActor.windDown / 240f), 0, tex.Size() / 2, starlightWaterActor.windDown / 240f * 2, 0, 0);

					spriteBatch.End();
					spriteBatch.Begin(default, default, SamplerMode, default, RasterizerCullMode, default, Main.GameViewMatrix.TransformationMatrix);
				}
			}

			return base.PreDrawInWorld(Item, spriteBatch, lightColor, alphaColor, ref rotation, ref scale, whoAmI);
		}
	}
}