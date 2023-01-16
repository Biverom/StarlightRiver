﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarlightRiver.Content.Abilities;
using StarlightRiver.Core;
using StarlightRiver.Content.Items.Gravedigger;
using StarlightRiver.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;
using Terraria.GameContent;

namespace StarlightRiver.Content.Items.Lightsaber
{
	public class LightsaberProj_Yellow : LightsaberProj
	{
		protected override Vector3 BladeColor => Color.Yellow.ToVector3() * 0.8f * fade;

		private bool dashing = false;

		private bool caughtUp = false;

		private float fade = 1f;

        protected override void RightClickBehavior()
        {
			hide = true;
			canHit = false;
			Projectile.active = false;
        }

        protected override void SafeLeftClickBehavior()
        {
			if (!thrown)
				return;

			if (Main.mouseRight && !dashing)
			{
				dashing = true;
				Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<YellowLightsaberDashProjectile>(), Projectile.damage * 2, 0, Owner.whoAmI);
				Owner.GetModPlayer<LightsaberPlayer>().dashing = true;
			}

			if (dashing)
            {
				Projectile.velocity = Vector2.Zero;
				if (Owner.Distance(Projectile.Center) < 80 || !Owner.GetModPlayer<LightsaberPlayer>().dashing && !caughtUp)
                {
					Owner.Center = Projectile.Center;
					Owner.velocity = Vector2.Zero;
					Owner.GetModPlayer<LightsaberPlayer>().dashing = false;
					Projectile.active = true;
					caughtUp = true;
				}

				if (caughtUp)
				{
					Projectile.active = true;
					fade -= 0.01f;
					if (fade <= 0)
						Projectile.active = false;
				}
				else
				{
					Owner.velocity = Owner.DirectionTo(Projectile.Center) * 60;
					Dust dust = Dust.NewDustPerfect(Owner.Center + Main.rand.NextVector2Circular(45, 45) + Owner.velocity, ModContent.DustType<Dusts.GlowLine>(), Owner.DirectionTo(Projectile.Center) * Main.rand.NextFloat(2), 0, new Color(BladeColor.X, BladeColor.Y, BladeColor.Z), Main.rand.NextFloat(1f, 1.5f));
					dust.fadeIn = 20;

					Dust.NewDustPerfect(Owner.Center + Owner.velocity + Main.rand.NextVector2Circular(30, 30), ModContent.DustType<LightsaberGlow>(), Vector2.Normalize(Owner.velocity).RotatedBy(Main.rand.NextFloat(2.5f, 3f)) * Main.rand.NextFloat(3), 0, new Color(BladeColor.X, BladeColor.Y, BladeColor.Z), Main.rand.NextFloat(0.5f, 0.85f));
					Dust.NewDustPerfect(Owner.Center + Owner.velocity + Main.rand.NextVector2Circular(30, 30), ModContent.DustType<LightsaberGlow>(), Vector2.Normalize(Owner.velocity).RotatedBy(-Main.rand.NextFloat(2.5f, 3f)) * Main.rand.NextFloat(3), 0, new Color(BladeColor.X, BladeColor.Y, BladeColor.Z), Main.rand.NextFloat(0.5f, 0.85f));
				}
			}
        }
    }
}