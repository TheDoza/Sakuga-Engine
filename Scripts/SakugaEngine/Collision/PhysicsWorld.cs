using Godot;
using SakugaEngine.Global;
using SakugaEngine.Resources;
using System.Collections.Generic;

namespace SakugaEngine.Collision
{
    public class PhysicsWorld
    {
        private List<PhysicsBody> bodies;
        private List<HitQuery> hitQueries;
            
        public int BodyCount => bodies.Count;
        private uint CreatedBodies;

        private int MinSteps = 1;
        private int MaxSteps = 64;
        private int Steps;
        
        public PhysicsWorld()
        {
            bodies = new List<PhysicsBody>();
            hitQueries = new List<HitQuery>();
            CreatedBodies = 0;
            Steps = Mathf.Clamp(GlobalVariables.SubSteps, MinSteps, MaxSteps);
        }
        
        public void AddBody(PhysicsBody newBody)
        {
            newBody.SetID(CreatedBodies);
            CreatedBodies++;
            bodies.Add(newBody);
        }
        
        public bool RemoveBody(PhysicsBody body)
        {
            return bodies.Remove(body);
        }

        public bool GetBody(int index, out PhysicsBody body)
        {
            body = null;

            if(index < 0 || index >= bodies.Count)
            {
                return false;
            }

            body = bodies[index];
            return true;
        }

        public void Simulate()
        {
            hitQueries.Clear();

            for (int st = 0; st < Steps; st++)
            {
                for (int i = 0; i < bodies.Count; i++)
                    bodies[i].Move();
                
                for (int i = 0; i < bodies.Count; i++)
                {
                    PhysicsBody bodyA = bodies[i];
                    for (int j = i + 1; j < bodies.Count; j++)
                    {
                        PhysicsBody bodyB = bodies[j];
                        
                        if (bodyA.IsStatic && bodyB.IsStatic) continue;
                        if (bodyA.IgnorePushbox || bodyB.IgnorePushbox) continue; // [FF PATCH: tech] wakeup roll passes through
                        if (ResolveCorner(bodyA, bodyB)) continue; // [FF PATCH: pushbox] no jumping over a cornered fighter
                        
                        if (bodyA.Pushbox.IsOverlapping(bodyB.Pushbox))
                        {
                            Vector2I depth = GetDepth(bodyA.Pushbox, bodyB.Pushbox);
                            Vector2I normal = Vector2I.Zero;
                            
                            if (bodyA.FixedPosition.X > bodyB.FixedPosition.X)
                                normal.X = -1;
                            else if (bodyA.FixedPosition.X < bodyB.FixedPosition.X)
                                normal.X = 1;
                            else
                                normal.X = bodyA.IsLeftSide ? 1 : -1;

                            if (bodyA.IsStatic)
                                bodyB.Resolve(-normal * depth);
                            else if (bodyB.IsStatic)
                                bodyA.Resolve(normal * depth);
                            else
                            {
                                if (Unpushable(bodyA, bodyB))
                                    bodyA.Resolve(normal * depth);
                                else
                                    bodyA.Resolve(normal * (depth / 2));

                                if (Unpushable(bodyB, bodyA))
                                    bodyB.Resolve(-normal * depth);
                                else
                                    bodyB.Resolve(-normal * (depth / 2));
                            }
                        }
                    }
                }
            }
            
            //Setup for proximity blocking
            for (int i = 0; i < bodies.Count; i++)
                bodies[i].ProximityBlocked = false;

            for (int i = 0; i < bodies.Count; i++)
            {
                PhysicsBody bodyA = bodies[i];
                for (int j = i + 1; j < bodies.Count; j++)
                {
                    PhysicsBody bodyB = bodies[j];

                    HitboxesCheck(bodyA, bodyB);
                }
            }

            //Actually check the hits
            HitQueries();

            //Check proximity blocking exit
            for (int i = 0; i < bodies.Count; i++)
                if (!bodies[i].ProximityBlocked)
                    bodies[i].Parent.OnHitboxExit();
        }

