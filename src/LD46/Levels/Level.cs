﻿using Floppy.Physics;
using LD46.Entities;
using LD46.Physics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace LD46.Levels {
    public class Level {
        public Level(LevelSettings settings) {
            int width = 32;
            if (settings.IsOpening) {
                width = 20;
            }
            else if (settings.IsVictory) {
                width = 16;
            }

            TileMap = new TileMap(width, settings.Height, PhysicsConstants.TileSize);
            PhysicsWorld = new PhysicsWorld();
            EntityWorld = new EntityWorld();

            var random = new Random();

            if (settings.IsOpening) {
                for (int y = 0; y < TileMap.Height; y++) {
                    for (int x = 0; x < TileMap.Width; x++) {
                        if (x == 0 || x == TileMap.Width - 1 | y == TileMap.Height - 1) {
                            TileMap[x, y].CollisionType = TileCollisionType.Solid;
                        }
                    }
                }

                for (int x = 1; x < TileMap.Width - 1; x++) {
                    TileMap[x, TileMap.Height - 5].CollisionType = TileCollisionType.Platform;

                    TileMap[x, TileMap.Height - 12].CollisionType = x < 7 ? TileCollisionType.Platform : TileCollisionType.Solid;

                    TileMap[x, TileMap.Height - 20].CollisionType = x < 15 ? TileCollisionType.Solid : TileCollisionType.Platform;

                    if (x > 7 && x < 14) {
                        TileMap[x, TileMap.Height - 15].CollisionType = TileCollisionType.Platform;
                    }

                    TileMap[x, TileMap.Height - 28].CollisionType = TileCollisionType.Platform;
                }

                FinishHeight = 22.5f;
            }
            else if (settings.IsVictory) {
                for (int y = 0; y < TileMap.Height; y++) {
                    for (int x = 0; x < TileMap.Width; x++) {
                        if (x == 0 || y == 0 || x == TileMap.Width - 1 | y == TileMap.Height - 1) {
                            TileMap[x, y].CollisionType = TileCollisionType.Solid;
                        }
                    }
                }

                for (int x = 1; x < TileMap.Width - 1; x++) {
                    if (x > 7 && x < 14) {
                        TileMap[x, TileMap.Height - 5].CollisionType = TileCollisionType.Platform;
                    }
                }

                FinishHeight = -100f;
            }
            else {
                for (int y = 0; y < TileMap.Height; y++) {
                    int platforms = 0;
                    int grates = 0;
                    bool solid = false;

                    for (int x = 0; x < TileMap.Width; x++) {
                        if (y % 3 == 0 && random.Next(10) == 0) {
                            platforms = random.Next(settings.MinPlatformWidth, settings.MaxPlatformWidth + 1);
                            solid = random.NextDouble() <= settings.SolidChance;
                        }

                        if (x == 0 || x == TileMap.Width - 1) {
                            TileMap[x, y].CollisionType = TileCollisionType.Solid;
                        }
                        else if (y == TileMap.Height - 20) {
                            TileMap[x, y].CollisionType = TileCollisionType.Platform;
                        }
                        else if (platforms > 0) {
                            if (settings.HasGrates && random.Next(8) == 0) {
                                grates = random.Next(2, 5);
                            }

                            if (grates > 0) {
                                TileMap[x, y].CollisionType = TileCollisionType.Grate;
                                grates--;
                            }
                            else {
                                TileMap[x, y].CollisionType = solid ? TileCollisionType.Solid : TileCollisionType.Platform;
                            }
                            platforms--;
                        }
                    }
                }
            }
            
            if (settings.HasWind) {
                for (int i = 0; i < TileMap.Height / 20 - 1; i++) {
                    WindChannels.Add(3f + i * 20f + ((float)random.NextDouble() - 0.5f) * 4f);
                }
            }
        }

        public TileMap TileMap { get; }
        public PhysicsWorld PhysicsWorld { get; }
        public EntityWorld EntityWorld { get; }

        public Vector2 CameraCenter { get; set; }

        public float FinishHeight { get; set; } = 16.5f;

        public float WaterLevel { get; set; } = 14f;
        public float WaterTop => TileMap.Height * PhysicsConstants.TileSize - WaterLevel;

        public float SlowMoTimer { get; set; } = 0f;

        public List<float> WindChannels { get; set; } = new List<float>();

        public void Update(float deltaTime) {
            foreach (Entity entity in EntityWorld.Entities) {
                entity.Brain?.Update(entity, this, deltaTime);

                if (PhysicsWorld.TryGetBody(entity.BodyID, out Body? body)) {
                    float speed = body.Velocity.Length();

                    if (speed >= entity.DangerSpeed && body.Contact.Y > 0f) {
                        body.Friction = entity.DangerFriction;
                    }
                    else {
                        body.Friction = body.Contact.Y > 0f ? entity.GroundFriction : entity.AirFriction;
                    }

                    if (entity.CanRotate) {
                        entity.Rotation += body.Velocity.X * deltaTime;
                    }

                    if (body.Position.Y >= WaterTop) {
                        entity.WaterTimer += deltaTime;

                        if (entity.WaterTimer >= 1f) {
                            entity.IsPutOut = true;
                        }
                    }
                    else {
                        entity.WaterTimer = 0f;
                    }

                    if (entity.IsBlowable) {
                        for (int i = 0; i < WindChannels.Count; i++) {
                            float channel = WindChannels[i];

                            if (body.Position.Y + body.Bounds.Center.Y >= channel - 4f && body.Position.Y + body.Bounds.Center.Y <= channel + 4f) {
                                body.Force += new Vector2(i % 2 == 0 ? 1f : -1f, 0f) * 10f;
                            }
                        }
                    }
                }
            }

            foreach (Body body in PhysicsWorld.Bodies) {
                BodyPhysics.UpdateBody(body, deltaTime, TileMap);
            }

            if (SlowMoTimer > 0f) {
                SlowMoTimer -= deltaTime;
            }
        }
    }
}
