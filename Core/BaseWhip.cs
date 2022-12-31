﻿using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria.Audio;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.ID;

namespace StarlightRiver.Core
{
	public abstract class BaseWhip : ModProjectile
	{
		protected int xFrames = 1;
		protected int yFrames = 5;
		protected int xFrame = 0;

		protected string name;
		protected int segments;
		protected float rangeMult;
		protected float flyTime;
		protected Color stringColor;
		protected int handleOffset;

		public BaseWhip(string name, int segments = 20, float rangeMultiplier = 1f, Color? stringColor = null, int handleOffset = 2)
		{
			this.name = name;
			this.segments = Math.Max(3, segments);
			rangeMult = rangeMultiplier;

			if (stringColor != null)
				this.stringColor = stringColor.Value;
			else
				this.stringColor = Color.White;

			this.handleOffset = handleOffset;
		}

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault(name);
			ProjectileID.Sets.IsAWhip[Type] = true;
		}

		public override void SetDefaults()
		{
			Projectile.DefaultToWhip();
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			return false;
		}

		public override bool PreAI()
		{
			Player player = Main.player[Projectile.owner];
			flyTime = player.itemAnimationMax * Projectile.MaxUpdates;
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
			Projectile.ai[0]++;
			Projectile.Center = Main.GetPlayerArmPosition(Projectile) + Projectile.velocity * (Projectile.ai[0] - 1f);
			Projectile.spriteDirection = (!(Vector2.Dot(Projectile.velocity, Vector2.UnitX) < 0f)) ? 1 : -1;

			if (Projectile.ai[0] >= flyTime || player.itemAnimation == 0)
			{
				Projectile.Kill();
				return false;
			}

			player.heldProj = Projectile.whoAmI;
			player.itemAnimation = player.itemAnimationMax - (int)(Projectile.ai[0] / Projectile.MaxUpdates);
			player.itemTime = player.itemAnimation;

			if (Projectile.ai[0] == (int)(flyTime / 2f))
			{
				Projectile.WhipPointsForCollision.Clear();
				SetPoints(Projectile.WhipPointsForCollision);
				Vector2 position = Projectile.WhipPointsForCollision[^1];
				SoundEngine.PlaySound(SoundID.Item153, position);
			}

			if (Utils.GetLerpValue(0.1f, 0.7f, Projectile.ai[0] / flyTime, true) * Utils.GetLerpValue(0.9f, 0.7f, Projectile.ai[0] / flyTime, true) > 0.5f)
			{
				Projectile.WhipPointsForCollision.Clear();
				SetPoints(Projectile.WhipPointsForCollision);
			}

			ArcAI();
			return false;
		}

		public float MiddleOfArc => flyTime / 1.5f;

		public Vector2 EndPoint => Projectile.WhipPointsForCollision[segments - 1] + new Vector2(Projectile.width * 0.5f, Projectile.height * 0.5f);

		public virtual void ArcAI() { }

		public override void CutTiles()
		{
			var value = new Vector2(Projectile.width * Projectile.scale * 0.5f, 0f);

			for (int i = 0; i < Projectile.WhipPointsForCollision.Count; i++)
			{
				DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
				Utils.PlotTileLine(Projectile.WhipPointsForCollision[i] - value, Projectile.WhipPointsForCollision[i] + value, Projectile.height * Projectile.scale, DelegateMethods.CutTiles);
			}
		}