        private void HitboxesCheck(PhysicsBody bodyA, PhysicsBody bodyB)
		{
            HitQuery currentHit = new HitQuery()
            {
                p1 = bodyA.Parent,
                p2 = bodyB.Parent,
                p1HitType = -1,
                p2HitType = -1
            };

			for (int i = 0; i < bodyA.Hitboxes.Length; i++)
			{	
				Collider hitboxA = bodyA.Hitboxes[i];

				for (int j = 0; j < bodyB.Hitboxes.Length; j++)
				{
					Collider hitboxB = bodyB.Hitboxes[j];

					if (hitboxA.IsOverlapping(hitboxB))
					{
                        //Get current hitbox settings to reference the correct hitbox element
						HitboxElement boxSettingsA = bodyA.CurrentHitbox.HitboxData.Hitboxes[i];
						HitboxElement boxSettingsB = bodyB.CurrentHitbox.HitboxData.Hitboxes[j];

						HitboxType myType = boxSettingsA.HitboxType;
						HitboxType otType = boxSettingsB.HitboxType;

                        if (myType == 0 && otType == 0) continue;

                        Vector2I ContactPoint = GetContactPoint(hitboxA, hitboxB);

                        //Basic damage
						if (myType == HitboxType.HITBOX && otType == HitboxType.HURTBOX)
                        {
                            if (!bodyA.CanHitBody(bodyB)) return;
                            
                            if (currentHit.p1HitType < 0)
                            {
                                currentHit.p1HitType = 0;
                                currentHit.p1Hitbox = boxSettingsA;
                                currentHit.p1ContactPoint = ContactPoint;
                            }
						}
                        else if (otType == HitboxType.HITBOX && myType == HitboxType.HURTBOX)
                        {
                            if (!bodyB.CanHitBody(bodyA)) return;

                            if (currentHit.p2HitType < 0)
                            {
                                currentHit.p2HitType = 0;
                                currentHit.p2Hitbox = boxSettingsB;
                                currentHit.p2ContactPoint = ContactPoint;
                            }
                        }
                        
                        //Hitboxes clash
						if (myType == HitboxType.HITBOX && otType == HitboxType.HITBOX)
                        {
                            if (!bodyA.CanHitBody(bodyB) || !bodyB.CanHitBody(bodyA)) return;

							if (boxSettingsA.Priority != boxSettingsB.Priority) return;

                            if (currentHit.p1HitType < 0)
                            {
                                currentHit.p1HitType = 11;
                                currentHit.p1Hitbox = boxSettingsA;
                                currentHit.p1ContactPoint = ContactPoint;
                            }
                            if (currentHit.p2HitType < 0)
                            {
                                currentHit.p2HitType = 11;
                                currentHit.p2Hitbox = boxSettingsB;
                                currentHit.p2ContactPoint = ContactPoint;
                            }
						}

                        //Projectile damage
                        if (myType == HitboxType.PROJECTILE && otType == HitboxType.HURTBOX)
                        {
                            if (!bodyA.CanHitBody(bodyB)) return;

                            if (currentHit.p1HitType < 0)
                            {
                                currentHit.p1HitType = 0;
                                currentHit.p1Hitbox = boxSettingsA;
                                currentHit.p1ContactPoint = ContactPoint;
                            }
                        }
                        else if (otType == HitboxType.PROJECTILE && myType == HitboxType.HURTBOX)
                        {
                            if (!bodyB.CanHitBody(bodyA)) return;
                            
                            if (currentHit.p2HitType < 0)
                            {
                                currentHit.p2HitType = 0;
                                currentHit.p2Hitbox = boxSettingsB;
                                currentHit.p2ContactPoint = ContactPoint;
                            }
                        }

                        //Projectile clash
                        if (myType == HitboxType.PROJECTILE && otType == HitboxType.PROJECTILE)
                        {
                            if (!bodyA.CanHitBody(bodyB) || !bodyB.CanHitBody(bodyA)) return;

                            if (currentHit.p1HitType < 0)
                            {
                                currentHit.p1HitType = 12;
                                currentHit.p1Hitbox = boxSettingsA;
                                currentHit.p1ContactPoint = ContactPoint;
                            }
                            if (currentHit.p2HitType < 0)
                            {
                                currentHit.p2HitType = 12;
                                currentHit.p2Hitbox = boxSettingsB;
                                currentHit.p2ContactPoint = ContactPoint;
                            }
                        }

                        //Deflect projectiles
                        if (myType == HitboxType.PROJECTILE && otType == HitboxType.DEFLECT)
                        {
                            if (!bodyA.CanHitBody(bodyB)) return;

                            if (currentHit.p1HitType < 0)
                            {
                                currentHit.p1HitType = 10;
                                currentHit.p1Hitbox = boxSettingsA;
                                currentHit.p1ContactPoint = ContactPoint;
                            }
                        }
                        else if (otType == HitboxType.PROJECTILE && myType == HitboxType.DEFLECT)
                        {
                            if (!bodyB.CanHitBody(bodyA)) return;
                            
                            if (currentHit.p2HitType < 0)
                            {
                                currentHit.p2HitType = 10;
                                currentHit.p2Hitbox = boxSettingsB;
                                currentHit.p2ContactPoint = ContactPoint;
                            }
                        }
                        
                        //Proximity block
                        if (myType == HitboxType.PROXIMITY_BLOCK && otType == HitboxType.HURTBOX)
                        {
                            if (!bodyA.CanHitBody(bodyB)) return;

                            bodyB.Parent.ProximityBlock(boxSettingsA);
                        }
                        else if (otType == HitboxType.PROXIMITY_BLOCK && myType == HitboxType.HURTBOX)
                        {
                            if (!bodyB.CanHitBody(bodyA)) return;

                            bodyA.Parent.ProximityBlock(boxSettingsB);
                        }

						//Throws
                        if (myType == HitboxType.THROW && otType == HitboxType.HURTBOX)
                        {
                            if (!bodyA.CanHitBody(bodyB)) return;

                            if (currentHit.p1HitType < 0)
                            {
                                currentHit.p1HitType = 2;
                                currentHit.p1Hitbox = boxSettingsA;
                                currentHit.p1ContactPoint = ContactPoint;
                            }
                        }
                        else if (otType == HitboxType.THROW && myType == HitboxType.HURTBOX)
                        {
                            if (!bodyB.CanHitBody(bodyA)) return;

                            if (currentHit.p2HitType < 0)
                            {
                                currentHit.p2HitType = 2;
                                currentHit.p2Hitbox = boxSettingsB;
                                currentHit.p2ContactPoint = ContactPoint;
                            }
                        }

                        //Counterattack (Needs some testing)
                        if (myType == HitboxType.COUNTER && otType == HitboxType.HITBOX)
                        {
                            if (!bodyA.CanHitBody(bodyB)) return;

                            if (currentHit.p1HitType < 0)
                            {
                                currentHit.p1HitType = 3;
                                currentHit.p1Hitbox = boxSettingsA;
                                currentHit.p1ContactPoint = ContactPoint;
                            }
                        }
                        else if (otType == HitboxType.COUNTER && myType == HitboxType.HITBOX)
                        {
                            if (!bodyB.CanHitBody(bodyA)) return;

                            if (currentHit.p2HitType < 0)
                            {
                                currentHit.p2HitType = 3;
                                currentHit.p2Hitbox = boxSettingsB;
                                currentHit.p2ContactPoint = ContactPoint;
                            }
                        }
					}
				}
			}
            hitQueries.Add(currentHit);
		}

