﻿using Floppy.Graphics;
using Floppy.Physics;
using LD46.Entities;
using LD46.Graphics;
using LD46.Levels;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LD46.Views {
    public enum EntityViewProfile {
        Player,
        Torch,
    }

    public class EntityView {
        private readonly int _entityID;
        private readonly EntityViewProfile _profile;

        private readonly Sprite _sprite;

        private readonly EntityAnimations _animations;

        private IAnimation? _animation;
        private float _animationTimer = 0f;

        private bool _facingRight = true;

        public EntityView(int entityID, EntityViewProfile profile, Texture2D texture, EntityAnimations animations) {
            _entityID = entityID;
            _profile = profile;

            _sprite = new Sprite(texture);

            _animations = animations;
        }

        public void Update(Level level, float deltaTime) {
            if (!level.EntityWorld.TryGetEntity(_entityID, out Entity? entity)) {
                return;
            }

            if (!level.PhysicsWorld.TryGetBody(entity.BodyID, out Body? body)) {
                return;
            }

            if (body.Velocity.X > 0f) {
                _facingRight = true;
            }
            else if (body.Velocity.X < 0f) {
                _facingRight = false;
            }

            switch (_profile) {
                case EntityViewProfile.Player: {
                    if (entity.Kick) {
                        ReplayAnimation(_animations.PlayerKickRight, _animations.PlayerKickLeft);
                        entity.Kick = false;
                    }
                    else if ((_animation != _animations.PlayerKickRight && _animation != _animations.PlayerKickLeft)
                        || _animationTimer >= 0.35f) {

                        if (body.Contact.Y > 0f) {
                            if (body.Velocity.X != 0f) {
                                PlayAnimation(_animations.PlayerRunRight, _animations.PlayerRunLeft);
                            }
                            else {
                                PlayAnimation(_animations.PlayerIdleRight, _animations.PlayerIdleLeft);
                            }
                        }
                        else {
                            PlayAnimation(_animations.PlayerFallRight, _animations.PlayerFallLeft);
                        }
                    }
                    break;
                }
                case EntityViewProfile.Torch: {
                    PlayAnimation(_animations.Torch);
                    break;
                }
            }

            _animationTimer += deltaTime;
        }

        public void Draw(Level level, SpriteBatch spriteBatch) {
            if (!level.EntityWorld.TryGetEntity(_entityID, out Entity? entity)) {
                return;
            }

            if (!level.PhysicsWorld.TryGetBody(entity.BodyID, out Body? body)) {
                return;
            }

            _sprite.Rotation = entity.Rotation;

            _animation?.Apply(_sprite, _animationTimer);

            _sprite.Draw(spriteBatch, Vector2.Round(GraphicsConstants.PhysicsToView(body.Position + body.Bounds.Center)));
        }

        private void PlayAnimation(IAnimation rightAnimation, IAnimation leftAnimation) {
            PlayAnimation(_facingRight ? rightAnimation : leftAnimation);
        }

        private void PlayAnimation(IAnimation animation) {
            if (_animation != animation) {
                ReplayAnimation(animation);
            }
        }

        private void ReplayAnimation(IAnimation rightAnimation, IAnimation leftAnimation) {
            ReplayAnimation(_facingRight ? rightAnimation : leftAnimation);
        }

        private void ReplayAnimation(IAnimation animation) {
            _animation = animation;
            _animationTimer = 0f;
        }
    }
}