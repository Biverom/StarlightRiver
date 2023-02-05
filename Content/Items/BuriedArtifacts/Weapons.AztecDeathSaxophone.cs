﻿using StarlightRiver.Core.Systems.CameraSystem;
using StarlightRiver.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.DataStructures;
using Terraria.ID;

namespace StarlightRiver.Content.Items.BuriedArtifacts
{
	public class AztecDeathSaxophone : ModItem
	{
		public const int MAX_CHARGE = 5;

		public int charge;

		public override string Texture => AssetDirectory.ArtifactItem + Name;

		public override void Load()
		{
			StarlightPlayer.OnHitByNPCEvent += StarlightPlayer_OnHitByNPCEvent;
			StarlightPlayer.OnHitByProjectileEvent += StarlightPlayer_OnHitByProjectileEvent;

			StarlightPlayer.OnHitNPCEvent += StarlightPlayer_OnHitNPCEvent;
			StarlightPlayer.OnHitNPCWithProjEvent += StarlightPlayer_OnHitNPCWithProjEvent;
		}

		private void StarlightPlayer_OnHitNPCWithProjEvent(Player player, Projectile proj, NPC target, int damage, float knockback, bool crit)
		{
			HitEffects(player, target);
		}

		private void StarlightPlayer_OnHitNPCEvent(Player player, Item Item, NPC target, int damage, float knockback, bool crit)
		{
			HitEffects(player, target);
		}

		private void StarlightPlayer_OnHitByProjectileEvent(Player player, Projectile projectile, int damage, bool crit)
		{
			HurtEffects(player, damage);
		}

		private void StarlightPlayer_OnHitByNPCEvent(Player player, NPC npc, int damage, bool crit)
		{
			HurtEffects(player, damage);
		}

		public void HitEffects(Player Player, NPC target)
		{
			if (target.life <= 0 && charge < MAX_CHARGE)
				IncreaseCharge(Player);
		}

		public void HurtEffects(Player Player, int damage)
		{
			bool valid = damage >= 10;

			if (charge < MAX_CHARGE && valid)
				IncreaseCharge(Player);
		}

		public void IncreaseCharge(Player Player)
		{
			for (int i = 0; i < Player.inventory.Length; i++)
			{
				Item item = Player.inventory[i];

				if (item.ModItem is AztecDeathSaxophone sax)
					sax.charge++;
			}
		}

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Aztec Death Saxophone"); //TODO: better name?
			Tooltip.SetDefault("Take damage and kill foes to charge the saxophone\nOnce charged, press <left> to play a violent tune, massacring anything caught in the way"); //TODO: make this not a skippzz tooltip :)
		}

		public override void SetDefaults()
		{
			Item.damage = 30;
			Item.DamageType = DamageClass.Magic;
			Item.knockBack = 3f;
			Item.useTime = 75;
			Item.useAnimation = 75;

			Item.value = Item.sellPrice(gold: 3);
			Item.rare = ItemRarityID.Green;

			Item.noUseGraphic = true;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.noMelee = true;
			Item.width = 32;
			Item.height = 32;

			Item.shoot = ModContent.ProjectileType<AztecDeathSaxophoneHoldout>();
			Item.shootSpeed = 1f;
		}

		public override bool CanUseItem(Player player)
		{
			return player.ownedProjectileCounts[ModContent.ProjectileType<AztecDeathSaxophoneHoldout>()] <= 0;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
		{
			charge = 0;
			return base.Shoot(player, source, position, velocity, type, damage, knockback);
		}

		public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
		{
			Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;

			spriteBatch.Draw(tex, position, frame, drawColor, 0f, origin, scale, 0f, 0f);

			return false;
		}
	}

	class AztecDeathSaxophoneHoldout : ModProjectile
	{
		public int originalTimeleft;
		private Player Owner => Main.player[Projectile.owner];
		public override string Texture => AssetDirectory.ArtifactItem + Name;

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Aztec Death Saxophone");
		}