        private void HitQueries()
        {
            if (hitQueries == null || hitQueries.Count <= 0) return;
            
            for (int i = 0; i < hitQueries.Count; i++)
            {
                HitQuery currentHit = hitQueries[i];
                if (currentHit.p1HitType < 0 && currentHit.p2HitType < 0) continue;

                SakugaActor actor1 = currentHit.p1 as SakugaActor;
                SakugaActor actor2 = currentHit.p2 as SakugaActor;
                if (!actor1.CanHitTarget(actor2)) continue;
                if (!actor2.CanHitTarget(actor1)) continue;
                
                //Basic damage
                if (currentHit.p1HitType == 0 && currentHit.p2HitType != 0) {
                    currentHit.p1.BaseDamage(currentHit.p2 as SakugaActor, currentHit.p1Hitbox, currentHit.p1ContactPoint);
                }
                else if (currentHit.p1HitType != 0 && currentHit.p2HitType == 0) {
                    currentHit.p2.BaseDamage(currentHit.p1 as SakugaActor, currentHit.p2Hitbox, currentHit.p2ContactPoint);
                }
                else if (currentHit.p1HitType == 0 && currentHit.p2HitType == 0) {
                    //Hit trades
                    currentHit.p1.HitTrade(currentHit.p2 as SakugaActor, currentHit.p2Hitbox, currentHit.p1ContactPoint);
                    currentHit.p2.HitTrade(currentHit.p1 as SakugaActor, currentHit.p1Hitbox, currentHit.p2ContactPoint);
                }
                else if (currentHit.p1HitType == 11 && currentHit.p2HitType == 11) {
                    //Hitboxes clash
                    if (currentHit.p1Hitbox.Priority != currentHit.p2Hitbox.Priority) return;

                    currentHit.p1.HitboxClash(currentHit.p2 as SakugaActor, currentHit.p1Hitbox, currentHit.p1ContactPoint);
                    currentHit.p2.HitboxClash(currentHit.p1 as SakugaActor, currentHit.p2Hitbox, Vector2I.Zero);
                }
                else if (currentHit.p1HitType == 12 && currentHit.p2HitType == 12) {
                    //Projectile clash
                    if (currentHit.p2Hitbox.Priority >= currentHit.p1Hitbox.Priority)
                        currentHit.p1.ProjectileClash(currentHit.p2 as SakugaActor, currentHit.p1Hitbox, currentHit.p1ContactPoint);
                    if (currentHit.p1Hitbox.Priority >= currentHit.p2Hitbox.Priority)
                        currentHit.p2.ProjectileClash(currentHit.p1 as SakugaActor, currentHit.p2Hitbox, currentHit.p2ContactPoint);
                }

                /*//Projectile damage
                if (currentHit.p1HitType == 1 && currentHit.p2HitType == -1) {
                    currentHit.p1.BaseDamage(currentHit.p2 as SakugaActor, currentHit.p1Hitbox, currentHit.contactPoint);
                }
                else if (currentHit.p1HitType == -1 && currentHit.p2HitType == 1) {
                    currentHit.p2.BaseDamage(currentHit.p1 as SakugaActor, currentHit.p2Hitbox, currentHit.contactPoint);
                }*/
                //Deflect projectiles
                if (currentHit.p1HitType == 12 && currentHit.p2HitType == -1) {
                    currentHit.p1.ProjectileDeflect(currentHit.p2 as SakugaActor, currentHit.p1Hitbox, currentHit.p1ContactPoint);
                }
                else if (currentHit.p1HitType == -1 && currentHit.p2HitType == 12) {
                    currentHit.p2.ProjectileDeflect(currentHit.p1 as SakugaActor, currentHit.p2Hitbox, currentHit.p2ContactPoint);
                }

                //Throws
                if (currentHit.p1HitType == 2 && currentHit.p2HitType == -1) {
                    currentHit.p1.ThrowDamage(currentHit.p2 as SakugaActor, currentHit.p1Hitbox, currentHit.p1ContactPoint);
                }
                else if (currentHit.p1HitType == -1 && currentHit.p2HitType == 2) {
                    currentHit.p2.ThrowDamage(currentHit.p1 as SakugaActor, currentHit.p2Hitbox, currentHit.p2ContactPoint);
                }
                else if (currentHit.p1HitType == 2 && currentHit.p2HitType == 2) {
                    //Throw trades
                    currentHit.p1.ThrowTrade();
                    currentHit.p2.ThrowTrade();
                }

                //Counterattack (Needs some testing)
                if (currentHit.p1HitType == 3 && currentHit.p2HitType == -1) {
                    currentHit.p1.CounterHit(currentHit.p2 as SakugaActor, currentHit.p1Hitbox, currentHit.p1ContactPoint);
                }
                else if (currentHit.p1HitType == -1 && currentHit.p2HitType == 3) {
                    currentHit.p2.CounterHit(currentHit.p1 as SakugaActor, currentHit.p2Hitbox, currentHit.p2ContactPoint);
                }
            }
        }