		public void SetPoints(List<Vector2> controlPoints)
		{
			float time = Projectile.ai[0] / flyTime;
			float timeModified = time * 1.5f;
			float segmentOffset = MathHelper.Pi * 10f * (1f - timeModified) * -Projectile.spriteDirection / segments;
			float tLerp = 0f;

			if (timeModified > 1f)
			{
				tLerp = (timeModified - 1f) / 0.5f;
				timeModified = MathHelper.Lerp(1f, 0f, tLerp);
			}

			//vanilla code
			Player player = Main.player[Projectile.owner];
			Item heldItem = player.HeldItem;
			float realRange = ContentSamples.ItemsByType[heldItem.type].useAnimation * 2 * time * player.whipRangeMultiplier;
			float num8 = Projectile.velocity.Length() * realRange * timeModified * rangeMult / segments;
			Vector2 playerArmPosition = Main.GetPlayerArmPosition(Projectile);
			Vector2 firstPos = playerArmPosition;
			float num10 = 0f - MathHelper.PiOver2;
			Vector2 midPos = firstPos;
			float num11 = 0f + MathHelper.PiOver2 + MathHelper.PiOver2 * Projectile.spriteDirection;
			Vector2 lastPos = firstPos;
			float num12 = 0f + MathHelper.PiOver2;
			controlPoints.Add(playerArmPosition);

			for (int i = 0; i < segments; i++)
			{
				float num14 = segmentOffset * (i / (float)segments);
				Vector2 nextFirst = firstPos + num10.ToRotationVector2() * num8;
				Vector2 nextLast = lastPos + num12.ToRotationVector2() * (num8 * 2f);
				Vector2 nextMid = midPos + num11.ToRotationVector2() * (num8 * 2f);
				float num15 = 1f - timeModified;
				float num16 = 1f - num15 * num15;
				var value3 = Vector2.Lerp(nextLast, nextFirst, num16 * 0.9f + 0.1f);
				var value4 = Vector2.Lerp(nextMid, value3, num16 * 0.7f + 0.3f);
				Vector2 spinningpoint = playerArmPosition + (value4 - playerArmPosition) * new Vector2(1f, 1.5f);
				float num17 = tLerp;
				num17 *= num17;
				Vector2 item = spinningpoint.RotatedBy(Projectile.rotation + 4.712389f * num17 * Projectile.spriteDirection, playerArmPosition);
				controlPoints.Add(item);
				num10 += num14;
				num12 += num14;
				num11 += num14;
				firstPos = nextFirst;
				lastPos = nextLast;
				midPos = nextMid;
			}
		}

		public virtual void DrawBehindWhip(ref Color lightColor) { }

		public override bool PreDraw(ref Color lightColor)
		{
			DrawBehindWhip(ref lightColor);
			var points = new List<Vector2>();
			points.Clear();
			SetPoints(points);

			//string
			Vector2 stringPoint = points[0];

			for (int i = 0; i < points.Count - 2; i++)
			{
				Vector2 nextPoint = points[i + 1] - points[i];
				Color color = stringColor.MultiplyRGBA(Projectile.GetAlpha(Lighting.GetColor(points[i].ToTileCoordinates())));
				var scale = new Vector2(1f, (nextPoint.Length() + 2f) / TextureAssets.FishingLine.Height());
				Main.EntitySpriteDraw(TextureAssets.FishingLine.Value, stringPoint - Main.screenPosition, null, color, nextPoint.ToRotation() - MathHelper.PiOver2, new Vector2(TextureAssets.FishingLine.Width() * 0.5f, 2f), scale, SpriteEffects.None, 0);
				stringPoint += nextPoint;
			}

			//whip
			Asset<Texture2D> texture = ModContent.Request<Texture2D>(Texture);
			Rectangle whipFrame = texture.Frame(xFrames, yFrames, xFrame, 0);
			int height = whipFrame.Height;
			Vector2 firstPoint = points[0];

			for (int i = 0; i < points.Count - 1; i++)
			{
				Vector2 origin = whipFrame.Size() * 0.5f;
				bool draw = true;

				if (i == 0)
				{
					origin.Y += handleOffset;
				}
				else if (i == points.Count - 2)
				{
					whipFrame.Y = height * (yFrames - 1);
				}
				else
				{
					whipFrame.Y = height * SegmentVariant(i);
					draw = ShouldDrawSegment(i);
				}

				Vector2 difference = points[i + 1] - points[i];

				if (draw)
				{
					Color alpha = Projectile.GetAlpha(Lighting.GetColor(points[i].ToTileCoordinates()));
					float rotation = difference.ToRotation() - MathHelper.PiOver2;
					Main.EntitySpriteDraw(texture.Value, points[i] - Main.screenPosition, whipFrame, alpha, rotation, origin, Projectile.scale, SpriteEffects.None, 0);
				}

				firstPoint += difference;
			}

			return false;
		}

		public virtual int SegmentVariant(int segment)
		{
			return 1 + segment % 3;
		}

		public virtual bool ShouldDrawSegment(int segment)
		{
			return segment % 2 == 0;
		}
	}
}