		public override void SetDefaults()
		{
			Projectile.width = 36;
			Projectile.height = 32;

			Projectile.tileCollide = false;
			Projectile.friendly = true;
			Projectile.ignoreWater = true;
			Projectile.DamageType = DamageClass.Magic;
		}

		public override bool? CanDamage()
		{
			return false;
		}

		public override void OnSpawn(IEntitySource source)
		{
			Projectile.timeLeft = Owner.itemTime;
			originalTimeleft = Projectile.timeLeft;
		}

		public override void AI()
		{
			Vector2 position = Owner.RotatedRelativePoint(Owner.MountedCenter, true);
			position += Vector2.Normalize(Owner.direction * Vector2.UnitX) * 15f;

			Projectile.position = position - Projectile.Size * 0.5f;

			Projectile.spriteDirection = Owner.direction;
			if (Main.myPlayer == Owner.whoAmI)
				Owner.ChangeDir(Main.MouseWorld.X < Owner.Center.X ? -1 : 1);

			Owner.heldProj = Projectile.whoAmI;

			Owner.itemTime = 2;
			Owner.itemAnimation = 2;

			Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, (MathHelper.PiOver2 + MathHelper.PiOver4) * -Owner.direction);


			if (Projectile.timeLeft > (int)(originalTimeleft * 0.65f))
			{
				float lerper = MathHelper.Lerp(35f, 1f, 1f - (Projectile.timeLeft - (int)(originalTimeleft * 0.65f)) / (float)(originalTimeleft * 0.35f));
				Vector2 offset = Main.rand.NextVector2CircularEdge(lerper, lerper);
				Vector2 pos = Owner.Center + new Vector2(30f * Owner.direction, 10f) + (Owner.direction == -1 ? Vector2.UnitX * 3f : Vector2.Zero);
				Dust.NewDustPerfect(pos + offset, ModContent.DustType<Dusts.GlowFastDecelerate>(), (pos + offset).DirectionTo(pos), 0, new Color(255, 0, 0), 0.55f);

				Dust.NewDustPerfect(pos + offset, ModContent.DustType<Dusts.GlowLineFast>(), (pos + offset).DirectionTo(pos) * 3f, 0, new Color(255, 0, 0), 1f);
			}
			else
			{
				if (Projectile.timeLeft % ((int)(originalTimeleft * 0.65f) / 5) == 0)
				{
					CameraSystem.shake += 7;

					Vector2 pos = Owner.Center + new Vector2(30f * Owner.direction, 10f) + (Owner.direction == -1 ? Vector2.UnitX * 3f : Vector2.Zero);

					Projectile.NewProjectileDirect(Projectile.GetSource_FromAI(), pos, Vector2.Zero, ModContent.ProjectileType<AztecDeathSaxophoneSoundwave>(), Projectile.damage, Projectile.knockBack, Projectile.owner).rotation = Main.rand.NextFloat(6.28f);
				}
			}
		}
	}

	class AztecDeathSaxophoneSoundwave : ModProjectile
	{
		public float Radius => 100 * Projectile.scale;
		public override string Texture => AssetDirectory.ArtifactItem + Name;

		public override void SetDefaults()
		{
			Projectile.DamageType = DamageClass.Magic;
			Projectile.friendly = true;

			Projectile.width = 50;
			Projectile.height = 50;
			Projectile.tileCollide = false;

			Projectile.timeLeft = 45;

			Projectile.penetrate = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
		}

		public override void AI()
		{
			Projectile.scale += 0.35f;



		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			return Helper.CheckCircularCollision(Projectile.Center, (int)Radius, targetHitbox);
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
			Color color = new Color(255, 0, 0, 0) * 0.45f;
			if (Projectile.timeLeft < 10)
				color *= Projectile.timeLeft / 10f;

			Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, color, Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None, 0f);
			return false;
		}
	}
}