        public Vector2I GetContactPoint(Collider A, Collider B)
        {
            int minPointX = Mathf.Min(A.Center.X + A.Size.X, B.Center.X + B.Size.X);
            int maxPointX = Mathf.Max(A.Center.X - A.Size.X, B.Center.X - B.Size.X);
            int minPointY = Mathf.Min(A.Center.Y + A.Size.Y, B.Center.Y + B.Size.Y);
            int maxPointY = Mathf.Max(A.Center.Y - A.Size.Y, B.Center.Y - B.Size.Y);
            int MedianX = (minPointX + maxPointX) / 2;
            int MedianY = (minPointY + maxPointY) / 2;
            return new Vector2I(MedianX, MedianY);
        }

        public Vector2I GetDepth(Collider A, Collider B)
        {
            Vector2I length = A.Center - B.Center;

            Vector2I Depth = (A.Size + B.Size) / 2;
            Depth.X -= Mathf.Abs(length.X);
            Depth.Y -= Mathf.Abs(length.Y);

            return Depth;
        }

        /// <summary>
        /// [FF PATCH: pushbox] Corner rule. When one fighter is pinned against a wall (there isn't room for the other
        /// fighter's pushbox between it and the wall), its pushbox counts as reaching all the way up: the other fighter
        /// can't jump, super jump, double jump or air dash over it and land behind. It always stays on the open side.
        /// The cornered fighter can still jump out over the other one (see JumpingOut).
        /// Returns true when the rule handled this pair (the normal pushbox check is skipped).
        /// </summary>
        private bool ResolveCorner(PhysicsBody bodyA, PhysicsBody bodyB)
        {
            if (!bodyA.Pushbox.Active || !bodyB.Pushbox.Active) return false;
            if (bodyA.IsStatic || bodyB.IsStatic) return false;

            // Right wall: the fighter further right is the one that could be pinned (a tie goes by side).
            bool aRight = bodyA.FixedPosition.X > bodyB.FixedPosition.X ||
                          (bodyA.FixedPosition.X == bodyB.FixedPosition.X && !bodyA.IsLeftSide);
            PhysicsBody right = aRight ? bodyA : bodyB, left = aRight ? bodyB : bodyA;
            if (right.MaxX - right.FixedPosition.X < left.Pushbox.Size.X && !JumpingOut(right, left))
            {
                // left must stay entirely on the left of right's pushbox
                int depth = (left.Pushbox.Center.X + left.Pushbox.Size.X / 2) - (right.Pushbox.Center.X - right.Pushbox.Size.X / 2);
                if (depth > 0) left.Resolve(new Vector2I(depth, 0));
                return true;
            }
            // Left wall: the fighter further left is the one that could be pinned.
            if (left.MaxX + left.FixedPosition.X < right.Pushbox.Size.X && !JumpingOut(left, right))
            {
                int depth = (left.Pushbox.Center.X + left.Pushbox.Size.X / 2) - (right.Pushbox.Center.X - right.Pushbox.Size.X / 2);
                if (depth > 0) right.Resolve(new Vector2I(-depth, 0));
                return true;
            }
            return false;
        }

        /// <summary>[FF PATCH: pushbox] The cornered fighter is in the air above the other one: it's jumping out of the
        /// corner, which is allowed (normal pushbox rules apply).</summary>
        private static bool JumpingOut(PhysicsBody pinned, PhysicsBody other) =>
            !pinned.IsOnGround && pinned.FixedPosition.Y > other.FixedPosition.Y;

        private bool Unpushable(PhysicsBody bodyA, PhysicsBody bodyB)
        {
            bool LeftWall = bodyB.IsOnLeftWall && bodyA.FixedPosition.X >= bodyB.FixedPosition.X;
            bool RightWall = bodyB.IsOnRightWall && bodyA.FixedPosition.X <= bodyB.FixedPosition.X;
            return LeftWall || RightWall;
        }
    }

    public struct HitQuery
    {
        public IDamage p1;
        public int p1HitType;
        public HitboxElement p1Hitbox;
        public Vector2I p1ContactPoint;
        public IDamage p2;
        public int p2HitType;
        public HitboxElement p2Hitbox;
        public Vector2I p2ContactPoint;
    }
}
